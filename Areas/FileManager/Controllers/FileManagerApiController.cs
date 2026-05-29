using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebFileManager.Services;
using System.IO;

namespace WebFileManager.Controllers;

[ApiController]
[Route("api/filemanager")]
public class FileManagerApiController : ControllerBase
{
    private static readonly HashSet<string> DefaultImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "gif", "bmp", "webp", "svg", "tif", "tiff"
    };

    private readonly CustomFileManagerService _fileService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileManagerApiController> _logger;
    private readonly long _maxUploadBytes;

    public FileManagerApiController(
        CustomFileManagerService fileService,
        IWebHostEnvironment env,
        ILogger<FileManagerApiController> logger,
        IConfiguration configuration)
    {
        _fileService = fileService;
        _env = env;
        _logger = logger;
        _maxUploadBytes = configuration.GetValue<long?>("UploadLimits:MaxRequestBodyBytes") ?? (2L * 1024L * 1024L * 1024L);
        if (_maxUploadBytes <= 0)
        {
            _maxUploadBytes = 2L * 1024L * 1024L * 1024L;
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] string? path, [FromQuery] string? root)
    {
        var decodedPath = DecodeUrlParam(path);
        var decodedRoot = DecodeUrlParam(root);
        try
        {
            var entries = await _fileService.ListAsync(decodedPath, decodedRoot);
            return Ok(entries);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "List denied. User={User}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", decodedRoot, decodedPath);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List failed. User={User}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", decodedRoot, decodedPath);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Načtení souborů selhalo." });
        }
    }

    [HttpPost("create-folder")]
    public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
    {
        try
        {
            await _fileService.CreateFolderAsync(request.Path, request.FolderName, request.Root);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "CreateFolder denied. User={User}, Root={Root}, Path={Path}, Folder={Folder}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.FolderName);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateFolder failed. User={User}, Root={Root}, Path={Path}, Folder={Folder}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.FolderName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Vytvoření složky selhalo." });
        }
    }

    [HttpPost("rename")]
    public async Task<IActionResult> Rename([FromBody] RenameRequest request)
    {
        try
        {
            var result = await _fileService.RenameAsync(request.Path, request.NewName, request.IsDirectory, request.Root);
            return result ? Ok() : BadRequest();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Rename denied. User={User}, Root={Root}, Path={Path}, NewName={NewName}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.NewName);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rename failed. User={User}, Root={Root}, Path={Path}, NewName={NewName}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.NewName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Přejmenování selhalo." });
        }
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        try
        {
            var result = await _fileService.DeleteAsync(request.Path, request.IsDirectory, request.Root);
            return result ? Ok() : BadRequest();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Delete denied. User={User}, Root={Root}, Path={Path}, IsDir={IsDirectory}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.IsDirectory);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed. User={User}, Root={Root}, Path={Path}, IsDir={IsDirectory}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.IsDirectory);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Mazání selhalo." });
        }
    }

    [HttpPost("delete-multiple")]
    public async Task<IActionResult> DeleteMultiple([FromBody] DeleteMultipleRequest request)
    {
        try
        {
            await _fileService.DeleteMultipleAsync(request.Path, request.FileNames, request.DirectoryNames, request.Root);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "DeleteMultiple denied. User={User}, Root={Root}, Path={Path}, Files={FileCount}, Dirs={DirCount}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.FileNames?.Count ?? 0, request.DirectoryNames?.Count ?? 0);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteMultiple failed. User={User}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Hromadné mazání selhalo." });
        }
    }

    [HttpPost("copy")]
    public async Task<IActionResult> Copy([FromBody] CopyRequest request)
    {
        try
        {
            await _fileService.CopyAsync(request.SourcePath, request.TargetPath, request.FileNames, request.DirectoryNames, request.Move, request.Root);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Copy denied. User={User}, Root={Root}, Src={Source}, Target={Target}, Move={Move}", User?.Identity?.Name ?? "anonymous", request.Root, request.SourcePath, request.TargetPath, request.Move);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy failed. User={User}, Root={Root}, Src={Source}, Target={Target}, Move={Move}", User?.Identity?.Name ?? "anonymous", request.Root, request.SourcePath, request.TargetPath, request.Move);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Kopírování selhalo." });
        }
    }

    [HttpPost("zip")]
    public async Task<IActionResult> CreateZip([FromBody] ZipRequest request)
    {
        try
        {
            var zipName = await _fileService.CreateZipAsync(request.Path, request.FileNames, request.DirectoryNames, request.Root);
            return Ok(new { ZipName = zipName });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "CreateZip denied. User={User}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateZip failed. User={User}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Vytvoření ZIP selhalo." });
        }
    }

    [HttpPost("unzip")]
    public async Task<IActionResult> ExtractZip([FromBody] UnzipRequest request)
    {
        try
        {
            var result = await _fileService.ExtractZipAsync(request.Path, request.ZipFileName, request.Root);
            return result ? Ok() : BadRequest();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "ExtractZip denied. User={User}, Root={Root}, Path={Path}, Zip={Zip}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.ZipFileName);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExtractZip failed. User={User}, Root={Root}, Path={Path}, Zip={Zip}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.ZipFileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Rozbalení ZIP selhalo." });
        }
    }

    [HttpPost("export-pdf-jpg")]
    public async Task<IActionResult> ExportPdfJpg([FromBody] ExportPdfJpgRequest request)
    {
        try
        {
            var exported = await _fileService.ExportPdfFirstPageToJpgAsync(request.Path, request.FileName, request.Root);
            if (exported is null)
            {
                return BadRequest(new { message = "PDF soubor nebyl nalezen nebo nejde exportovat." });
            }

            return Ok(new
            {
                fileName = exported.FileName,
                url = exported.Url,
                relativePath = exported.RelativePath
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "ExportPdfJpg denied. User={User}, Root={Root}, Path={Path}, File={File}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.FileName);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExportPdfJpg failed. User={User}, Root={Root}, Path={Path}, File={File}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Export PDF do JPG selhal." });
        }
    }

    [HttpGet("thumbnail")]
    public async Task<IActionResult> GetThumbnail([FromQuery] string? path, [FromQuery] string fileName, [FromQuery] string? root)
    {
        var decodedPath = DecodeUrlParam(path);
        var decodedFileName = DecodeUrlParam(fileName) ?? fileName;
        var decodedRoot = DecodeUrlParam(root);

        try
        {
            var thumbnail = await _fileService.GetImageThumbnailAsync(
                decodedPath,
                decodedFileName,
                root: decodedRoot);

            if (thumbnail != null)
            {
                return File(thumbnail, "image/jpeg");
            }
        }
        catch
        {
            // Na některých hostinzích může selhat generování miniatury (práva/cesta/cache).
            // Fallback níže vrátí originální soubor, aby UI nepadalo na HTTP 500.
            _logger.LogWarning("Thumbnail generation failed, serving original. User={User}, Root={Root}, Path={Path}, File={File}", User?.Identity?.Name ?? "anonymous", decodedRoot, decodedPath, decodedFileName);
        }

        var originalAbsolutePath = _fileService.GetAbsolutePathForRead(decodedPath, decodedFileName, decodedRoot);
        if (!System.IO.File.Exists(originalAbsolutePath))
        {
            return NotFound();
        }

        return PhysicalFile(originalAbsolutePath, GetImageContentType(decodedFileName));
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload()
    {
        string? path = null;
        string? root = null;
        string? onlyImagesRaw = null;
        string? allowExtRaw = null;
        List<IFormFile>? uploadedFiles = null;

        try
        {
            path = Request.Query["path"].FirstOrDefault();
            root = Request.Query["root"].FirstOrDefault();
            onlyImagesRaw = Request.Query["onlyImages"].FirstOrDefault() ?? Request.Query["onlyIMG"].FirstOrDefault();
            allowExtRaw = Request.Query["allowExt"].FirstOrDefault() ?? Request.Query["extensions"].FirstOrDefault();
            uploadedFiles = Request.Form?.Files?.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Upload request read failed. User={User}", User?.Identity?.Name ?? "anonymous");
            return BadRequest(new { message = "Upload request nelze načíst." });
        }

        if (uploadedFiles == null || uploadedFiles.Count == 0)
        {
            return BadRequest(new { message = "Nebyly predany soubory k uploadu." });
        }

        var decodedPath = DecodeUrlParam(path) ?? string.Empty;
        var decodedRoot = DecodeUrlParam(root);
        var decodedOnlyImages = DecodeUrlParam(onlyImagesRaw);
        var decodedAllowExt = DecodeUrlParam(allowExtRaw);
        var onlyImages = IsTruthy(decodedOnlyImages);
        var allowedExtensions = ParseAllowedExtensions(decodedAllowExt);

        if (onlyImages && (allowedExtensions is null || allowedExtensions.Count == 0))
        {
            allowedExtensions = new HashSet<string>(DefaultImageExtensions, StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var uploaded = new List<object>();
            foreach (var file in uploadedFiles)
            {
                if (file.Length <= 0)
                {
                    continue;
                }

                var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
                if (allowedExtensions is not null && allowedExtensions.Count > 0 && !allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Upload rejected by extension list. User={User}, File={File}, Ext={Ext}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", file.FileName, extension, decodedRoot, decodedPath);
                    return BadRequest(new { message = $"Soubor '{file.FileName}' nema povolenou priponu." });
                }

                if (onlyImages && !DefaultImageExtensions.Contains(extension))
                {
                    _logger.LogWarning("Upload rejected (not image). User={User}, File={File}, Ext={Ext}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", file.FileName, extension, decodedRoot, decodedPath);
                    return BadRequest(new { message = $"Soubor '{file.FileName}' neni podporovany obrazek." });
                }

                if (file.Length > _maxUploadBytes)
                {
                    return BadRequest(new { message = $"Soubor '{file.FileName}' prekrocil maximalni velikost uploadu." });
                }

                await using var stream = file.OpenReadStream();
                var stored = await _fileService.UploadWithResultAsync(decodedPath, stream, file.FileName, decodedRoot);
                if (stored is not null)
                {
                    uploaded.Add(new
                    {
                        fileName = stored.FileName,
                        url = stored.Url,
                        relativePath = stored.RelativePath
                    });
                }
            }

            if (uploaded.Count == 0)
            {
                return BadRequest(new { message = "Nepodarilo se ulozit zadny soubor. Zkontrolujte velikost souboru a zkuste to znovu." });
            }

            return Ok(new { uploaded });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Upload denied. User={User}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", decodedRoot, decodedPath);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed. User={User}, Root={Root}, Path={Path}", User?.Identity?.Name ?? "anonymous", decodedRoot, decodedPath);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Upload selhal." });
        }

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
            _logger.LogWarning("SaveText denied. User={User}, Root={Root}, Path={Path}, File={File}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.FileName);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Nemáte oprávnění k zápisu do tohoto souboru." });
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogWarning("SaveText directory not found. User={User}, Root={Root}, Path={Path}, File={File}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.FileName);
            return BadRequest(new { message = "Adresář nebyl nalezen." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveText failed. User={User}, Root={Root}, Path={Path}, File={File}", User?.Identity?.Name ?? "anonymous", request.Root, request.Path, request.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Chyba při ukládání souboru." });
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

    private static bool IsTruthy(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var value = raw.Trim();
        return !value.Equals("0", StringComparison.OrdinalIgnoreCase)
            && !value.Equals("false", StringComparison.OrdinalIgnoreCase)
            && !value.Equals("off", StringComparison.OrdinalIgnoreCase)
            && !value.Equals("no", StringComparison.OrdinalIgnoreCase);
    }

    private static HashSet<string>? ParseAllowedExtensions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var items = raw
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().TrimStart('.').ToLowerInvariant())
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return items.Count == 0 ? null : items;
    }

    private static string GetImageContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".tif" => "image/tiff",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }
}

public record CreateFolderRequest(string? Path, string FolderName, string? Root = null);
public record RenameRequest(string Path, string NewName, bool IsDirectory, string? Root = null);
public record DeleteRequest(string Path, bool IsDirectory, string? Root = null);
public record DeleteMultipleRequest(string? Path, List<string> FileNames, List<string> DirectoryNames, string? Root = null);
public record CopyRequest(string? SourcePath, string? TargetPath, List<string> FileNames, List<string> DirectoryNames, bool Move = false, string? Root = null);
public record ZipRequest(string? Path, List<string> FileNames, List<string> DirectoryNames, string? Root = null);
public record UnzipRequest(string? Path, string ZipFileName, string? Root = null);
public record ExportPdfJpgRequest(string? Path, string FileName, string? Root = null);
public record SaveTextRequest(string? Path, string FileName, string? Content, string? Root = null);


