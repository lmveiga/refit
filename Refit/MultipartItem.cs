using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Refit
{
    public abstract class MultipartItem
    {
        public MultipartItem(string fileName, string contentType)
        {
            FileName = fileName ?? throw new ArgumentNullException("fileName");
            ContentType = contentType;
        }

        public MultipartItem(string fileName, string contentType, string name) : this(fileName, contentType)
        {
            Name = name;
        }

        public string Name { get; }

        public string ContentType { get; }

        public string FileName { get; }

        public HttpContent ToContent()
        {
            var content = CreateContent();
            if (!string.IsNullOrEmpty(ContentType))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
            }

            return content;
        }

        protected abstract HttpContent CreateContent();
    }

    public class StreamPart : MultipartItem
    {
        public StreamPart(Stream value, string fileName, string contentType = null, string name = null, Action<double> progress = null) :
            base(fileName, contentType, name)
        {
            Value = value ?? throw new ArgumentNullException("value");
            Progress = progress;
        }

        public Stream Value { get; }
        private Action<double> Progress { get; }

        protected override HttpContent CreateContent()
        {
            return new StreamProgressContent(Value, Progress);
        }
    }

    public class StreamProgressContent : StreamContent
    {

        const int ChunkSize = 4096;
        readonly byte[] bytes;
        readonly Action<double> progress;

        public StreamProgressContent(Stream content, Action<double> progress) : base(content)
        {
            this.progress = progress;
            this.bytes = new byte[content.Length];
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            for (var i = 0; i < bytes.Length; i+= ChunkSize)
            {
                await stream.WriteAsync(this.bytes, i, Math.Min(ChunkSize, bytes.Length - i));
                progress?.Invoke(100.0 * i / bytes.Length);
            }
        }
    }

    public class ByteArrayPart : MultipartItem
    {
        public ByteArrayPart(byte[] value, string fileName, string contentType = null, string name = null) :
            base(fileName, contentType, name)
        {
            Value = value ?? throw new ArgumentNullException("value");
        }

        public byte[] Value { get; }

        protected override HttpContent CreateContent()
        {
            return new ByteArrayContent(Value);
        }
    }

    public class FileInfoPart : MultipartItem
    {
        public FileInfoPart(FileInfo value, string fileName, string contentType = null, string name = null) :
            base(fileName, contentType, name)
        {
            Value = value ?? throw new ArgumentNullException("value");
        }

        public FileInfo Value { get; }

        protected override HttpContent CreateContent()
        {
            return new StreamContent(Value.OpenRead());
        }
    }
}
