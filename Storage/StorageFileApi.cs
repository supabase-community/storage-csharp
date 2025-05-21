﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Supabase.Storage.Exceptions;
using Supabase.Storage.Extensions;
using Supabase.Storage.Interfaces;
using Supabase.Storage.Responses;

namespace Supabase.Storage
{
    public class StorageFileApi : IStorageFileApi<FileObject>
    {
        public ClientOptions Options { get; protected set; }
        protected string Url { get; set; }
        protected Dictionary<string, string> Headers { get; set; }
        protected string? BucketId { get; set; }

        public StorageFileApi(string url, string bucketId, ClientOptions? options,
            Dictionary<string, string>? headers = null) : this(url, headers, bucketId)
        {
            Options = options ?? new ClientOptions();
        }

        public StorageFileApi(string url, Dictionary<string, string>? headers = null, string? bucketId = null)
        {
            Url = url;
            BucketId = bucketId;
            Options ??= new ClientOptions();
            Headers = headers ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// A simple convenience function to get the URL for an asset in a public bucket.If you do not want to use this function, you can construct the public URL by concatenating the bucket URL with the path to the asset.
        /// This function does not verify if the bucket is public. If a public URL is created for a bucket which is not public, you will not be able to download the asset.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="transformOptions"></param>
        /// <returns></returns>
        public string GetPublicUrl(string path, TransformOptions? transformOptions)
        {
            if (transformOptions == null)
                return $"{Url}/object/public/{GetFinalPath(path)}";

            var builder = new UriBuilder($"{Url}/render/image/public/{GetFinalPath(path)}")
            {
                Query = transformOptions.ToQueryCollection().ToString()
            };

            return builder.ToString();
        }

        /// <summary>
        /// Create signed url to download file without requiring permissions. This URL can be valid for a set number of seconds.
        /// </summary>
        /// <param name="path">The file path to be downloaded, including the current file name. For example `folder/image.png`.</param>
        /// <param name="expiresIn">The number of seconds until the signed URL expires. For example, `60` for a URL which is valid for one minute.</param>
        /// <param name="transformOptions"></param>
        /// <returns></returns>
        public async Task<string> CreateSignedUrl(string path, int expiresIn, TransformOptions? transformOptions = null)
        {
            var body = new Dictionary<string, object?> { { "expiresIn", expiresIn } };
            var url = $"{Url}/object/sign/{GetFinalPath(path)}";

            if (transformOptions != null)
            {
                var transformOptionsJson = JsonConvert.SerializeObject(transformOptions, new StringEnumConverter());
                var transformOptionsObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(transformOptionsJson);
                body.Add("transform", transformOptionsObj);
            }

            var response = await Helpers.MakeRequest<CreateSignedUrlResponse>(HttpMethod.Post, url, body, Headers);

            if (response == null || string.IsNullOrEmpty(response.SignedUrl))
                throw new SupabaseStorageException(
                    $"Signed Url for {path} returned empty, do you have permission?");

            return $"{Url}{response?.SignedUrl}";
        }

        /// <summary>
        /// Create signed URLs to download files without requiring permissions. These URLs can be valid for a set number of seconds.
        /// </summary>
        /// <param name="paths">paths The file paths to be downloaded, including the current file names. For example [`folder/image.png`, 'folder2/image2.png'].</param>
        /// <param name="expiresIn">The number of seconds until the signed URLs expire. For example, `60` for URLs which are valid for one minute.</param>
        /// <returns></returns>
        public async Task<List<CreateSignedUrlsResponse>?> CreateSignedUrls(List<string> paths, int expiresIn)
        {
            var body = new Dictionary<string, object> { { "expiresIn", expiresIn }, { "paths", paths } };
            var response = await Helpers.MakeRequest<List<CreateSignedUrlsResponse>>(HttpMethod.Post,
                $"{Url}/object/sign/{BucketId}", body, Headers);

            if (response != null)
            {
                foreach (var item in response)
                {
                    if (string.IsNullOrEmpty(item.SignedUrl))
                        throw new SupabaseStorageException(
                            $"Signed Url for {item.Path} returned empty, do you have permission?");

                    item.SignedUrl = $"{Url}{item.SignedUrl}";
                }
            }

            return response;
        }

        /// <summary>
        /// Lists all the files within a bucket.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<List<FileObject>?> List(string path = "", SearchOptions? options = null)
        {
            options ??= new SearchOptions();

            var json = JsonConvert.SerializeObject(options);
            var body = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (body != null)
                body.Add("prefix", string.IsNullOrEmpty(path) ? "" : path);

            var response =
                await Helpers.MakeRequest<List<FileObject>>(HttpMethod.Post, $"{Url}/object/list/{BucketId}", body,
                    Headers);

            return response;
        }

        /// <summary>
        /// Uploads a file to an existing bucket.
        /// </summary>
        /// <param name="localFilePath">File Source Path</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options"></param>
        /// <param name="onProgress"></param>
        /// <param name="inferContentType"></param>
        /// <returns></returns>
        public async Task<string> Upload(string localFilePath, string supabasePath, FileOptions? options = null,
            EventHandler<float>? onProgress = null, bool inferContentType = true)
        {
            options ??= new FileOptions();

            if (inferContentType)
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(localFilePath);

            var result = await UploadOrUpdate(localFilePath, supabasePath, options, onProgress);
            return result;
        }

        /// <summary>
        /// Uploads a byte array to an existing bucket.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options"></param>
        /// <param name="onProgress"></param>
        /// <param name="inferContentType"></param>
        /// <returns></returns>
        public async Task<string> Upload(byte[] data, string supabasePath, FileOptions? options = null,
            EventHandler<float>? onProgress = null, bool inferContentType = true)
        {
            options ??= new FileOptions();

            if (inferContentType)
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(supabasePath);

            var result = await UploadOrUpdate(data, supabasePath, options, onProgress);
            return result;
        }

        /// <summary>
        /// Uploads a file to using a pre-generated Signed Upload Url
        /// </summary>
        /// <param name="localFilePath">File Source Path</param>
        /// <param name="signedUrl"></param>
        /// <param name="options"></param>
        /// <param name="onProgress"></param>
        /// <param name="inferContentType"></param>
        /// <returns></returns>
        public async Task<string> UploadToSignedUrl(string localFilePath, UploadSignedUrl signedUrl,
            FileOptions? options = null, EventHandler<float>? onProgress = null, bool inferContentType = true)
        {
            options ??= new FileOptions();

            if (inferContentType)
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(localFilePath);

            var headers = new Dictionary<string, string>(Headers)
            {
                ["Authorization"] = $"Bearer {signedUrl.Token}",
                ["cache-control"] = $"max-age={options.CacheControl}",
                ["content-type"] = options.ContentType
            };

            if (options.Upsert)
                headers.Add("x-upsert", options.Upsert.ToString().ToLower());

            var progress = new Progress<float>();

            if (onProgress != null)
                progress.ProgressChanged += onProgress;

            await Helpers.HttpUploadClient!.UploadFileAsync(signedUrl.SignedUrl, localFilePath, headers, progress);

            return GetFinalPath(signedUrl.Key);
        }

        /// <summary>
        /// Uploads a byte array using a pre-generated Signed Upload Url
        /// </summary>
        /// <param name="data"></param>
        /// <param name="signedUrl"></param>
        /// <param name="options"></param>
        /// <param name="onProgress"></param>
        /// <param name="inferContentType"></param>
        /// <returns></returns>
        public async Task<string> UploadToSignedUrl(byte[] data, UploadSignedUrl signedUrl, FileOptions? options = null,
            EventHandler<float>? onProgress = null, bool inferContentType = true)
        {
            options ??= new FileOptions();

            if (inferContentType)
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(signedUrl.Key);

            var headers = new Dictionary<string, string>(Headers)
            {
                ["Authorization"] = $"Bearer {signedUrl.Token}",
                ["cache-control"] = $"max-age={options.CacheControl}",
                ["content-type"] = options.ContentType
            };

            if (options.Upsert)
                headers.Add("x-upsert", options.Upsert.ToString().ToLower());

            var progress = new Progress<float>();

            if (onProgress != null)
                progress.ProgressChanged += onProgress;

            await Helpers.HttpUploadClient!.UploadBytesAsync(signedUrl.SignedUrl, data, headers, progress);

            return GetFinalPath(signedUrl.Key);
        }


        /// <summary>
        /// Replaces an existing file at the specified path with a new one.
        /// </summary>
        /// <param name="localFilePath">File source path.</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options">HTTP headers.</param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<string> Update(string localFilePath, string supabasePath, FileOptions? options = null,
            EventHandler<float>? onProgress = null)
        {
            options ??= new FileOptions();
            return UploadOrUpdate(localFilePath, supabasePath, options, onProgress);
        }

        /// <summary>
        /// Replaces an existing file at the specified path with a new one.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options">HTTP headers.</param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<string> Update(byte[] data, string supabasePath, FileOptions? options = null,
            EventHandler<float>? onProgress = null)
        {
            options ??= new FileOptions();
            return UploadOrUpdate(data, supabasePath, options, onProgress);
        }

        /// <summary>
        /// Moves an existing file to a new location, optionally allowing renaming.
        /// </summary>
        /// <param name="fromPath">The original file path, including the current file name (e.g., `folder/image.png`).</param>
        /// <param name="toPath">The target file path, including the new file name (e.g., `folder/image-copy.png`).</param>
        /// <param name="options">Optional parameters for specifying the destination bucket and other settings.</param>
        /// <returns>Returns a boolean value indicating whether the operation was successful.</returns>
        public async Task<bool> Move(string fromPath, string toPath, DestinationOptions? options = null)
        {
            var body = new Dictionary<string, string?>
            {
                { "bucketId", BucketId },
                { "sourceKey", fromPath },
                { "destinationKey", toPath },
                { "destinationBucket", options?.DestinationBucket }
            };
            await Helpers.MakeRequest<GenericResponse>(HttpMethod.Post, $"{Url}/object/move", body, Headers);
            return true;
        }

        /// <summary>
        /// Copies a file/object from one path to another within a bucket or across buckets.
        /// </summary>
        /// <param name="fromPath">The source path of the file/object to copy.</param>
        /// <param name="toPath">The destination path for the copied file/object.</param>
        /// <param name="options">Optional parameters such as the destination bucket.</param>
        /// <returns>True if the copy operation was successful.</returns>
        public async Task<bool> Copy(string fromPath, string toPath, DestinationOptions? options = null)
        {
            var body = new Dictionary<string, string?>
            {
                { "bucketId", BucketId },
                { "sourceKey", fromPath },
                { "destinationKey", toPath },
                { "destinationBucket", options?.DestinationBucket }
            };

            await Helpers.MakeRequest<GenericResponse>(HttpMethod.Post, $"{Url}/object/copy", body, Headers);
            return true;
        }

        /// <summary>
        /// Downloads a file from a private bucket. For public buckets, use <see cref="DownloadPublicFile(string, string, TransformOptions?, EventHandler{float}?)"/>
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="localPath"></param>
        /// <param name="transformOptions"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<string> Download(string supabasePath, string localPath, TransformOptions? transformOptions = null,
            EventHandler<float>? onProgress = null)
        {
            var url = transformOptions != null
                ? $"{Url}/render/image/authenticated/{GetFinalPath(supabasePath)}"
                : $"{Url}/object/{GetFinalPath(supabasePath)}";
            return DownloadFile(url, localPath, transformOptions, onProgress);
        }

        /// <summary>
        /// Downloads a file from a private bucket. For public buckets, use <see cref="DownloadPublicFile(string, string, TransformOptions?, EventHandler{float}?)"/>
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="localPath"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<string> Download(string supabasePath, string localPath, EventHandler<float>? onProgress = null) =>
            Download(supabasePath, localPath, null, onProgress: onProgress);

        /// <summary>
        /// Downloads a byte array from a private bucket to be used programmatically. For public buckets <see cref="DownloadPublicFile(string, TransformOptions?, EventHandler{float}?)"/>
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="transformOptions"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<byte[]> Download(string supabasePath, TransformOptions? transformOptions = null,
            EventHandler<float>? onProgress = null)
        {
            var url = $"{Url}/object/{GetFinalPath(supabasePath)}";
            return DownloadBytes(url, transformOptions, onProgress);
        }

        /// <summary>
        /// Downloads a byte array from a private bucket to be used programmatically. For public buckets <see cref="DownloadPublicFile(string, TransformOptions?, EventHandler{float}?)"/>
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<byte[]> Download(string supabasePath, EventHandler<float>? onProgress = null) =>
            Download(supabasePath, transformOptions: null, onProgress: onProgress);

        /// <summary>
        /// Downloads a public file to the filesystem. This method DOES NOT VERIFY that the file is actually public.
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="localPath"></param>
        /// <param name="transformOptions"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<string> DownloadPublicFile(string supabasePath, string localPath,
            TransformOptions? transformOptions = null, EventHandler<float>? onProgress = null)
        {
            var url = GetPublicUrl(supabasePath, transformOptions);
            return DownloadFile(url, localPath, transformOptions, onProgress);
        }

        /// <summary>
        /// Downloads a byte array from a private bucket to be used programmatically. This method DOES NOT VERIFY that the file is actually public.
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="transformOptions"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<byte[]> DownloadPublicFile(string supabasePath, TransformOptions? transformOptions = null,
            EventHandler<float>? onProgress = null)
        {
            var url = GetPublicUrl(supabasePath, transformOptions);
            return DownloadBytes(url, transformOptions, onProgress);
        }

        /// <summary>
        /// Deletes file within the same bucket
        /// </summary>
        /// <param name="path">A path to delete, for example `folder/image.png`.</param>
        /// <returns></returns>
        public async Task<FileObject?> Remove(string path)
        {
            var result = await Remove(new List<string> { path });
            return result?.FirstOrDefault();
        }

        /// <summary>
        /// Deletes files within the same bucket
        /// </summary>
        /// <param name="paths">An array of files to be deletes, including the path and file name. For example [`folder/image.png`].</param>
        /// <returns></returns>
        public async Task<List<FileObject>?> Remove(List<string> paths)
        {
            var data = new Dictionary<string, object> { { "prefixes", paths } };
            var response =
                await Helpers.MakeRequest<List<FileObject>>(HttpMethod.Delete, $"{Url}/object/{BucketId}", data,
                    Headers);

            return response;
        }

        /// <summary>
        /// Creates an upload signed URL. Use it to upload a file straight to the bucket without credentials
        /// </summary>
        /// <param name="supabasePath">The file path, including the current file name. For example `folder/image.png`.</param>
        /// <returns></returns>
        public async Task<UploadSignedUrl> CreateUploadSignedUrl(string supabasePath)
        {
            var path = GetFinalPath(supabasePath);

            var url = $"{Url}/object/upload/sign/{path}";
            var response =
                await Helpers.MakeRequest<CreatedUploadSignedUrlResponse>(HttpMethod.Post, url, null, Headers);

            if (response == null || string.IsNullOrEmpty(response.Url) || !response.Url!.Contains("token"))
                throw new SupabaseStorageException(
                    "Response did not return with expected data. Does this token have proper permission to generate a url?");

            var generatedUri = new Uri($"{Url}{response.Url}");
            var query = HttpUtility.ParseQueryString(generatedUri.Query);
            var token = query["token"];

            return new UploadSignedUrl(generatedUri, token, supabasePath);
        }

        private async Task<string> UploadOrUpdate(string localPath, string supabasePath, FileOptions options,
            EventHandler<float>? onProgress = null)
        {
            Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

            var headers = new Dictionary<string, string>(Headers)
            {
                { "cache-control", $"max-age={options.CacheControl}" },
                { "content-type", options.ContentType }
            };

            if (options.Upsert)
                headers.Add("x-upsert", options.Upsert.ToString().ToLower());

            var progress = new Progress<float>();

            if (onProgress != null)
                progress.ProgressChanged += onProgress;

            await Helpers.HttpUploadClient!.UploadFileAsync(uri, localPath, headers, progress);

            return GetFinalPath(supabasePath);
        }

        private async Task<string> UploadOrUpdate(byte[] data, string supabasePath, FileOptions options,
            EventHandler<float>? onProgress = null)
        {
            Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

            var headers = new Dictionary<string, string>(Headers)
            {
                { "cache-control", $"max-age={options.CacheControl}" },
                { "content-type", options.ContentType }
            };

            if (options.Upsert)
                headers.Add("x-upsert", options.Upsert.ToString().ToLower());

            var progress = new Progress<float>();

            if (onProgress != null)
                progress.ProgressChanged += onProgress;

            await Helpers.HttpUploadClient!.UploadBytesAsync(uri, data, headers, progress);

            return GetFinalPath(supabasePath);
        }

        private async Task<string> DownloadFile(string url, string localPath, TransformOptions? transformOptions = null,
            EventHandler<float>? onProgress = null)
        {
            var builder = new UriBuilder(url);
            var progress = new Progress<float>();

            if (transformOptions != null)
                builder.Query = transformOptions.ToQueryCollection().ToString();

            if (onProgress != null)
                progress.ProgressChanged += onProgress;

            var stream = await Helpers.HttpDownloadClient!.DownloadDataAsync(builder.Uri, Headers, progress);

            using var fileStream = new FileStream(localPath, FileMode.OpenOrCreate, FileAccess.Write);

            stream.WriteTo(fileStream);

            return localPath;
        }

        private async Task<byte[]> DownloadBytes(string url, TransformOptions? transformOptions = null,
            EventHandler<float>? onProgress = null)
        {
            var builder = new UriBuilder(url);
            var progress = new Progress<float>();

            if (transformOptions != null)
                builder.Query = transformOptions.ToQueryCollection().ToString();

            if (onProgress != null)
                progress.ProgressChanged += onProgress;

            var stream = await Helpers.HttpDownloadClient!.DownloadDataAsync(builder.Uri, Headers, progress);

            return stream.ToArray();
        }

        private string GetFinalPath(string path) => $"{BucketId}/{path}";
    }
}