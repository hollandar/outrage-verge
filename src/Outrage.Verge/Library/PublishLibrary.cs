using Compose.Path;
using Force.Crc32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Library
{
    public class Size
    {
        public int? width;
        public int? height;
        public int? resizeWidth;
        public int? resizeHeight;

        public Size(int? width = null, int? height = null, int? resizeWidth = null, int? resizeHeight = null)
        {
            this.width = width;
            this.height = height;
            this.resizeWidth = width;
            this.resizeHeight = height;
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

        public Stream OpenPublishStream(string publishName)
        {
            var fileName = GetFilename(publishName);
            var writeStream = new PublishStream(fileName);
            writtenFiles.Add(fileName);

            return writeStream;
        }

        public async Task<string> LoadContentAsync(string publishName)
        {
            var filename = GetFilename(publishName);
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using var streamReader = new StreamReader(stream);
            return await streamReader.ReadToEndAsync();
        }

        public Task<IEnumerable<Size>> Resize(ContentName publishName, Size[] sizes, ContentName? outputPublishName = null)
        {
            var rebuild = false;
            SixLabors.ImageSharp.Image? image = null;
            List<Size> outputSizes = new();
            var imageName = this.publishPath / publishName;
            if (imageName.IsFile)
            {
                var loadedImage = new MemoryStream();
                using (var imageStream = imageName.OpenFilestream(access: FileAccess.Read))
                {
                    imageStream.CopyTo(loadedImage);
                }
                // Load image width and height metadata
                loadedImage.Seek(0, SeekOrigin.Begin);
                IImageInfo imageInfo = SixLabors.ImageSharp.Image.Identify(loadedImage);

                // Calculate a useful crc
                loadedImage.Seek(0, SeekOrigin.Begin);
                var crc = Crc32Algorithm.Compute(loadedImage.GetBuffer());
                var crcString = crc.ToString();

                var crcName = imageName.Append(".crc");
                string previousCrc = String.Empty;
                if (crcName.IsFile)
                {
                    previousCrc = crcName.ReadToEnd();
                }

                if (crcString != previousCrc)
                {
                    rebuild = true;
                    crcName.Write(crcString);
                }
                writtenFiles.Add(crcName);

                foreach (var size in sizes)
                {
                    var width = size.width;
                    var height = size.height;
                    var imageWidth = size.resizeWidth ?? width;
                    var imageHeight = size.resizeHeight ?? height;

                    // Dont render sizes beyond the size of the image, let the browser scale it if needed
                    if ((width.HasValue && width > imageInfo.Width) || (height.HasValue && height > imageInfo.Height))
                        continue;

                    outputSizes.Add(size);


                    var resultingOutputPublishName = outputPublishName;
                    if (resultingOutputPublishName == null)
                    {
                        ContentName resultName = publishName;
                        if (imageWidth.HasValue && !imageHeight.HasValue)
                            resultName = publishName.InjectExtension(String.Format("w{0}", imageWidth));
                        else if (!imageWidth.HasValue && imageHeight.HasValue)
                            resultName = publishName.InjectExtension(String.Format("h{0}", imageHeight));
                        else
                            resultName = publishName.InjectExtension(String.Format("w{0}h{1}", imageWidth, imageHeight));

                        resultingOutputPublishName = resultName;
                    }

                    var outputPath = this.publishPath / resultingOutputPublishName;

                    if (!outputPath.IsFile || rebuild)
                    {
                        if (image == null)
                        {
                            loadedImage.Seek(0, SeekOrigin.Begin);
                            image = SixLabors.ImageSharp.Image.Load(loadedImage);
                        }

                        var resizedImage = image.Clone(operation =>
                        {

                            if (imageWidth.HasValue && !imageHeight.HasValue)
                            {
                                var aspectRatio = (float)image.Height / image.Width;
                                var newHeight = (int)(imageWidth.Value * aspectRatio);
                                operation.Resize(imageWidth.Value, newHeight);
                            }

                            if (!imageWidth.HasValue && imageHeight.HasValue)
                            {
                                var aspectRatio = (float)image.Width / image.Height;
                                var newWidth = (int)(imageHeight.Value * aspectRatio);
                                operation.Resize(newWidth, imageHeight.Value);
                            }

                            if (imageWidth.HasValue && imageHeight.HasValue)
                            {
                                operation.Resize(imageWidth.Value, imageWidth.Value);
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

            return Task.FromResult<IEnumerable<Size>>(outputSizes);
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
