using Microsoft.AspNetCore.Mvc;
using System.Text;
using RazorFileManagerApp.Services;

namespace RazorFileManagerApp.Controllers;

[ApiController]
[Route("api/filemanager")]
public class FileManagerApiController : ControllerBase
{
    private readonly CustomFileManagerService _fileService;
    private readonly IWebHostEnvironment _env;

    public FileManagerApiController(CustomFileManagerService fileService, IWebHostEnvironment env)
    {
        _fileService = fileService;
        _env = env;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] string? path, [FromQuery] string? root)
    {
        var entries = await _fileService.ListAsync(DecodeUrlParam(path), DecodeUrlParam(root));
        return Ok(entries);
    }

    [HttpPost("create-folder")]
    public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
    {
        await _fileService.CreateFolderAsync(request.Path, request.FolderName, request.Root);
        return Ok();
    }

    [HttpPost("rename")]
    public async Task<IActionResult> Rename([FromBody] RenameRequest request)
    {
        var result = await _fileService.RenameAsync(request.Path, request.NewName, request.IsDirectory, request.Root);
        return result ? Ok() : BadRequest();
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        var result = await _fileService.DeleteAsync(request.Path, request.IsDirectory, request.Root);
        return result ? Ok() : BadRequest();
    }

    [HttpPost("delete-multiple")]
    public async Task<IActionResult> DeleteMultiple([FromBody] DeleteMultipleRequest request)
    {
        await _fileService.DeleteMultipleAsync(request.Path, request.FileNames, request.DirectoryNames, request.Root);
        return Ok();
    }

    [HttpPost("copy")]
    public async Task<IActionResult> Copy([FromBody] CopyRequest request)
    {
        await _fileService.CopyAsync(request.SourcePath, request.TargetPath, request.FileNames, request.DirectoryNames, request.Move, request.Root);
        return Ok();
    }

    [HttpPost("zip")]
    public async Task<IActionResult> CreateZip([FromBody] ZipRequest request)
    {
        var zipName = await _fileService.CreateZipAsync(request.Path, request.FileNames, request.DirectoryNames, request.Root);
        return Ok(new { ZipName = zipName });
    }

    [HttpPost("unzip")]
    public async Task<IActionResult> ExtractZip([FromBody] UnzipRequest request)
    {
        var result = await _fileService.ExtractZipAsync(request.Path, request.ZipFileName, request.Root);
        return result ? Ok() : BadRequest();
    }

    [HttpGet("thumbnail")]
    public async Task<IActionResult> GetThumbnail([FromQuery] string? path, [FromQuery] string fileName, [FromQuery] string? root)
    {
        var thumbnail = await _fileService.GetImageThumbnailAsync(
            DecodeUrlParam(path),
            DecodeUrlParam(fileName) ?? fileName,
            root: DecodeUrlParam(root));
        if (thumbnail == null)
        {
            return NotFound();
        }

        return File(thumbnail, "image/jpeg");
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromQuery] string? path, [FromQuery] string? root, IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest();
        }

        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                await using var stream = file.OpenReadStream();
                await _fileService.UploadAsync(DecodeUrlParam(path), stream, file.FileName, DecodeUrlParam(root));
            }
        }

        return Ok();
    }

    [HttpPost("save-text")]
    public async Task<IActionResult> SaveText([FromBody] SaveTextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest(new { message = "Název souboru není zadán." });
        }

        try
        {
            var absolutePath = _fileService.GetAbsolutePathForWrite(request.Path, request.FileName, request.Root);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await System.IO.File.WriteAllTextAsync(absolutePath, request.Content ?? string.Empty, System.Text.Encoding.UTF8);
            return Ok(new { success = true });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Nemáte oprávnění k zápisu do tohoto souboru." });
        }
        catch (DirectoryNotFoundException)
        {
            return BadRequest(new { message = "Adresář nebyl nalezen." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Chyba při ukládání souboru: {ex.Message}" });
        }
    }

    private static string? DecodeUrlParam(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("b64:", StringComparison.OrdinalIgnoreCase))
        {
            var decoded = TryDecodeBase64Url(trimmed.Substring(4));
            return decoded ?? string.Empty;
        }

        // Backward compatibility for historical plain Base64 values.
        if (LooksLikeLegacyBase64(trimmed))
        {
            var decodedLegacy = TryDecodeBase64(trimmed);
            if (!string.IsNullOrWhiteSpace(decodedLegacy))
            {
                return decodedLegacy;
            }
        }

        return trimmed;
    }

    private static bool LooksLikeLegacyBase64(string value)
    {
        if (value.Length < 8 || value.Length % 4 != 0)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!(char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '='))
            {
                return false;
            }
        }

        return true;
    }

    private static string? TryDecodeBase64Url(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return string.Empty;
        }

        var normalized = payload.Replace('-', '+').Replace('_', '/');
        var padLen = normalized.Length % 4 == 0 ? 0 : 4 - (normalized.Length % 4);
        normalized = normalized.PadRight(normalized.Length + padLen, '=');
        return TryDecodeBase64(normalized);
    }

    private static string? TryDecodeBase64(string payload)
    {
        try
        {
            var bytes = Convert.FromBase64String(payload);
            var decoded = Encoding.UTF8.GetString(bytes);
            return IsSafeDecodedValue(decoded) ? decoded : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSafeDecodedValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var c in value)
        {
            if (char.IsControl(c) && !char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        return true;
    }
}

public record CreateFolderRequest(string? Path, string FolderName, string? Root = null);
public record RenameRequest(string Path, string NewName, bool IsDirectory, string? Root = null);
public record DeleteRequest(string Path, bool IsDirectory, string? Root = null);
public record DeleteMultipleRequest(string? Path, List<string> FileNames, List<string> DirectoryNames, string? Root = null);
public record CopyRequest(string? SourcePath, string? TargetPath, List<string> FileNames, List<string> DirectoryNames, bool Move = false, string? Root = null);
public record ZipRequest(string? Path, List<string> FileNames, List<string> DirectoryNames, string? Root = null);
public record UnzipRequest(string? Path, string ZipFileName, string? Root = null);
public record SaveTextRequest(string? Path, string FileName, string? Content, string? Root = null);

