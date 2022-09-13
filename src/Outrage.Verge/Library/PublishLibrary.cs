using Compose.Path;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Library
{
    public struct Size
    {
        public int? width;
        public int? height;

        public Size(int? width = null, int? height = null)
        {
            this.width = width;
            this.height = height;
        }
    }

    public class PublishLibrary
    {
        private readonly PathBuilder publishPath;
        private HashSet<PathBuilder> writtenFiles = new();

        public PublishLibrary(PathBuilder publishPath)
        {
            this.publishPath = publishPath;
            if (this.publishPath.IsFile)
                throw new ArgumentException($"The publish path {publishPath} exists, and is not a directory.");
            if (!this.publishPath.IsDirectory)
                this.publishPath.CreateDirectory();
        }

        private PathBuilder GetFilename(string publishName)
        {
            var fileName = publishPath / publishName;
            var folderName = fileName.GetDirectory();
            folderName.CreateDirectory();
            return fileName;
        }

        public StreamWriter OpenWriter(string publishName)
        {
            var fileName = GetFilename(publishName);
            var writeStream = new PublishStream(fileName);
            writtenFiles.Add(fileName);
            return new StreamWriter(writeStream);
        }

        public Stream OpenStream(string publishName)
        {
            var fileName = GetFilename(publishName);
            var writeStream = new PublishStream(fileName);
            writtenFiles.Add(fileName);

            return writeStream;
        }

        public async Task<IEnumerable<Size>> Resize(ContentName publishName, Size[] sizes, ContentName? outputPublishName = null)
        {
            var rebuild = false;
            var sha1 = SHA1.Create();
            List<Size> outputSizes = new();
            var imageName = this.publishPath / publishName;
            if (imageName.IsFile)
            {
                SixLabors.ImageSharp.Image image;
                using (var imageStream = imageName.OpenFilestream(access: FileAccess.Read))
                {
                    image = SixLabors.ImageSharp.Image.Load(imageStream);
                    if (imageStream.CanSeek)
                    {
                        imageStream.Seek(0, SeekOrigin.Begin);
                        var hash = await sha1.ComputeHashAsync(imageStream);
                        var crc = Convert.ToBase64String(hash);

                        var crcName = imageName.Append(".crc");
                        string previousCrc = String.Empty;
                        if (crcName.IsFile)
                        {
                            previousCrc = crcName.ReadToEnd();
                        }

                        if (crc != previousCrc)
                        {
                            rebuild = true;
                            crcName.Write(crc);
                        }
                        writtenFiles.Add(crcName);
                    }
                    else
                    {
                        rebuild = true;
                    }
                }

                foreach (var size in sizes)
                {
                    var width = size.width;
                    var height = size.height;
                    if ((width.HasValue && width > image.Width) || (height.HasValue && height > image.Height))
                        continue;

                    outputSizes.Add(size);


                    var resultingOutputPublishName = outputPublishName;
                    if (resultingOutputPublishName == null)
                    {
                        ContentName resultName = publishName;
                        if (width.HasValue && !height.HasValue)
                            resultName = publishName.InjectExtension(String.Format("w{0}", width));
                        else if (!width.HasValue && height.HasValue)
                            resultName = publishName.InjectExtension(String.Format("h{0}", height));
                        else
                            resultName = publishName.InjectExtension(String.Format("w{0}h{1}", width, height));

                        resultingOutputPublishName = resultName;
                    }

                    var outputPath = this.publishPath / resultingOutputPublishName;

                    if (!outputPath.IsFile || rebuild)
                    {
                        var resizedImage = image.Clone(operation =>
                        {
                            if (width.HasValue && !height.HasValue)
                            {
                                var aspectRatio = (float)image.Height / image.Width;
                                var newHeight = (int)(width.Value * aspectRatio);
                                operation.Resize(width.Value, newHeight);
                            }

                            if (!width.HasValue && height.HasValue)
                            {
                                var aspectRatio = (float)image.Width / image.Height;
                                var newWidth = (int)(height.Value * aspectRatio);
                                operation.Resize(newWidth, height.Value);
                            }

                            if (width.HasValue && height.HasValue)
                            {
                                operation.Resize(width.Value, height.Value);
                            }

                        });


                        var encoder = resizedImage.DetectEncoder(outputPath);
                        using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                        resizedImage.Save(outputStream, encoder);
                    }
                    writtenFiles.Add(outputPath);
                }
            }
            else
                throw new ArgumentException($"Image to resize {imageName} is not a file.");

            return outputSizes;
        }

        public DateTimeOffset GetLastModified(ContentName contentName)
        {
            var contentFile = this.publishPath / contentName;
            return contentFile.GetLastModified();
        }

        public void CleanUp()
        {
            CleanUpUnwrittenFiles();
            CleanUpEmptyDirectories();
        }

        private void CleanUpUnwrittenFiles()
        {
            foreach (var file in this.publishPath.ListFiles(options: new EnumerationOptions { RecurseSubdirectories = true }))
            {
                var relative = file.GetRelativeTo(this.publishPath);
                if (!writtenFiles.Contains(file) && !relative.ToString().StartsWith('.'))
                    file.Delete();
            }
        }

        private void CleanUpEmptyDirectories()
        {
            foreach (var directory in this.publishPath.ListDirectories(options: new EnumerationOptions { RecurseSubdirectories = true }))
            {
                if (directory.ListFiles(options: new EnumerationOptions { RecurseSubdirectories = true }).Count() == 0)
                    directory.Delete(true);
            }
        }
    }
}
