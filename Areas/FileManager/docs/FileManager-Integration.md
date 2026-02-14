# FileManager Integration Guide

Tento dokument je aktuální pro strukturu s oddělením na serverovou a klientskou větev:
- server: `Areas/FileManager`
- klient: `wwwroot/Lib/Filemanager`

## 1. Cílová architektura
```text
Areas/FileManager
  Controllers/FileManagerApiController.cs
  Pages/FileManager.cshtml
  Pages/FileManager.cshtml.cs
  Services/CustomFileManagerService.cs
  Services/ComponentSettings/*

wwwroot/Lib/Filemanager
  js/filemanager.js
  css/*
  ace/*
  file-icon-vectors/*
```

## 2. Route a API
- UI: `/FileManager`
- API base: `/api/filemanager`

Používané endpointy:
- `GET /api/filemanager/list`
- `POST /api/filemanager/create-folder`
- `POST /api/filemanager/rename`
- `POST /api/filemanager/delete`
- `POST /api/filemanager/delete-multiple`
- `POST /api/filemanager/copy`
- `POST /api/filemanager/zip`
- `POST /api/filemanager/unzip`
- `POST /api/filemanager/upload`
- `GET /api/filemanager/thumbnail`
- `POST /api/filemanager/save-text`

## 3. Co zapojit v hostitelské app
### 3.1 Program.cs
```csharp
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddScoped<YourProject.Services.CustomFileManagerService>();
builder.Services.AddSingleton<YourProject.Services.ComponentSettings.IComponentSettingsProvider, YourProject.Services.ComponentSettings.JsonComponentSettingsProvider>();

app.MapControllers();
app.MapRazorPages();
```

### 3.2 Layout
Musí renderovat sekce `Styles` a `Scripts`.

### 3.3 Area pages
V `Areas/FileManager/Pages` musí existovat `_ViewStart.cshtml` s layoutem `/Pages/Shared/_Layout.cshtml`.

## 4. Konfigurační soubor
`components.settings.json` obsahuje výchozí nastavení pro instance FileManageru.

Relevantní instance:
- `instanceId`: `main-filemanager-page`
- `pageScope`: `/FileManager`

## 5. Integrace do dalších projektů
FileManager je používán v souběžných repozitářích:
- Web_FileManager: https://github.com/zukovVeliky/Web_FileManager
- Web_Avatar: https://github.com/zukovVeliky/Web_Avatar
- Web_CkEditor: https://github.com/zukovVeliky/Web_CkEditor
- Web_FTP: https://github.com/zukovVeliky/Web_FTP

Tyto odkazy drž tento dokument aktuální i při dalších refaktorech.

## 6. Poznámky
- Thumbnail pipeline používá `System.Drawing.Common`.
- Na Linux/macOS je potřeba řešit alternativu (ImageSharp/SkiaSharp), pokud chceš plnou multiplatformnost.
