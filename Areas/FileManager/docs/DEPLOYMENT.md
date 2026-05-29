# FileManager Deployment Checklist

Pouzij tento checklist po kazdem zkopirovani komponenty do noveho host projektu.

## 1. Soubory

- `Areas/FileManager` je zkopirovane 1:1
- `wwwroot/lib/Filemanager` je zkopirovane 1:1
- `components.settings.json` obsahuje sekci `components.fileManager`

## 2. Service registration

- `AddRazorPages()`
- `AddControllers()`
- `AddScoped<WebFileManager.Services.CustomFileManagerService>()`
- `AddSingleton<IComponentSettingsProvider, JsonComponentSettingsProvider>()`

## 3. Middleware a mapovani

- `UseStaticFiles()`
- `MapControllers()`
- `MapRazorPages()`

## 4. NuGet dependency

- `SixLabors.ImageSharp`
- `PDFtoImage`

Volitelne:

- LibreOffice pokud budes pouzivat DOCX -> PDF helper

## 5. Layout

- `Areas/FileManager/Pages/_ViewStart.cshtml` ukazuje na platny host layout
- host layout umi standardni Razor page render

Picker mode layout nepotrebuje:

- `/FileManager?picker=1`

## 6. Root policy

Aktualni verze povoluje zapis pouze do:

- `UzivatelskeSoubory`
- `Event`

Read-only picker lze pouzit i pro:

- `lib`

## 7. Upload limit

Pokud chces velke soubory, nastav:

```json
{
  "UploadLimits": {
    "MaxRequestBodyBytes": 2147483648
  }
}
```

## 8. Produkcni bezpecnost

Balicek sam o sobe neobsahuje host-specific role atributy.

Dopln v host projektu vlastni ochranu:

- `[Authorize]` nebo policy na page a API
- pripadne reverse proxy / ingress omezeni

## 9. Smoke test

1. Otevri `/FileManager`
2. Otestuj `GET /api/filemanager/list`
3. Vytvor slozku
4. Nahraj 1 obrazek
5. Nahraj 1 PDF
6. Otestuj thumbnail
7. Otestuj ZIP/UNZIP
8. Otestuj drag and drop move/copy
9. Otestuj picker mode

## 10. CKEditor binding smoke test

1. Otevri stranku s `Web_CkEditor`
2. Klikni toolbar FileManager button
3. Over popup `/FileManager?picker=1...`
4. Vyber obrazek
5. Over vlozeni obrazku do editoru
6. Vyber PDF nebo jiny soubor
7. Over vlozeni odkazu do editoru
