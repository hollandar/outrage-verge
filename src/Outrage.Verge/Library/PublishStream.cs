using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Outrage.Verge.Library
{
    public class PublishStream : Stream
    {
        private readonly string path;
        private MemoryStream innerStream;
        private byte[] underlyingChecksum = emptyChecksum;
        static SHA1 sha1 = SHA1.Create();
        static byte[] emptyChecksum = Enumerable.Repeat((byte)0, 20).ToArray();
        public PublishStream(string path)
        {
            innerStream = new MemoryStream();
            this.path = path;
            if (File.Exists(path))
            {
                using var underlyingStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                underlyingChecksum = sha1.ComputeHash(underlyingStream);
            }
        }

        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;
        public override long Length => innerStream.Length;
        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }
        public override long Position { get => innerStream.Position; set => innerStream.Position = value; }
        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerStream.Read(buffer, offset, count);
        }
        public override void Flush()
        {
            innerStream.Flush();
        }

        public override void Close()
        {
            innerStream.Seek(0, SeekOrigin.Begin);
            var sha1Checksum = sha1.ComputeHash(innerStream);
            if (!Enumerable.SequenceEqual(sha1Checksum, underlyingChecksum))
            {
                innerStream.Seek(0, SeekOrigin.Begin);

                using var underlyingStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                innerStream.CopyTo(underlyingStream);
                underlyingStream.SetLength(innerStream.Length);
            }
            base.Close();
        }
    }
}
