# Web_FileManager Component Package

Tento repozitář obsahuje pouze komponentu FileManager určenou ke kopírování do jiných ASP.NET Core projektů.

## Zásadní pravidlo (bez výjimek)
Komponentu kopíruj **beze změn** (1:1), včetně názvů složek, souborů, namespace a route.

Nepřejmenovávej:
- `Areas/FileManager`
- `wwwroot/lib/Filemanager`
- route `/FileManager`
- API base `/api/filemanager`
- `instanceId` `main-filemanager-page`

## Co je v repozitáři
```text
.
├─ Areas/FileManager
│  ├─ Controllers/FileManagerApiController.cs
│  ├─ Pages/FileManager.cshtml
│  ├─ Pages/FileManager.cshtml.cs
│  ├─ Pages/_ViewImports.cshtml
│  ├─ Pages/_ViewStart.cshtml
│  ├─ Services/CustomFileManagerService.cs
│  ├─ Services/ComponentSettings/*
│  └─ docs/*
├─ wwwroot/lib/Filemanager
│  ├─ js/filemanager.js
│  ├─ css/*
│  ├─ ace/*
│  └─ file-icon-vectors/*
└─ components.settings.json
```

## Integrace do cílového projektu
### 1) Zkopíruj beze změn
- `Areas/FileManager`
- `wwwroot/lib/Filemanager`
- `components.settings.json`

### 2) Program.cs
V host projektu musí být:
```csharp
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddScoped<WebFileManager.Services.CustomFileManagerService>();
builder.Services.AddSingleton<
    WebFileManager.Services.ComponentSettings.IComponentSettingsProvider,
    WebFileManager.Services.ComponentSettings.JsonComponentSettingsProvider>();

app.MapControllers();
app.MapRazorPages();
```

### 3) Layout
Host layout musí umět sekce:
```cshtml
@await RenderSectionAsync("Styles", required: false)
@await RenderSectionAsync("Scripts", required: false)
```

### 4) Area _ViewStart
Pokud host projekt používá jiný layout path, uprav pouze `Areas/FileManager/Pages/_ViewStart.cshtml`.
Ostatní soubory nech 1:1.

## Parametry FileManageru (URL query)
Parametry můžeš posílat plain text nebo ve formátu `b64:<base64url(utf8)>`.

### Přepínače a aliasy
- `picker` | `select` : `1/true/yes` zapne picker mode.
- `onlyImages` | `onlyIMG` : omezení na obrázky.
- `allowExt` | `extensions` : seznam přípon oddělený čárkou (`jpg,png,pdf`).
- `root` | `setPath` | `r` : kořenová složka.
- `callback` | `f` : callback funkce ve `window.opener`.
- `setPath` | `p` : doplňkový path parametr pro integrace.

### Výchozí callback
- `ZachyceniURLCKeditor`

## components.settings.json
### FileManager instance
- `components.fileManager.defaults.root` : `string|null`
- `components.fileManager.defaults.onlyImages` : `bool|null`
- `components.fileManager.defaults.allowExt` : `string|null`
- `components.fileManager.defaults.callback` : `string|null`

- `components.fileManager.instances[].instanceId`
- `components.fileManager.instances[].pageScope`
- `components.fileManager.instances[].settings.root`
- `components.fileManager.instances[].settings.onlyImages`
- `components.fileManager.instances[].settings.allowExt`
- `components.fileManager.instances[].settings.callback`

### Souběžně podporované sekce (pro ostatní komponenty)
- `components.avatar.*`
- `components.ckEditor.*`

## API kontrakt
Base route: `/api/filemanager`

- `GET /list?path=&root=`
- `POST /create-folder` body:
```json
{ "path": "", "folderName": "NovaSlozka", "root": null }
```
- `POST /rename` body:
```json
{ "path": "a/b.txt", "newName": "c.txt", "isDirectory": false, "root": null }
```
- `POST /delete` body:
```json
{ "path": "a/b.txt", "isDirectory": false, "root": null }
```
- `POST /delete-multiple` body:
```json
{ "path": "a", "fileNames": ["1.txt"], "directoryNames": ["x"], "root": null }
```
- `POST /copy` body:
```json
{ "sourcePath": "a", "targetPath": "b", "fileNames": ["1.txt"], "directoryNames": ["x"], "move": false, "root": null }
```
- `POST /zip` body:
```json
{ "path": "a", "fileNames": ["1.txt"], "directoryNames": ["x"], "root": null }
```
- `POST /unzip` body:
```json
{ "path": "a", "zipFileName": "Zip_2026_01_01_120000.zip", "root": null }
```
- `GET /thumbnail?path=&fileName=&root=`
- `POST /upload?path=&root=` multipart form-data (`files`)
- `POST /save-text` body:
```json
{ "path": "a", "fileName": "b.txt", "content": "...", "root": null }
```

## Souběžné git projekty
Komponentu využívají nebo referencují:
- https://github.com/zukovVeliky/Web_FileManager
- https://github.com/zukovVeliky/Web_Avatar
- https://github.com/zukovVeliky/Web_CkEditor
- https://github.com/zukovVeliky/Web_FTP

## Pro AI asistenty
Když dostaneš úkol typu „integruj filemanager“:
1. Zkopíruj přesně 3 položky (`Areas/FileManager`, `wwwroot/lib/Filemanager`, `components.settings.json`).
2. Doplň registrace do `Program.cs` podle této dokumentace.
3. Ověř route `/FileManager` a endpoint `/api/filemanager/list`.
4. Neprováděj refaktoring cest ani namespace.
