# Changelog

## 1.1.0 - 07-17-2022

- API Change [Breaking/Minor] Library no longer uses `WebClient` and instead leverages `HttpClient`. Progress events on `Upload` and `Download` are now handled with `EventHandler<float>` instead of `WebClient` EventHandlers.

## 1.0.2 - 02-27-2022

- Add `CreatedSignedUrls` method.

## 1.0.1 - 12-9-2021

- Add missing support for `X-Client-Info`

## 1.0.0 - 12-9-2021

- Initial release of separated storage client