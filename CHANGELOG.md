# Changelog

## 1.2.8 - 03-14-2023

- [Merge #5](https://github.com/supabase-community/storage-csharp/pull/5) Added search string as an optional search parameter. Thanks [@ElectroKnight22](https://github.com/ElectroKnight22)!

## 1.2.7 - 03-02-2023

- Fix incorrect namespacing for Supabase.Storage.ClientOptions.

## 1.2.6 - 03-02-2023

- Re: [#4](https://github.com/supabase-community/storage-csharp/issues/4) Implementation for `ClientOptions` which supports specifying Upload, Download, and Request timeouts.

## 1.2.5 - 02-28-2023

- Provides fix for [supabase-community/supabase-csharp#54](https://github.com/supabase-community/supabase-csharp/issues/54) - Dynamic headers were always being overwritten by initialized headers, so the storage client would not receive user's access token as expected.
- Provides fix for upload progress not reporting in [supabase-community/storage-csharp#3](https://github.com/supabase-community/storage-csharp/issues/3)

## 1.2.4 - 02-26-2023

- `UploadOrUpdate` now appropriately throws request exception if server returns a bad status code.

## 1.2.3 - 11-12-2022

- Use `supabase-core` and implement `IGettableHeaders` on `Client`
- `Client` no longer has `headers` as a required parameter.

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