using Demos.LargeFilesUploading.WebApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Demos.LargeFilesUploading.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddUploaderService("https://labs-blobstorage-wa-01.azurewebsites.net");

await builder.Build().RunAsync();
