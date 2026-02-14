using System.Text.Json;

namespace RazorFileManagerApp.Services.ComponentSettings;

public sealed class JsonComponentSettingsProvider : IComponentSettingsProvider
{
    private readonly string _settingsPath;
    private readonly object _sync = new();

    private ComponentsSettingsDocument? _cachedDocument;
    private DateTime _lastWriteUtc;

    public JsonComponentSettingsProvider(IWebHostEnvironment environment)
    {
        _settingsPath = Path.Combine(environment.ContentRootPath, "components.settings.json");
    }

    public AvatarComponentSettings GetAvatarSettings(string instanceId, string? pageScope = null)
    {
        var doc = LoadDocument();
        var defaults = doc.Components.Avatar.Defaults ?? new AvatarComponentSettings();
        var selected = SelectInstance(doc.Components.Avatar.Instances, instanceId, pageScope);
        return Merge(defaults, selected?.Settings);
    }

    public CKEditorComponentSettings GetCKEditorSettings(string instanceId, string? pageScope = null)
    {
        var doc = LoadDocument();
        var defaults = doc.Components.CKEditor.Defaults ?? new CKEditorComponentSettings();
        var selected = SelectInstance(doc.Components.CKEditor.Instances, instanceId, pageScope);
        return Merge(defaults, selected?.Settings);
    }

    public FileManagerComponentSettings GetFileManagerSettings(string instanceId, string? pageScope = null)
    {
        var doc = LoadDocument();
        var defaults = doc.Components.FileManager.Defaults ?? new FileManagerComponentSettings();
        var selected = SelectInstance(doc.Components.FileManager.Instances, instanceId, pageScope);
        return Merge(defaults, selected?.Settings);
    }

    private ComponentsSettingsDocument LoadDocument()
    {
        lock (_sync)
        {
            EnsureSettingsFile();

            var writeUtc = File.GetLastWriteTimeUtc(_settingsPath);
            if (_cachedDocument != null && writeUtc == _lastWriteUtc)
            {
                return _cachedDocument;
            }

            var json = File.ReadAllText(_settingsPath);
            var parsed = JsonSerializer.Deserialize<ComponentsSettingsDocument>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ComponentsSettingsDocument();

            _cachedDocument = parsed;
            _lastWriteUtc = writeUtc;
            return parsed;
        }
    }

    private void EnsureSettingsFile()
    {
        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(_settingsPath))
        {
            return;
        }

        var defaultDoc = new ComponentsSettingsDocument
        {
            Components = new ComponentsSettingsRoot
            {
                Avatar = new AvatarSettingsSection
                {
                    Defaults = new AvatarComponentSettings(),
                    Instances =
                    {
                        new AvatarInstanceSettings
                        {
                            InstanceId = "avatar-main",
                            PageScope = "/AvatarFileManager",
                            Settings = new AvatarComponentSettingsOverride
                            {
                                Modifier = "AvatarFM",
                                CropWidth = 800,
                                CropHeight = 200,
                                MaxCropStateLength = 2000,
                                MaxDisplayedLogEntries = 100,
                                FileManagerOnlyImages = true,
                                FileManagerAllowExt = "jpg,jpeg,png,webp,gif",
                                FileManagerUseDefaultRoot = true
                            }
                        }
                    }
                },
                CKEditor = new CKEditorSettingsSection
                {
                    Defaults = new CKEditorComponentSettings(),
                    Instances =
                    {
                        new CKEditorInstanceSettings
                        {
                            InstanceId = "home-editor",
                            PageScope = "/",
                            Settings = new CKEditorComponentSettingsOverride
                            {
                                SetPath = "UzivatelskeSoubory",
                                CallbackName = "ZachyceniURLCKeditor_home",
                                PopupName = "FileManagerPicker_home"
                            }
                        }
                    }
                },
                FileManager = new FileManagerSettingsSection
                {
                    Defaults = new FileManagerComponentSettings(),
                    Instances =
                    {
                        new FileManagerInstanceSettings
                        {
                            InstanceId = "main-filemanager-page",
                            PageScope = "/FileManager",
                            Settings = new FileManagerComponentSettingsOverride
                            {
                                Root = null,
                                OnlyImages = null,
                                AllowExt = null,
                                Callback = null
                            }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(defaultDoc, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_settingsPath, json);
    }

    private static AvatarInstanceSettings? SelectInstance(List<AvatarInstanceSettings> instances, string instanceId, string? pageScope)
    {
        return SelectInstanceCore(instances, instanceId, pageScope, x => x.InstanceId, x => x.PageScope);
    }

    private static CKEditorInstanceSettings? SelectInstance(List<CKEditorInstanceSettings> instances, string instanceId, string? pageScope)
    {
        return SelectInstanceCore(instances, instanceId, pageScope, x => x.InstanceId, x => x.PageScope);
    }

    private static FileManagerInstanceSettings? SelectInstance(List<FileManagerInstanceSettings> instances, string instanceId, string? pageScope)
    {
        return SelectInstanceCore(instances, instanceId, pageScope, x => x.InstanceId, x => x.PageScope);
    }

    private static T? SelectInstanceCore<T>(
        IEnumerable<T> instances,
        string instanceId,
        string? pageScope,
        Func<T, string> getId,
        Func<T, string?> getPage)
    {
        var normalizedId = instanceId?.Trim() ?? string.Empty;
        var normalizedPage = NormalizePage(pageScope);

        var exact = instances.FirstOrDefault(x =>
            string.Equals(getId(x), normalizedId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(NormalizePage(getPage(x)), normalizedPage, StringComparison.OrdinalIgnoreCase));

        if (exact != null)
        {
            return exact;
        }

        return instances.FirstOrDefault(x =>
            string.Equals(getId(x), normalizedId, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(getPage(x)));
    }

    private static string NormalizePage(string? page)
    {
        if (string.IsNullOrWhiteSpace(page))
        {
            return string.Empty;
        }

        return page.Trim();
    }

    private static AvatarComponentSettings Merge(AvatarComponentSettings defaults, AvatarComponentSettingsOverride? overrides)
    {
        return new AvatarComponentSettings
        {
            Modifier = string.IsNullOrWhiteSpace(overrides?.Modifier) ? defaults.Modifier : overrides.Modifier,
            CropWidth = overrides?.CropWidth ?? defaults.CropWidth,
            CropHeight = overrides?.CropHeight ?? defaults.CropHeight,
            MaxCropStateLength = overrides?.MaxCropStateLength ?? defaults.MaxCropStateLength,
            MaxDisplayedLogEntries = overrides?.MaxDisplayedLogEntries ?? defaults.MaxDisplayedLogEntries,
            FileManagerOnlyImages = overrides?.FileManagerOnlyImages ?? defaults.FileManagerOnlyImages,
            FileManagerAllowExt = string.IsNullOrWhiteSpace(overrides?.FileManagerAllowExt) ? defaults.FileManagerAllowExt : overrides.FileManagerAllowExt,
            FileManagerUseDefaultRoot = overrides?.FileManagerUseDefaultRoot ?? defaults.FileManagerUseDefaultRoot,
            FileManagerRoot = overrides?.FileManagerRoot ?? defaults.FileManagerRoot
        };
    }

    private static CKEditorComponentSettings Merge(CKEditorComponentSettings defaults, CKEditorComponentSettingsOverride? overrides)
    {
        return new CKEditorComponentSettings
        {
            SetPath = string.IsNullOrWhiteSpace(overrides?.SetPath) ? defaults.SetPath : overrides.SetPath,
            CallbackName = string.IsNullOrWhiteSpace(overrides?.CallbackName) ? defaults.CallbackName : overrides.CallbackName,
            PickerAllowExt = string.IsNullOrWhiteSpace(overrides?.PickerAllowExt) ? defaults.PickerAllowExt : overrides.PickerAllowExt,
            PopupName = string.IsNullOrWhiteSpace(overrides?.PopupName) ? defaults.PopupName : overrides.PopupName
        };
    }

    private static FileManagerComponentSettings Merge(FileManagerComponentSettings defaults, FileManagerComponentSettingsOverride? overrides)
    {
        return new FileManagerComponentSettings
        {
            Root = overrides?.Root ?? defaults.Root,
            OnlyImages = overrides?.OnlyImages ?? defaults.OnlyImages,
            AllowExt = overrides?.AllowExt ?? defaults.AllowExt,
            Callback = overrides?.Callback ?? defaults.Callback
        };
    }
}

