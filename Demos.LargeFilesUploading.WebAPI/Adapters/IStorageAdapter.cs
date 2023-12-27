namespace Demos.LargeFilesUploading.WebAPI.Adapters
{
    public interface IStorageAdapter
    {
        Task CreateIfNotExists(string name);
        Task<IStorageWritter> Open(string name);
    }
}
