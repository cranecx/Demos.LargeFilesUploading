using Demos.LargeFilesUploading.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Demos.LargeFilesUploading.WebApp.Components;

public partial class FileUploader
{
    private Guid Key {  get; set; } = Guid.NewGuid();

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> InputAttributes { get; set; } = [];

    [Parameter]
    public long MaxAllowedSize { get; set; }

    [Parameter]
    public EventCallback OnFileSelected { get; set; }

    [Parameter]
    public bool Disabled { get; set; } = false;

    public Stream? File {  get; private set; }
    public string? FileName { get; private set; } 
    public bool FileSelected => File != null;

    private InputFile? InputFile { get; set; }

    private async void OnFileChanged(InputFileChangeEventArgs e)
    {
        File = e.File.OpenReadStream(MaxAllowedSize);
        FileName = e.File.Name;

        await OnFileSelected.InvokeAsync();
    }

    public void Reset()
    {
        Key = Guid.NewGuid();
        File?.Dispose();
        File = null;
        FileName = null;

        StateHasChanged();
    }
}
