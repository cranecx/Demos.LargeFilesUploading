using Microsoft.Extensions.DependencyInjection;

namespace Demos.LargeFilesUploading.Client;

public static class UploadServiceExtensions
{
    public static IServiceCollection AddUploaderService(this IServiceCollection services, string baseAddress)
    {
        services.AddHttpClient(UploaderService.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(baseAddress);
        });

        services.AddSingleton<UploaderService>();
        return services;
    }
}
