namespace WebFileManager.Services.ComponentSettings;

public sealed class AvatarComponentSettings
{
    public string Modifier { get; set; } = "AvatarFM";
    public int CropWidth { get; set; } = 800;
    public int CropHeight { get; set; } = 200;
    public int MaxCropStateLength { get; set; } = 2000;
    public int MaxDisplayedLogEntries { get; set; } = 100;
    public bool FileManagerOnlyImages { get; set; } = true;
    public string FileManagerAllowExt { get; set; } = "jpg,jpeg,png,webp,gif";
    public bool FileManagerUseDefaultRoot { get; set; } = true;
    public string? FileManagerRoot { get; set; }
}

public sealed class CKEditorComponentSettings
{
    public string SetPath { get; set; } = "UzivatelskeSoubory";
    public string CallbackName { get; set; } = "ZachyceniURLCKeditor";
    public string PickerAllowExt { get; set; } = "jpg,jpeg,png,gif,bmp,webp,svg,pdf";
    public string PopupName { get; set; } = "FileManagerPicker";
}

public sealed class FileManagerComponentSettings
{
    public string? Root { get; set; }
    public bool? OnlyImages { get; set; }
    public string? AllowExt { get; set; }
    public string? Callback { get; set; }
}

public sealed class AvatarComponentSettingsOverride
{
    public string? Modifier { get; set; }
    public int? CropWidth { get; set; }
    public int? CropHeight { get; set; }
    public int? MaxCropStateLength { get; set; }
    public int? MaxDisplayedLogEntries { get; set; }
    public bool? FileManagerOnlyImages { get; set; }
    public string? FileManagerAllowExt { get; set; }
    public bool? FileManagerUseDefaultRoot { get; set; }
    public string? FileManagerRoot { get; set; }
}

public sealed class CKEditorComponentSettingsOverride
{
    public string? SetPath { get; set; }
    public string? CallbackName { get; set; }
    public string? PickerAllowExt { get; set; }
    public string? PopupName { get; set; }
}

public sealed class FileManagerComponentSettingsOverride
{
    public string? Root { get; set; }
    public bool? OnlyImages { get; set; }
    public string? AllowExt { get; set; }
    public string? Callback { get; set; }
}

public sealed class ComponentsSettingsDocument
{
    public int Version { get; set; } = 1;
    public ComponentsSettingsRoot Components { get; set; } = new();
}

public sealed class ComponentsSettingsRoot
{
    public AvatarSettingsSection Avatar { get; set; } = new();
    public CKEditorSettingsSection CKEditor { get; set; } = new();
    public FileManagerSettingsSection FileManager { get; set; } = new();
}

public sealed class AvatarSettingsSection
{
    public AvatarComponentSettings Defaults { get; set; } = new();
    public List<AvatarInstanceSettings> Instances { get; set; } = new();
}

public sealed class CKEditorSettingsSection
{
    public CKEditorComponentSettings Defaults { get; set; } = new();
    public List<CKEditorInstanceSettings> Instances { get; set; } = new();
}

public sealed class FileManagerSettingsSection
{
    public FileManagerComponentSettings Defaults { get; set; } = new();
    public List<FileManagerInstanceSettings> Instances { get; set; } = new();
}

public sealed class AvatarInstanceSettings
{
    public string InstanceId { get; set; } = string.Empty;
    public string? PageScope { get; set; }
    public AvatarComponentSettingsOverride Settings { get; set; } = new();
}

public sealed class CKEditorInstanceSettings
{
    public string InstanceId { get; set; } = string.Empty;
    public string? PageScope { get; set; }
    public CKEditorComponentSettingsOverride Settings { get; set; } = new();
}

public sealed class FileManagerInstanceSettings
{
    public string InstanceId { get; set; } = string.Empty;
    public string? PageScope { get; set; }
    public FileManagerComponentSettingsOverride Settings { get; set; } = new();
}


