# FileManager Deployment Checklist

## 1. Povinné soubory v projektu
- `Areas/FileManager/Controllers/FileManagerApiController.cs`
- `Areas/FileManager/Pages/FileManager.cshtml`
- `Areas/FileManager/Pages/FileManager.cshtml.cs`
- `Areas/FileManager/Pages/_ViewStart.cshtml`
- `Areas/FileManager/Services/CustomFileManagerService.cs`
- `Areas/FileManager/Services/ComponentSettings/*`
- `wwwroot/Lib/Filemanager/*`
- `components.settings.json`

## 2. Registrace v Program.cs
```csharp
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddScoped<RazorFileManagerApp.Services.CustomFileManagerService>();
builder.Services.AddSingleton<RazorFileManagerApp.Services.ComponentSettings.IComponentSettingsProvider, RazorFileManagerApp.Services.ComponentSettings.JsonComponentSettingsProvider>();

app.MapControllers();
app.MapRazorPages();
```

## 3. Layout
Layout musí obsahovat:
- `@await RenderSectionAsync("Styles", required: false)`
- `@await RenderSectionAsync("Scripts", required: false)`

## 4. Smoke test
1. Spusť aplikaci.
2. Otevři `/FileManager`.
3. Ověř: načtení seznamu, vytvoření složky, upload souboru, rename, delete.
4. Ověř editor textového souboru.
5. Ověř API endpoint `GET /api/filemanager/list`.

## 5. Nejčastější chyby
- Nezobrazené styly: chybí `_ViewStart.cshtml` v `Areas/FileManager/Pages`.
- 404 na API: chybí `AddControllers()` nebo `MapControllers()`.
- Chybné namespace po kopii: nesedí jméno projektu ve `using`.
