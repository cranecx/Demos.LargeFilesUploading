namespace Demos.LargeFilesUploading.WebAPI.Adapters
{
    public interface IStorageWritter : IDisposable
    {
        Task Append(Memory<byte> data);
    }
}
