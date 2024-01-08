using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;

namespace Demos.LargeFilesUploading.WebAPI.Adapters;

public class BlobStorageAdapter : IStorageAdapter
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageOptions _blobStorageOptions;

    public BlobStorageAdapter(IOptions<BlobStorageOptions> options)
    {
        _blobStorageOptions = options.Value;
        _blobServiceClient = new BlobServiceClient(new
            Uri($"https://{_blobStorageOptions.AccountName}.blob.core.windows.net"),
            new DefaultAzureCredential());
    }

    public async Task CreateIfNotExists(string name)
    {
        var blobClient = _blobServiceClient
            .GetBlobContainerClient(_blobStorageOptions.ContainerName!)
            .GetAppendBlobClient(name);

        await blobClient.CreateIfNotExistsAsync();
    }

    public Task<IStorageWritter> Open(string name)
    {
        var blobClient = _blobServiceClient
            .GetBlobContainerClient(_blobStorageOptions.ContainerName!)
            .GetAppendBlobClient(name);

        return Task.FromResult<IStorageWritter>(new BlobStorageWritter(blobClient));
    }

    public async Task UploadBlock(string blobName, string blockId, Stream block)
    {
        var blobClient = _blobServiceClient
            .GetBlobContainerClient(_blobStorageOptions.ContainerName!)
            .GetBlockBlobClient(blobName);

        block.Position = 0;
        await blobClient.StageBlockAsync(blockId, block);
    }

    public async Task CommitBlocks(string blobName, IEnumerable<string> blockIds)
    {
        var blobClient = _blobServiceClient
            .GetBlobContainerClient(_blobStorageOptions.ContainerName!)
            .GetBlockBlobClient(blobName);

        await blobClient.CommitBlockListAsync(blockIds);
    }
}
