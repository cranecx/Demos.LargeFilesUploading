using Demos.LargeFilesUploading.WebAPI.Adapters;
using Demos.LargeFilesUploading.WebAPI.Filters;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Demos.LargeFilesUploading.WebAPI.Controllers;

[Route("files")]
[ApiController]
public class FilesController : ControllerBase
{
    private IStorageAdapter _storageAdapter { get; }

    public FilesController(IStorageAdapter storageAdapter)
    {
        _storageAdapter = storageAdapter;
    }

    [HttpPost]
    [Route("upload/stream")]
    public async Task<IActionResult> UploadStream()
    {
        if (string.IsNullOrEmpty(Request.ContentType) ||
            !Request.ContentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Missing or wrong Content-Type header.");
        }

        var boundary = Request.GetMultipartBoundary();

        var reader = new MultipartReader(boundary, HttpContext.Request.Body);
        MultipartSection? section;
        while ((section = await reader.ReadNextSectionAsync()) != null)
        {
            var fileSection = section.AsFileSection();
            if (fileSection != null)
            {
                if (!section.Headers!.TryGetValue("Content-Length", out var contentLength))
                    return BadRequest("Missing Content-Length header in file.");

                var fileSize = long.Parse(contentLength!);
                var fileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(fileSection.FileName)}");
                await _storageAdapter.CreateIfNotExists(fileName);
                using var storageWritter = await _storageAdapter.Open(fileName);

                var bufferSize = 8192;
                var buffer = new byte[bufferSize];
                var totalRead = 0;
                var bytesRead = 0;

                while (totalRead < fileSize && (bytesRead = await fileSection.FileStream!.ReadAsync(buffer, 0, bufferSize)) > 0)
                {
                    await storageWritter.Append(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;
                }
            }
        }

        return Ok();
    }

    [HttpPost]
    [Route("upload/chunk")]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> UploadChunk()
    {
        if (string.IsNullOrEmpty(Request.ContentType) ||
            !Request.ContentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Missing or wrong Content-Type header.");
        }

        var formCollection = await Request.ReadFormAsync();
        var operationIdString = formCollection["operationId"];
        if (!Guid.TryParse(operationIdString, out var operationId))
        {
            return BadRequest("Invalid operationId.");
        }

        var boundary = Request.GetMultipartBoundary();
        var reader = new MultipartReader(boundary, HttpContext.Request.Body);
        MultipartSection? section;

        while ((section = await reader.ReadNextSectionAsync()) != null)
        {
            var fileSection = section.AsFileSection();
            if (fileSection != null)
            {
                if (!section.Headers!.TryGetValue("Content-Length", out var contentLength))
                    return BadRequest("Missing Content-Length header in file."); ;

                var fileSize = long.Parse(contentLength!);
                var fileName = Path.Combine(Path.GetTempPath(), $"{operationId}{Path.GetExtension(fileSection.FileName)}");

                await _storageAdapter.CreateIfNotExists(fileName);
                using var storageWritter = await _storageAdapter.Open(fileName);
                var bufferSize = 8192;
                var buffer = new byte[bufferSize];
                var totalRead = 0;
                var bytesRead = 0;

                while (totalRead < fileSize && (bytesRead = await fileSection.FileStream!.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await storageWritter.Append(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;
                }
            }
        }

        return Ok();
    }
}
