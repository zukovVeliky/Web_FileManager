# AI Integration Playbook

Tento dokument je psany tak, aby podle nej mohl komponentu integrovat clovek i AI klient bez znalosti puvodniho host projektu.

## Cíl

Nasadit FileManager jako samostatnou komponentu a zprovoznit picker workflow pro CKEditor nebo jiny popup klient.

## Udělej přesně toto

1. Zkopiruj `Areas/FileManager`.
2. Zkopiruj `wwwroot/lib/Filemanager`.
3. Zkopiruj `components.settings.json` nebo alespon mergni sekci `components.fileManager`.
4. Zaregistruj `CustomFileManagerService` a `JsonComponentSettingsProvider` v `Program.cs`.
5. Zapni `MapControllers()` a `MapRazorPages()`.
6. Over route `/FileManager`.
7. Over endpoint `/api/filemanager/list`.

## Neprováděj

- neprejmenovavej `Filemanager` na `FileManager` v ceste do `wwwroot`
- nemen route `/FileManager`
- nemen base API `/api/filemanager`
- nedelej refaktor namespace `WebFileManager.*`

## Pokud integrujes s CKEditorem

1. Ujisti se, ze CKEditor generuje unikatni `callbackName`.
2. Otevirej picker s:
   - `picker=1`
   - `root`
   - `allowExt`
   - `callback`
3. Over, ze popup muze volat `window.opener[callbackName](url, fileName)`.

## Pokud integrujes s vlastnim formularem

Pouzij stejny kontrakt callbacku:

```js
function MyFilePickerCallback(url, fileName) {
    document.getElementById('TargetUrl').value = url;
    document.getElementById('TargetName').value = fileName;
}
```

Pak otevri:

```text
/FileManager?picker=1&callback=b64:...
```

## Diagnostika

- popup se neotevrel:
  - blokuje ho browser
- popup se otevrel, ale nic nevraci:
  - neexistuje `window.opener`
  - callback jmeno nesouhlasi
- upload vraci 403:
  - root neni v povolene write oblasti
- thumbnail vraci original:
  - generovani miniatury selhalo, fallback je zamerne aktivni
