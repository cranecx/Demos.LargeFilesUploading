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

            while ((bytesRead = await content.ReadAsync(buffer.AsMemory(0, chunkSize))) > 0)
            {
                using var multipartContent = new MultipartFormDataContent();
                using var streamedContent = new StreamedContent(new MemoryStream(buffer), 1 * 1024 * 1024);

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
}
