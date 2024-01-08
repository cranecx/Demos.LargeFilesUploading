using Azure.Storage.Blobs.Specialized;

namespace Demos.LargeFilesUploading.WebAPI.Adapters;

public class BlobStorageWritter : IStorageWritter
{
    private readonly AppendBlobClient _blobClient;

    public BlobStorageWritter(AppendBlobClient blobClient)
    {
        _blobClient = blobClient;
    }

    public async Task Append(Memory<byte> data)
    {
        using var memoryStream = new MemoryStream(data.ToArray());
        await _blobClient.AppendBlockAsync(memoryStream);
    }

    public void Dispose()
    {
    }
}
