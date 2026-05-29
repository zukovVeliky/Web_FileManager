# Web_FileManager Component Package

Znovupouzitelny webovy FileManager pro ASP.NET Core Razor Pages a MVC. Balicek je navrzeny jako copy package: zkopiruj vybrane slozky do host projektu pri zachovani puvodnich cest, route a namespace.

Aktualni verze obsahuje:

- picker mode pro integraci s CKEditorem a dalsimi popup klienty
- detailni a tile view
- upload vice souboru s progress barem a retry logikou
- drag and drop presuny a kopirovani
- editaci textovych souboru v Ace editoru
- obrazkove preview a generovani thumbnailu
- ZIP a UNZIP operace
- export prvni stranky PDF do JPG
- filtrovani rootu na read/write a read-only oblasti

## Co zkopirovat 1:1

- `Areas/FileManager`
- `wwwroot/lib/Filemanager`
- `components.settings.json`

Neměn:

- `Areas/FileManager`
- `wwwroot/lib/Filemanager`
- route `/FileManager`
- API base `/api/filemanager`
- namespace `WebFileManager.*`
- `instanceId` `main-filemanager-page`

## Struktura balicku

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

## Host projekt: minimalni pozadavky

- ASP.NET Core Razor Pages
- `AddControllers()` kvuli API endpointum
- staticke soubory `UseStaticFiles()`
- Bootstrap, Bootstrap Icons a jQuery v host projektu nebo v picker layoutu
- NuGet balicky:
  - `SixLabors.ImageSharp`
  - `PDFtoImage`
- volitelne LibreOffice pokud chces pouzivat helper pro DOCX -> PDF

## Program.cs

V host projektu musi byt aspon:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddScoped<WebFileManager.Services.CustomFileManagerService>();
builder.Services.AddSingleton<
    WebFileManager.Services.ComponentSettings.IComponentSettingsProvider,
    WebFileManager.Services.ComponentSettings.JsonComponentSettingsProvider>();

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();
app.MapRazorPages();
```

Doporucene nastaveni pro velke uploady:

```json
{
  "UploadLimits": {
    "MaxRequestBodyBytes": 2147483648
  }
}
```

## Layout a picker mode

Standardni page render pouziva layout definovany v `Areas/FileManager/Pages/_ViewStart.cshtml`.

Pokud projekt pouziva jinou cestu k layoutu:

- uprav pouze `Areas/FileManager/Pages/_ViewStart.cshtml`
- ostatni soubory nech beze zmen

Picker mode layout obchazi automaticky:

- `/FileManager?picker=1`

V picker modu si page sama natahne Bootstrap, Bootstrap Icons, jQuery a FileManager assets, aby fungovala i v samostatnem popup okne.

## Bezpecnost a rooty

Balicek zamerne neresi konkretni autorizacni role host projektu. Zabezpeceni route a API si dopln v host aplikaci podle vlastni identity vrstvy.

Read/write root prefixy v aktualni verzi:

- `UzivatelskeSoubory`
- `Event`

Read-only root prefixy:

- `lib`

To znamena:

- zapis mimo `UzivatelskeSoubory` a `Event` je blokovan
- `wwwroot/lib/...` lze pouzit jako read-only picker root
- `root=wwwroot` nebo `root=wwwroot/lib/...` je vhodne pro vyber statickych assetu

## URL parametry

Parametry muzes posilat plain text nebo ve formatu `b64:<base64url(utf8)>`.

- `picker` nebo `select`: `1`, `true`, `yes` zapne picker mode
- `onlyImages` nebo `onlyIMG`: omezeni na obrazky
- `allowExt` nebo `extensions`: povolene pripony oddelene carkou
- `root` nebo `setPath` nebo `r`: root slozka
- `callback` nebo `f`: callback funkce ve `window.opener`
- `setPath` nebo `p`: doplnkovy parametr pro stare integrace

Vychozi callback:

- `ZachyceniURLCKeditor`

## API kontrakt

Base route: `/api/filemanager`

- `GET /list?path=&root=`
- `POST /create-folder`
- `POST /rename`
- `POST /delete`
- `POST /delete-multiple`
- `POST /copy`
- `POST /zip`
- `POST /unzip`
- `GET /thumbnail?path=&fileName=&root=`
- `POST /upload?path=&root=&onlyImages=&allowExt=` multipart form-data `files`
- `POST /save-text`
- `POST /export-pdf-jpg`

### Create folder

```json
{ "path": "", "folderName": "NovaSlozka", "root": null }
```

### Rename

```json
{ "path": "a/b.txt", "newName": "c.txt", "isDirectory": false, "root": null }
```

### Delete

```json
{ "path": "a/b.txt", "isDirectory": false, "root": null }
```

### Delete multiple

```json
{ "path": "a", "fileNames": ["1.txt"], "directoryNames": ["x"], "root": null }
```

### Copy or move

```json
{
  "sourcePath": "a",
  "targetPath": "b",
  "fileNames": ["1.txt"],
  "directoryNames": ["x"],
  "move": false,
  "root": null
}
```

### Zip

```json
{ "path": "a", "fileNames": ["1.txt"], "directoryNames": ["x"], "root": null }
```

### Unzip

```json
{ "path": "a", "zipFileName": "Zip_2026_01_01_120000.zip", "root": null }
```

### Save text

```json
{ "path": "a", "fileName": "b.txt", "content": "...", "root": null }
```

### Export PDF first page to JPG

```json
{ "path": "a", "fileName": "brochure.pdf", "root": null }
```

## components.settings.json

FileManager cte konfiguraci pres `components.fileManager.*`.

Zakladni shape:

```json
{
  "components": {
    "fileManager": {
      "defaults": {
        "root": null,
        "onlyImages": null,
        "allowExt": null,
        "callback": null
      },
      "instances": [
        {
          "instanceId": "main-filemanager-page",
          "pageScope": "/FileManager",
          "settings": {
            "root": null,
            "onlyImages": null,
            "allowExt": null,
            "callback": null
          }
        }
      ]
    }
  }
}
```

Dulezite:

- `root` nastavuj jen na povolene root prefixy
- `allowExt` je comma-separated string
- `callback` dava smysl hlavne pro picker instanci

## Propojeni s CKEditorem

FileManager picker je navrzen tak, aby fungoval primo s `Web_CkEditor`.

CKEditor typicky otevre:

```text
/FileManager?picker=1&root=b64:...&allowExt=b64:...&callback=b64:...
```

Po vyberu souboru FileManager zavola v popup openeru:

```js
window[callbackName](url, fileName);
```

Stejne callback schema muzes pouzit i pro vlastni klienty mimo CKEditor.

## Smoke test

1. Otevri `/FileManager`.
2. Over `GET /api/filemanager/list`.
3. Vytvor slozku.
4. Nahraj obrazek a sleduj progress.
5. Otevri obrazek v preview.
6. Nahraj PDF a otestuj `Exportovat do JPG`.
7. Otestuj picker mode pres `/FileManager?picker=1&onlyImages=1`.
8. Otestuj callback do opener window.

## Dokumentace

- deployment checklist: [Areas/FileManager/docs/DEPLOYMENT.md](Areas/FileManager/docs/DEPLOYMENT.md)
- integration notes: [Areas/FileManager/docs/FileManager-Integration.md](Areas/FileManager/docs/FileManager-Integration.md)
- AI playbook: [Areas/FileManager/docs/AI-INTEGRATION.md](Areas/FileManager/docs/AI-INTEGRATION.md)

## Souvisejici repozitare

- https://github.com/zukovVeliky/Web_FileManager
- https://github.com/zukovVeliky/Web_CkEditor
- https://github.com/zukovVeliky/Web_Avatar
- https://github.com/zukovVeliky/Web_FTP
