# RazorFileManagerApp

Referenční ASP.NET Core Razor Pages aplikace s integrovanou komponentou FileManager.

## Co projekt obsahuje
- Hotovou stránku FileManageru na route `/FileManager`.
- API endpointy pro práci se soubory na `/api/filemanager/*`.
- Konfiguraci komponent přes `components.settings.json`.
- Klientskou část (JS/CSS/Ace/icon set) odděleně ve `wwwroot/Lib/Filemanager`.

## Aktuální struktura
```text
RazorFileManagerApp
├─ Areas/FileManager
│  ├─ Controllers/FileManagerApiController.cs
│  ├─ Pages/FileManager.cshtml
│  ├─ Pages/FileManager.cshtml.cs
│  └─ Services
│     ├─ CustomFileManagerService.cs
│     └─ ComponentSettings/*
├─ wwwroot/Lib/Filemanager
│  ├─ js/filemanager.js
│  ├─ css/*
│  ├─ ace/*
│  └─ file-icon-vectors/*
├─ Program.cs
└─ components.settings.json
```

## Rychlý start
1. `dotnet restore`
2. `dotnet build`
3. `dotnet run`
4. otevři `/FileManager`

## Zapojení do jiného projektu (člověk i AI)
Zkopíruj přesně tyto části:
- `Areas/FileManager`
- `wwwroot/Lib/Filemanager`
- `components.settings.json`

V cílovém `Program.cs` musí být:
```csharp
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddScoped<YourProject.Services.CustomFileManagerService>();
builder.Services.AddSingleton<YourProject.Services.ComponentSettings.IComponentSettingsProvider, YourProject.Services.ComponentSettings.JsonComponentSettingsProvider>();

app.MapControllers();
app.MapRazorPages();
```

V layoutu musí být možnost renderovat sekce:
```cshtml
@await RenderSectionAsync("Styles", required: false)
@await RenderSectionAsync("Scripts", required: false)
```

A u pages v area musí být `_ViewStart.cshtml` s layoutem:
```cshtml
@{
    Layout = "/Pages/Shared/_Layout.cshtml";
}
```

## Konfigurace
Soubor `components.settings.json` řídí výchozí parametry pro FileManager (root, filtrování přípon, callback pro picker režim).

## Git poznámky
- Projekt používá `.gitignore` pro .NET artefakty (`bin/`, `obj/`, `.vs/`).
- Commituj pouze zdrojové soubory a dokumentaci.

## Souběžné projekty využívající FileManager
Zachované odkazy na repozitáře:
- Web_FileManager: https://github.com/zukovVeliky/Web_FileManager
- Web_Avatar: https://github.com/zukovVeliky/Web_Avatar
- Web_CkEditor: https://github.com/zukovVeliky/Web_CkEditor
- Web_FTP (hostitelský/integrační projekt): https://github.com/zukovVeliky/Web_FTP

## Poznámka k platformě
`CustomFileManagerService` používá `System.Drawing` pro thumbnail, což je primárně Windows scénář.
