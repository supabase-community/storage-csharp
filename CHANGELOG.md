# Changelog

## 1.2.2 - 11-10-2022

- Clarifies `IStorageClient` as implementing `IStorageBucket`

## 1.2.1 - 11-10-2022

- Expose `StorageBucketApi.Headers` as a public property.

## 1.2.0 - 11-4-2022

- [#2](https://github.com/supabase-community/storage-csharp/issues/2) Restructure Library to support Dependency Injection (DI)
- Enable nullability in the project and make use of nullable reference types.

## 1.1.1 - 07-17-2022

- Fix missing API change on `Update` method of `StorageFileApi`

## 1.1.0 - 07-17-2022

- API Change [Breaking/Minor] Library no longer uses `WebClient` and instead leverages `HttpClient`. Progress events on `Upload` and `Download` are now handled with `EventHandler<float>` instead of `WebClient` EventHandlers.

## 1.0.2 - 02-27-2022

- Add `CreatedSignedUrls` method.

## 1.0.1 - 12-9-2021

- Add missing support for `X-Client-Info`

## 1.0.0 - 12-9-2021

- Initial release of separated storage client