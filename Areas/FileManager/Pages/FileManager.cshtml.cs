using Microsoft.AspNetCore.Mvc.RazorPages;
using WebFileManager.Services.ComponentSettings;

namespace WebFileManager.Areas.FileManager.Pages;

public class FileManagerModel : PageModel
{
    private const string FileManagerInstanceId = "main-filemanager-page";
    private readonly IComponentSettingsProvider _componentSettings;

    public FileManagerModel(IComponentSettingsProvider componentSettings)
    {
        _componentSettings = componentSettings;
    }

    public FileManagerComponentSettings Settings { get; private set; } = new();
    public bool IsPickerMode { get; private set; }

    public void OnGet()
    {
        Settings = _componentSettings.GetFileManagerSettings(FileManagerInstanceId, Request.Path);
        IsPickerMode = IsTruthyQueryValue("picker");
    }

    private bool IsTruthyQueryValue(string key)
    {
        if (!Request.Query.TryGetValue(key, out var values))
        {
            return false;
        }

        var raw = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        return !raw.Equals("0", StringComparison.OrdinalIgnoreCase)
            && !raw.Equals("false", StringComparison.OrdinalIgnoreCase)
            && !raw.Equals("off", StringComparison.OrdinalIgnoreCase)
            && !raw.Equals("no", StringComparison.OrdinalIgnoreCase);
    }
}



