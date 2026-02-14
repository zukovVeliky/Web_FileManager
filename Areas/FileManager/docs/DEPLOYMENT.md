# Deployment Checklist (Component-Only)

## Povinně zkopírovat 1:1
- `Areas/FileManager`
- `wwwroot/lib/Filemanager`
- `components.settings.json`

## Program.cs
```csharp
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddScoped<WebFileManager.Services.CustomFileManagerService>();
builder.Services.AddSingleton<WebFileManager.Services.ComponentSettings.IComponentSettingsProvider, WebFileManager.Services.ComponentSettings.JsonComponentSettingsProvider>();

app.MapControllers();
app.MapRazorPages();
```

## Layout
- musí renderovat `Styles` a `Scripts` sekci.

## Ověření
- `/FileManager` renderuje UI
- `GET /api/filemanager/list` vrací JSON
- upload/create-folder/rename/delete fungují
