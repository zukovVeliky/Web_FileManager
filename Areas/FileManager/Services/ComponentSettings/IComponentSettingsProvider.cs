namespace WebFileManager.Services.ComponentSettings;

public interface IComponentSettingsProvider
{
    AvatarComponentSettings GetAvatarSettings(string instanceId, string? pageScope = null);
    CKEditorComponentSettings GetCKEditorSettings(string instanceId, string? pageScope = null);
    FileManagerComponentSettings GetFileManagerSettings(string instanceId, string? pageScope = null);
}


