using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorFileManagerApp.Services.ComponentSettings;

namespace RazorFileManagerApp.Areas.FileManager.Pages;

public class FileManagerModel : PageModel
{
    private const string FileManagerInstanceId = "main-filemanager-page";
    private readonly IComponentSettingsProvider _componentSettings;

    public FileManagerModel(IComponentSettingsProvider componentSettings)
    {
        _componentSettings = componentSettings;
    }

    public FileManagerComponentSettings Settings { get; private set; } = new();

    public void OnGet()
    {
        Settings = _componentSettings.GetFileManagerSettings(FileManagerInstanceId, Request.Path);
    }
}


