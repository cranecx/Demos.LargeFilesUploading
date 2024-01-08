using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Demos.LargeFilesUploading.Client;

public class UploaderService
{
    public const string HttpClientName = "UploaderServiceHttpClient";
    private const string StreamRoute = "files/upload/stream";

    public event Action<Guid>? UploadStarted;
    public event Action<Guid, long, long>? UploadProgressed;
    public event Action<Guid>? UploadCompleted;
    public event Action<Guid, Exception>? UploadFailed;

    private HttpClient HttpClient { get; }
    public UploaderService(IHttpClientFactory clientFactory)
    {
        HttpClient = clientFactory.CreateClient(HttpClientName);
    }

    public async Task UploadStream(Stream content, string fileName, int bufferSize)
    {
        var operationId = Guid.NewGuid();
        try
        {
            using var multipartContent = new MultipartFormDataContent();
            using var streamedContent = new StreamedContent(content, bufferSize);

            // Suscribirse a los eventos de StreamedContent y 'reenviarlos'
            streamedContent.SerializationProgressed += (bytesUploaded, totalBytes) => UploadProgressed?.Invoke(operationId, bytesUploaded, totalBytes);

            multipartContent.Add(streamedContent, "file", Path.GetFileName(fileName));

            // Enviar la solicitud
            UploadStarted?.Invoke(operationId);

            var response = await HttpClient.PostAsync(StreamRoute, multipartContent);
            response.EnsureSuccessStatusCode();

            UploadCompleted?.Invoke(operationId);
        }
        catch (Exception ex)
        {
            UploadFailed?.Invoke(operationId, ex);
        }
    }

    public async Task UploadChunks(Stream content, string fileName, int chunkSize)
    {
        var operationId = Guid.NewGuid();
        try
        {
            byte[] buffer = new byte[chunkSize];
            int bytesRead;
            long totalBytesRead = 0;

            while ((bytesRead = await content.ReadAsync(buffer, 0, chunkSize)) > 0)
            {
                using var multipartContent = new MultipartFormDataContent();
                using var memoryBuffer = new MemoryStream(buffer);
                using var streamedContent = new StreamedContent(memoryBuffer, 1 * 1024 * 1024);

                // Suscribirse a los eventos de StreamedContent
                streamedContent.SerializationProgressed += (uploaded, total) => UploadProgressed?.Invoke(operationId, totalBytesRead + uploaded, content.Length);

                multipartContent.Add(new StringContent(operationId.ToString()), "operationId");
                multipartContent.Add(streamedContent, "file", Path.GetFileName(fileName));

                // Enviar la solicitud
                UploadStarted?.Invoke(operationId);

                var response = await HttpClient.PostAsync($"files/upload/chunk?operationId={operationId}", multipartContent);
                response.EnsureSuccessStatusCode();

                totalBytesRead += bytesRead;
            }

            UploadCompleted?.Invoke(operationId);
        }
        catch (Exception ex)
        {
            UploadFailed?.Invoke(operationId, ex);
        }
    }

    public async Task UploadFileBlocks(Stream content, string fileName, int blockSize, int maxParallelUploads)
    {
        var operationId = Guid.NewGuid();
        var blobName = $"{operationId}{Path.GetExtension(fileName)}";
        var blockIds = new ConcurrentBag<string>();
        var semaphore = new SemaphoreSlim(maxParallelUploads);
        var tasks = new List<Task>();

        int blockNum = 0;
        byte[] buffer = new byte[blockSize];
        int bytesRead;

        UploadStarted?.Invoke(operationId);

        try
        {
            while ((bytesRead = await content.ReadAsync(buffer, 0, blockSize)) > 0)
            {
                await semaphore.WaitAsync(); // Espera por un espacio en el semáforo

                var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{blockNum:D6}"));
                blockIds.Add(blockId);

                var blockData = new MemoryStream(buffer, 0, bytesRead);
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await UploadBlockAsync(blobName, blockId, blockData);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
                blockNum++;
            }

            await Task.WhenAll(tasks);
            await CommitBlocksAsync(blobName, blockIds.ToList());
        }
        catch (Exception ex)
        {
            UploadFailed?.Invoke(operationId, ex);
        }
    }

    private async Task UploadBlockAsync(string blobName, string blockId, Stream blockData)
    {
        using var streamedContent = new StreamedContent(blockData, 1024 * 1024); // Ajusta el tamaño del buffer según sea necesario
        var multipartContent = new MultipartFormDataContent
        {
            { new StringContent(blobName), "fileName" },
            { new StringContent(blockId), "blockId" },
            { streamedContent, "file", $"{blockId}.bin" } // Usa blockId para el nombre del archivo temporal
        };

        var response = await HttpClient.PostAsync("files/upload/block", multipartContent);
        response.EnsureSuccessStatusCode();
    }

    private async Task CommitBlocksAsync(string blobName, IEnumerable<string> blockIds)
    {
        var url = $"files/upload/block/commit?blobName={blobName}";
        var content = new StringContent(JsonSerializer.Serialize(blockIds), Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
    }
}
