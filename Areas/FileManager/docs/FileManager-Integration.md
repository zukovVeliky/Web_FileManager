# Integration Notes

Tato komponenta je distribuovana jako copy package. Pri integraci zachovej puvodni strukturu, route a namespace.

## Cílové umístění

- server: `Areas/FileManager`
- klient: `wwwroot/lib/Filemanager`
- config: `components.settings.json`

## Povinne invarianty

- route `/FileManager`
- API `/api/filemanager`
- namespace `WebFileManager.*`
- `instanceId` `main-filemanager-page`

## Kompatibilita s dalsimi komponentami

FileManager je pripraveny na integraci s:

- `Web_CkEditor`
- `Web_Avatar`
- vlastnimi popup klienty zalozenymi na callbacku

## Picker contract

Klient otevre popup:

```text
/FileManager?picker=1&root=b64:...&allowExt=b64:...&callback=b64:...
```

Po potvrzeni vyberu FileManager vola:

```js
window.opener[callbackName](absoluteUrl, fileName);
```

## Typicke rooty

- `UzivatelskeSoubory`
- `UzivatelskeSoubory/Articles`
- `Event/123`
- `wwwroot/lib/...` pouze read-only picker

## Poznamky k produkci

- pokud host layout pouziva jinou cestu, uprav jen `_ViewStart.cshtml`
- pokud potrebujes auth, dopln ji v host projektu
- pokud potrebujes zmenit povolene rooty, uprav `CustomFileManagerService`
