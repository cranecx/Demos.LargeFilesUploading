using Demos.LargeFilesUploading.Client;
using Demos.LargeFilesUploading.WebApp.Components;
using Microsoft.AspNetCore.Components;

namespace Demos.LargeFilesUploading.WebApp.Pages;

public partial class Home
{
    private const long MaxAllowedSize = 5000000000;

    private bool FormDisabled = false;
    private bool FileSelected => FileUploader?.FileSelected ?? false;
    private string? SuccessMessage { get; set; }
    private string? StatusMessage { get; set; } = "Seleccione un archivo.";
    private string? FailedMessage { get; set; }

    [Inject]
    private UploaderService? UploaderService { get; set; }

    private FileUploader? FileUploader { get; set; }

    protected override void OnInitialized()
    {
        UploaderService!.UploadStarted += OnUploadStarted;
        UploaderService!.UploadProgressed += OnUploadProgressed;
        UploaderService!.UploadCompleted += OnUploadCompleted;
        UploaderService!.UploadFailed += OnUploadFailed;
    }

    private void OnFileSelected()
    {
        StateHasChanged();
    }

    private async void OnUploadClick()
    {
        if (!FileSelected)
            return;

        await UploaderService!.UploadBlocks(FileUploader!.File!, FileUploader.FileName!, 10 * 1024 * 1024, 10);
    }

    private void OnUploadStarted(Guid _)
    {
        FormDisabled = true;
        SuccessMessage = null;
        FailedMessage = null;
        StatusMessage = "Iniciando carga de archivo...";

        StateHasChanged();
    }

    private void OnUploadProgressed(Guid _, long uploaded, long total)
    {
        var percentage = ((double)(uploaded * 100)) / total;
        StatusMessage = $"{percentage:0.00}% completado.";

        StateHasChanged();
    }

    private void OnUploadCompleted(Guid _)
    {
        SuccessMessage = "Carga completada.";
        StatusMessage = "Seleccione un archivo.";
        FormDisabled = false;
        FileUploader?.Reset();

        StateHasChanged();
    }

    private void OnUploadFailed(Guid _, Exception exception)
    {
        FailedMessage = $"Ocurrio un error al cargar el archivo: ${exception.Message}";
        StatusMessage = "Seleccione un archivo.";
        FormDisabled = false;
        FileUploader?.Reset();

        StateHasChanged();
    }
}
