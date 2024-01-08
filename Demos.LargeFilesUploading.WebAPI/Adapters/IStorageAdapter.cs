namespace Demos.LargeFilesUploading.WebAPI.Adapters;

public interface IStorageAdapter
{
    Task CreateIfNotExists(string name);
    Task<IStorageWritter> Open(string name);
    Task UploadBlock(string blobName, string blockId, Stream blockData);
    Task CommitBlocks(string blobName, IEnumerable<string> blockIds);
}
