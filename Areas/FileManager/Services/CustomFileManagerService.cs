using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Diagnostics;

namespace WebFileManager.Services;

public class CustomFileManagerService
{
    private readonly IWebHostEnvironment _env;
    private const string DefaultRootFolder = "UzivatelskeSoubory";

    public CustomFileManagerService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public Task<List<FileManagerEntry>> ListAsync(string? relativePath, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var absolute = GetAbsolutePath(safePath, rootContext);
        Directory.CreateDirectory(absolute);

        var entries = new List<FileManagerEntry>();
        foreach (var dir in Directory.GetDirectories(absolute))
        {
            var name = Path.GetFileName(dir);
            entries.Add(new FileManagerEntry(
                name,
                CombineRelative(safePath, name),
                true,
                0,
                Directory.GetLastWriteTimeUtc(dir),
                null));
        }

        foreach (var file in Directory.GetFiles(absolute))
        {
            var info = new FileInfo(file);
            var name = info.Name;
            entries.Add(new FileManagerEntry(
                name,
                CombineRelative(safePath, name),
                false,
                info.Length,
                info.LastWriteTimeUtc,
                BuildFileUrl(safePath, name, root)));
        }

        entries = entries
            .OrderByDescending(e => e.IsDirectory)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(entries);
    }

    public Task CreateFolderAsync(string? relativePath, string folderName, string? root = null)
    {
        var safeName = Path.GetFileName(folderName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return Task.CompletedTask;
        }

        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var absolute = GetAbsolutePath(CombineRelative(safePath, safeName), rootContext);
        Directory.CreateDirectory(absolute);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string relativePath, bool isDirectory, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var absolute = GetAbsolutePath(NormalizeRelative(relativePath), rootContext);
        if (isDirectory)
        {
            if (!Directory.Exists(absolute))
            {
                return Task.FromResult(false);
            }
            Directory.Delete(absolute, true);
            return Task.FromResult(true);
        }

        if (!File.Exists(absolute))
        {
            return Task.FromResult(false);
        }

        File.Delete(absolute);
        return Task.FromResult(true);
    }

    public Task<bool> RenameAsync(string relativePath, string newName, bool isDirectory, string? root = null)
    {
        var safeName = Path.GetFileName(newName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return Task.FromResult(false);
        }

        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var absolute = GetAbsolutePath(safePath, rootContext);
        var parent = Path.GetDirectoryName(absolute) ?? rootContext.AbsolutePath;
        var target = Path.Combine(parent, safeName);

        if (isDirectory)
        {
            if (!Directory.Exists(absolute))
            {
                return Task.FromResult(false);
            }
            Directory.Move(absolute, target);
            return Task.FromResult(true);
        }

        if (!File.Exists(absolute))
        {
            return Task.FromResult(false);
        }

        File.Move(absolute, target);
        return Task.FromResult(true);
    }

    public async Task<bool> UploadAsync(string? relativePath, Stream content, string fileName, string? root = null)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return false;
        }

        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var folder = GetAbsolutePath(safePath, rootContext);
        Directory.CreateDirectory(folder);
        var target = GetUniqueFilePath(folder, safeName);

        await using var targetStream = File.Create(target);
        await content.CopyToAsync(targetStream);
        return true;
    }

    public string GetParent(string? relativePath)
    {
        var safePath = NormalizeRelative(relativePath);
        if (string.IsNullOrWhiteSpace(safePath))
        {
            return string.Empty;
        }

        var parent = Path.GetDirectoryName(safePath) ?? string.Empty;
        return parent.Replace('\\', '/');
    }

    public string BuildFileUrl(string? relativePath, string fileName, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var combined = CombineRelative(safePath, fileName);
        var rootUrl = rootContext.UrlPath.Trim('/');
        var rel = combined.Replace("\\", "/").Trim('/');
        if (string.IsNullOrWhiteSpace(rootUrl))
        {
            return string.IsNullOrWhiteSpace(rel) ? "/" : $"/{rel}";
        }

        return string.IsNullOrWhiteSpace(rel) ? $"/{rootUrl}" : $"/{rootUrl}/{rel}";
    }

    private RootContext ResolveRootContext(string? root)
    {
        var defaultAbsolute = Path.Combine(_env.WebRootPath, DefaultRootFolder);
        var defaultContext = new RootContext(defaultAbsolute, DefaultRootFolder);
        var webRootAbsolute = Path.GetFullPath(_env.WebRootPath);

        var normalizedRoot = NormalizeRootParameter(root);
        if (string.IsNullOrWhiteSpace(normalizedRoot))
        {
            Directory.CreateDirectory(defaultContext.AbsolutePath);
            return defaultContext;
        }

        var relativeRoot = normalizedRoot;
        if (relativeRoot.Equals("wwwroot", StringComparison.OrdinalIgnoreCase))
        {
            relativeRoot = string.Empty;
        }
        else if (relativeRoot.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
        {
            relativeRoot = relativeRoot.Substring("wwwroot/".Length);
        }

        var absoluteCandidate = Path.GetFullPath(
            Path.Combine(_env.WebRootPath, relativeRoot.Replace("/", Path.DirectorySeparatorChar.ToString())));
        if (!absoluteCandidate.StartsWith(webRootAbsolute, StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(defaultContext.AbsolutePath);
            return defaultContext;
        }

        // If requested root does not exist (or is not a directory), fallback to default root.
        if (!Directory.Exists(absoluteCandidate))
        {
            Directory.CreateDirectory(defaultContext.AbsolutePath);
            return defaultContext;
        }

        return new RootContext(absoluteCandidate, relativeRoot.Replace("\\", "/").Trim('/'));
    }

    private string GetAbsolutePath(string relativePath, RootContext? rootContext = null)
    {
        var context = rootContext ?? ResolveRootContext(null);
        var root = context.AbsolutePath;
        var combined = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        return combined;
    }

    private static string NormalizeRelative(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var trimmed = relativePath.Replace("\\", "/").Trim('/');
        return trimmed;
    }

    private static string CombineRelative(string basePath, string name)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return name;
        }

        return $"{basePath.TrimEnd('/')}/{name}";
    }

    private static string NormalizeRootParameter(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return string.Empty;
        }

        var value = root.Trim();

        var isLikelyBase64 = value.Length >= 8
            && value.Length % 4 == 0
            && System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z0-9+/]+={0,2}$");

        if (isLikelyBase64)
        {
            try
            {
                var decodedBytes = Convert.FromBase64String(value);
                var decoded = System.Text.Encoding.UTF8.GetString(decodedBytes);
                if (!string.IsNullOrWhiteSpace(decoded)
                    && System.Text.RegularExpressions.Regex.IsMatch(decoded, "^[\\w\\s./\\\\\\-\\u00C0-\\u024F]+$"))
                {
                    value = decoded;
                }
            }
            catch
            {
                // Keep raw value.
            }
        }

        return value.Replace("\\", "/").Trim('/');
    }

    private static string GetUniqueFilePath(string folder, string fileName)
    {
        var target = Path.Combine(folder, fileName);
        if (!File.Exists(target))
        {
            return target;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var i = 1;
        while (true)
        {
            var candidate = Path.Combine(folder, $"{name}-{i}{ext}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
            i++;
        }
    }

    public Task<bool> DeleteMultipleAsync(string? relativePath, List<string> fileNames, List<string> directoryNames, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var basePath = GetAbsolutePath(safePath, rootContext);

        foreach (var fileName in fileNames)
        {
            if (string.IsNullOrWhiteSpace(fileName)) continue;
            var filePath = Path.Combine(basePath, Path.GetFileName(fileName));
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        foreach (var dirName in directoryNames)
        {
            if (string.IsNullOrWhiteSpace(dirName)) continue;
            var dirPath = Path.Combine(basePath, Path.GetFileName(dirName));
            if (Directory.Exists(dirPath) && dirPath != rootContext.AbsolutePath)
            {
                Directory.Delete(dirPath, true);
            }
        }

        return Task.FromResult(true);
    }

    public Task<bool> CopyAsync(string? relativePath, string? targetPath, List<string> fileNames, List<string> directoryNames, bool move = false, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var sourceBase = GetAbsolutePath(safePath, rootContext);
        
        string targetBase;
        if (targetPath is null)
        {
            // KopĂ­rovĂˇnĂ­ o ĂşroveĹ vĂ˝Ĺˇ
            var parent = GetParent(safePath);
            targetBase = GetAbsolutePath(parent, rootContext);
        }
        else
        {
            targetBase = GetAbsolutePath(NormalizeRelative(targetPath), rootContext);
        }

        // KopĂ­rovĂˇnĂ­ adresĂˇĹ™ĹŻ
        foreach (var dirName in directoryNames)
        {
            if (string.IsNullOrWhiteSpace(dirName)) continue;
            var sourceDir = Path.Combine(sourceBase, Path.GetFileName(dirName));
            var targetDir = Path.Combine(targetBase, Path.GetFileName(dirName));
            
            if (Directory.Exists(sourceDir) && sourceDir != targetDir)
            {
                CopyDirectory(sourceDir, GetUniqueDirectoryPath(targetBase, Path.GetFileName(dirName)));
                if (move)
                {
                    Directory.Delete(sourceDir, true);
                }
            }
        }

        // KopĂ­rovĂˇnĂ­ souborĹŻ
        foreach (var fileName in fileNames)
        {
            if (string.IsNullOrWhiteSpace(fileName)) continue;
            var sourceFile = Path.Combine(sourceBase, Path.GetFileName(fileName));
            var targetFile = Path.Combine(targetBase, Path.GetFileName(fileName));
            
            if (File.Exists(sourceFile) && sourceFile != targetFile)
            {
                var uniqueTarget = GetUniqueFilePath(targetBase, Path.GetFileName(fileName));
                File.Copy(sourceFile, uniqueTarget);
                if (move)
                {
                    File.Delete(sourceFile);
                }
            }
        }

        return Task.FromResult(true);
    }

    public Task<string> CreateZipAsync(string? relativePath, List<string> fileNames, List<string> directoryNames, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var basePath = GetAbsolutePath(safePath, rootContext);
        var zipName = $"Zip_{DateTime.Now:yyyy_MM_dd_HHmmss}.zip";
        var zipPath = GetUniqueFilePath(basePath, zipName);

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        
        // PĹ™idĂˇnĂ­ souborĹŻ
        foreach (var fileName in fileNames)
        {
            if (string.IsNullOrWhiteSpace(fileName)) continue;
            var filePath = Path.Combine(basePath, Path.GetFileName(fileName));
            if (File.Exists(filePath))
            {
                zip.CreateEntryFromFile(filePath, Path.GetFileName(fileName));
            }
        }

        // PĹ™idĂˇnĂ­ adresĂˇĹ™ĹŻ
        foreach (var dirName in directoryNames)
        {
            if (string.IsNullOrWhiteSpace(dirName)) continue;
            var dirPath = Path.Combine(basePath, Path.GetFileName(dirName));
            if (Directory.Exists(dirPath))
            {
                AddDirectoryToZip(zip, dirPath, Path.GetFileName(dirName));
            }
        }

        return Task.FromResult(Path.GetFileName(zipPath));
    }

    public Task<bool> ExtractZipAsync(string? relativePath, string zipFileName, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var basePath = GetAbsolutePath(safePath, rootContext);
        var zipPath = Path.Combine(basePath, Path.GetFileName(zipFileName));
        
        if (!File.Exists(zipPath))
        {
            return Task.FromResult(false);
        }

        var extractDir = Path.GetFileNameWithoutExtension(zipFileName);
        var extractPath = GetUniqueDirectoryPath(basePath, extractDir);
        Directory.CreateDirectory(extractPath);

        ZipFile.ExtractToDirectory(zipPath, extractPath);
        return Task.FromResult(true);
    }

    public Task<byte[]?> GetImageThumbnailAsync(string? relativePath, string fileName, int maxWidth = 300, int maxHeight = 300, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var basePath = GetAbsolutePath(safePath, rootContext);
        var imagePath = Path.Combine(basePath, Path.GetFileName(fileName));

        if (!File.Exists(imagePath))
        {
            return Task.FromResult<byte[]?>(null);
        }

        try
        {
            using var originalImage = Image.FromFile(imagePath);
            var thumbnail = ResizeImage(originalImage, new Size(maxWidth, maxHeight));
            
            using var ms = new MemoryStream();
            thumbnail.Save(ms, ImageFormat.Jpeg);
            return Task.FromResult<byte[]?>(ms.ToArray());
        }
        catch
        {
            return Task.FromResult<byte[]?>(null);
        }
    }

    /// <summary>
    /// PĹ™evod DOCX -> doÄŤasnĂ© PDF pomocĂ­ nainstalovanĂ©ho LibreOffice (soffice).
    /// VracĂ­ relativnĂ­ cestu z koĹ™ene UzivatelskeSoubory (napĹ™. "_temp/xxx.pdf") nebo null pĹ™i chybÄ›.
    /// </summary>
    public async Task<string?> ConvertDocxToTempPdfAsync(string? relativePath, string fileName, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var basePath = GetAbsolutePath(safePath, rootContext);
        var sourcePath = Path.Combine(basePath, Path.GetFileName(fileName));

        if (!File.Exists(sourcePath))
        {
            return null;
        }

        var rootPath = rootContext.AbsolutePath;
        var tempDir = Path.Combine(rootPath, "_temp");
        Directory.CreateDirectory(tempDir);

        // VĂ˝stupnĂ­ nĂˇzev PDF
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var targetPdfPath = GetUniqueFilePath(tempDir, baseName + ".pdf");

        // LibreOffice zapisuje PDF do cĂ­lovĂ©ho adresĂˇĹ™e se jmĂ©nem podle zdrojovĂ©ho souboru,
        // proto si zapamatujeme oÄŤekĂˇvanĂ˝ prefix.
        var expectedPdfName = Path.GetFileName(targetPdfPath);

        // SpuĹˇtÄ›nĂ­ LibreOffice v headless reĹľimu
        var psi = new ProcessStartInfo
        {
            FileName = "soffice",
            Arguments = $"--headless --convert-to pdf --outdir \"{tempDir}\" \"{sourcePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await process.WaitForExitAsync(cts.Token);

            if (process.ExitCode != 0)
            {
                return null;
            }
        }
        catch
        {
            return null;
        }

        // NajĂ­t v temp sloĹľce vygenerovanĂ© PDF
        var pdfFile = Directory.GetFiles(tempDir, baseName + "*.pdf")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (pdfFile == null || !File.Exists(pdfFile))
        {
            return null;
        }

        // VrĂˇtĂ­me relativnĂ­ cestu z UzivatelskeSoubory
        var rel = pdfFile.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return rel.Replace("\\", "/");
    }

    public Task<bool> DeleteTempRelativeFileAsync(string? relativePath, string? root = null)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.FromResult(false);
        }

        var rootPath = ResolveRootContext(root).AbsolutePath;
        var safe = relativePath.Replace("\\", "/").TrimStart('/');
        var absolute = Path.Combine(rootPath, safe.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (File.Exists(absolute))
        {
            try
            {
                File.Delete(absolute);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }

    public string GetFileExtension(string fileName)
    {
        return Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
    }

    public bool IsImageFile(string fileName)
    {
        var ext = GetFileExtension(fileName);
        return ext is "jpg" or "jpeg" or "png" or "gif" or "bmp" or "svg" or "tif" or "webp";
    }

    public string GetIconUrl(string fileName, bool isDirectory)
    {
        if (isDirectory)
        {
            return "/lib/FileManager/Icony/folder.png";
        }

        var ext = GetFileExtension(fileName);
        var iconPath = $"/lib/FileManager/Icony/{ext}.png";
        
        // Kontrola existence ikony
        var physicalPath = Path.Combine(_env.WebRootPath, "lib", "FileManager", "Icony", $"{ext}.png");
        if (!File.Exists(physicalPath))
        {
            return "/lib/FileManager/Icony/txt.png"; // VĂ˝chozĂ­ ikona
        }

        return iconPath;
    }

    private void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(targetDir, fileName), true);
        }

        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(subDir);
            CopyDirectory(subDir, Path.Combine(targetDir, dirName));
        }
    }

    private string GetUniqueDirectoryPath(string folder, string dirName)
    {
        var target = Path.Combine(folder, dirName);
        if (!Directory.Exists(target))
        {
            return target;
        }

        var i = 1;
        while (true)
        {
            var candidate = Path.Combine(folder, $"{dirName}_({i})");
            if (!Directory.Exists(candidate))
            {
                return candidate;
            }
            i++;
        }
    }

    private void AddDirectoryToZip(ZipArchive zip, string dirPath, string entryName)
    {
        foreach (var file in Directory.GetFiles(dirPath))
        {
            var relativePath = Path.Combine(entryName, Path.GetFileName(file));
            zip.CreateEntryFromFile(file, relativePath);
        }

        foreach (var subDir in Directory.GetDirectories(dirPath))
        {
            var subDirName = Path.GetFileName(subDir);
            AddDirectoryToZip(zip, subDir, Path.Combine(entryName, subDirName));
        }
    }

    private static Image ResizeImage(Image image, Size size)
    {
        var sourceWidth = image.Width;
        var sourceHeight = image.Height;
        
        float nPercentW = (float)size.Width / sourceWidth;
        float nPercentH = (float)size.Height / sourceHeight;
        float nPercent = Math.Min(nPercentH, nPercentW);
        
        var destWidth = (int)(sourceWidth * nPercent);
        var destHeight = (int)(sourceHeight * nPercent);
        
        var bitmap = new Bitmap(destWidth, destHeight);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(image, 0, 0, destWidth, destHeight);
        
        return bitmap;
    }

    public string GetAbsolutePathForRead(string? relativePath, string fileName, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var basePath = GetAbsolutePath(safePath, rootContext);
        return Path.Combine(basePath, Path.GetFileName(fileName));
    }

    public string GetAbsolutePathForWrite(string? relativePath, string fileName, string? root = null)
    {
        var rootContext = ResolveRootContext(root);
        var safePath = NormalizeRelative(relativePath);
        var basePath = GetAbsolutePath(safePath, rootContext);
        Directory.CreateDirectory(basePath);
        return Path.Combine(basePath, Path.GetFileName(fileName));
    }

    private sealed record RootContext(string AbsolutePath, string UrlPath);
}

public record FileManagerEntry(
    string Name,
    string RelativePath,
    bool IsDirectory,
    long Size,
    DateTimeOffset Modified,
    string? Url);




