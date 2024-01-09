using System.Net;

namespace Demos.LargeFilesUploading.Client;

public class StreamedContent : HttpContent
{
    private readonly Stream _stream;
    private readonly int _bufferSize;

    public event Action<long, long>? SerializationProgressed;


    public StreamedContent(Stream stream, int bufferSize)
    {
        _stream = stream;
        _bufferSize = bufferSize;
        Headers.ContentLength = _stream.Length;
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var length = _stream.Length;
        var buffer = new byte[_bufferSize];
        int bytesRead;

        while ((bytesRead = await _stream.ReadAsync(buffer, 0, _bufferSize)) > 0)
        {
            await stream.WriteAsync(buffer, 0, bytesRead);
            SerializationProgressed?.Invoke(bytesRead, length);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        length = _stream.Length;
        return true;
    }
}
