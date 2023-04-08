using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public StorageFileApi(string url, string bucketId, ClientOptions options, Dictionary<string, string>? headers = null) : this(url, headers, bucketId)
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
        /// <returns></returns>
        public string GetPublicUrl(string path, TransformOptions? transformOptions)
        {
            if (transformOptions == null)
                return $"{Url}/object/public/{GetFinalPath(path)}";

            var builder = new UriBuilder($"{Url}/render/image/public/{GetFinalPath(path)}");
            builder.Query = transformOptions.ToQueryCollection().ToString();

            return builder.ToString();
        }

        /// <summary>
        /// Create signed url to download file without requiring permissions. This URL can be valid for a set number of seconds.
        /// </summary>
        /// <param name="path">The file path to be downloaded, including the current file name. For example `folder/image.png`.</param>
        /// <param name="expiresIn">The number of seconds until the signed URL expires. For example, `60` for a URL which is valid for one minute.</param>
        /// <returns></returns>
        public async Task<string> CreateSignedUrl(string path, int expiresIn, TransformOptions? transformOptions = null)
        {
            var body = new Dictionary<string, object?> { { "expiresIn", expiresIn } };
            var url = $"{Url}/object/sign/{GetFinalPath(path)}";

            if (transformOptions != null)
                body.Add("transform", transformOptions);

            var response = await Helpers.MakeRequest<CreateSignedUrlResponse>(HttpMethod.Post, url, body, Headers, Options.HttpRequestTimeout);

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
            var response = await Helpers.MakeRequest<List<CreateSignedUrlsResponse>>(HttpMethod.Post, $"{Url}/object/sign/{BucketId}", body, Headers, Options.HttpRequestTimeout);

            if (response != null)
            {
                foreach (var item in response)
                {
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

            var response = await Helpers.MakeRequest<List<FileObject>>(HttpMethod.Post, $"{Url}/object/list/{BucketId}", body, Headers, Options.HttpRequestTimeout);

            return response;
        }

        /// <summary>
        /// Uploads a file to an existing bucket.
        /// </summary>
        /// <param name="localFilePath">File Source Path</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<string> Upload(string localFilePath, string supabasePath, FileOptions? options = null, EventHandler<float>? onProgress = null, bool inferContentType = true)
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
        /// <param name="localFilePath">File Source Path</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<string> Upload(byte[] data, string supabasePath, FileOptions? options = null, EventHandler<float>? onProgress = null, bool inferContentType = true)
        {
            options ??= new FileOptions();

            if (inferContentType)
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(supabasePath);

            var result = await UploadOrUpdate(data, supabasePath, options, onProgress);
            return result;
        }

        /// <summary>
        /// Uploads a file to using a pregenerated Signed Upload Url
        /// </summary>
        /// <param name="localFilePath">File Source Path</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<string> UploadToSignedUrl(string localFilePath, UploadSignedUrl signedUrl, FileOptions? options = null, EventHandler<float>? onProgress = null, bool inferContentType = true)
        {
            options ??= new FileOptions();

            if (inferContentType)
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(localFilePath);

            using (var client = new HttpClient { Timeout = Options.HttpUploadTimeout })
            {
                var headers = new Dictionary<string, string>(Headers);

                headers["Authorization"] = $"Bearer {signedUrl.Token}";
                headers.Add("cache-control", $"max-age={options.CacheControl}");
                headers.Add("content-type", options.ContentType);

                if (options.Upsert)
                    headers.Add("x-upsert", options.Upsert.ToString().ToLower());

                var progress = new Progress<float>();

                if (onProgress != null)
                    progress.ProgressChanged += onProgress;

                var response = await client.UploadFileAsync(signedUrl.SignedUrl, localFilePath, headers, progress);

                if (response.IsSuccessStatusCode)
                    return GetFinalPath(signedUrl.Key);
                else
                    throw new BadRequestException(response, (await response.Content.ReadAsStringAsync()));
            }
        }

        /// <summary>
        /// Uploads a byte array using a pregenerated Signed Upload Url
        /// </summary>
        /// <param name="localFilePath">File Source Path</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<string> UploadToSignedUrl(byte[] data, UploadSignedUrl signedUrl, FileOptions? options = null, EventHandler<float>? onProgress = null, bool inferContentType = true)
        {
            options ??= new FileOptions();

            if (inferContentType)
                options.ContentType = MimeMapping.MimeUtility.GetMimeMapping(signedUrl.Key);

            using (var client = new HttpClient { Timeout = Options.HttpUploadTimeout })
            {
                var headers = new Dictionary<string, string>(Headers);

                headers["Authorization"] = $"Bearer {signedUrl.Token}";
                headers.Add("cache-control", $"max-age={options.CacheControl}");
                headers.Add("content-type", options.ContentType);

                if (options.Upsert)
                    headers.Add("x-upsert", options.Upsert.ToString().ToLower());

                var progress = new Progress<float>();

                if (onProgress != null)
                    progress.ProgressChanged += onProgress;

                var response = await client.UploadBytesAsync(signedUrl.SignedUrl, data, headers, progress);

                if (response.IsSuccessStatusCode)
                    return GetFinalPath(signedUrl.Key);
                else
                    throw new BadRequestException(response, (await response.Content.ReadAsStringAsync()));
            }
        }


        /// <summary>
        /// Replaces an existing file at the specified path with a new one.
        /// </summary>
        /// <param name="localFilePath">File source path.</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options">HTTP headers.</param>
        /// <returns></returns>
        public Task<string> Update(string localFilePath, string supabasePath, FileOptions? options = null, EventHandler<float>? onProgress = null)
        {
            options ??= new FileOptions();
            return UploadOrUpdate(localFilePath, supabasePath, options, onProgress);
        }

        /// <summary>
        /// Replaces an existing file at the specified path with a new one.
        /// </summary>
        /// <param name="localFilePath">File source path.</param>
        /// <param name="supabasePath">The relative file path. Should be of the format `folder/subfolder/filename.png`. The bucket must already exist before attempting to upload.</param>
        /// <param name="options">HTTP headers.</param>
        /// <returns></returns>
        public Task<string> Update(byte[] data, string supabasePath, FileOptions? options = null, EventHandler<float>? onProgress = null)
        {
            options ??= new FileOptions();
            return UploadOrUpdate(data, supabasePath, options, onProgress);
        }

        /// <summary>
        /// Moves an existing file, optionally renaming it at the same time.
        /// </summary>
        /// <param name="fromPath">The original file path, including the current file name. For example `folder/image.png`.</param>
        /// <param name="toPath">The new file path, including the new file name. For example `folder/image-copy.png`.</param>
        /// <returns></returns>
        public async Task<bool> Move(string fromPath, string toPath)
        {
            try
            {
                var body = new Dictionary<string, string?> { { "bucketId", BucketId }, { "sourceKey", fromPath }, { "destinationKey", toPath } };
                await Helpers.MakeRequest<GenericResponse>(HttpMethod.Post, $"{Url}/object/move", body, Headers, Options.HttpRequestTimeout);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Downloads a file and saves it to a local path.
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="localPath"></param>
        /// <param name="transformOptions"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public async Task<string> Download(string supabasePath, string localPath, TransformOptions? transformOptions = null, EventHandler<float>? onProgress = null)
        {
            using (HttpClient client = new HttpClient { Timeout = Options.HttpDownloadTimeout })
            {
                var url = transformOptions != null ? $"{Url}/render/image/authenticated/{GetFinalPath(supabasePath)}" : $"{Url}/object/{GetFinalPath(supabasePath)}";
                var builder = new UriBuilder(url);
                var progress = new Progress<float>();

                if (transformOptions != null)
                    builder.Query = transformOptions.ToQueryCollection().ToString();

                if (onProgress != null)
                    progress.ProgressChanged += onProgress;

                var stream = await client.DownloadDataAsync(builder.Uri, Headers, progress);

                using (var outstream = new FileStream(localPath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    stream.WriteTo(outstream);
                }

                return localPath;
            }
        }

        /// <summary>
        /// Downloads a file and saves it to a local path.
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="localPath"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public Task<string> Download(string supabasePath, string localPath, EventHandler<float>? onProgress = null) =>
            Download(supabasePath, localPath, null, onProgress: onProgress);

        /// <summary>
        /// Downloads a byte array to be used programmatically.
        /// </summary>
        /// <param name="supabasePath"></param>
        /// <param name="onProgress"></param>
        /// <returns></returns>
        public async Task<byte[]> Download(string supabasePath, EventHandler<float>? onProgress = null)
        {
            using (HttpClient client = new HttpClient { Timeout = Options.HttpDownloadTimeout })
            {
                Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

                var progress = new Progress<float>();

                if (onProgress != null)
                    progress.ProgressChanged += onProgress;

                var stream = await client.DownloadDataAsync(uri, Headers, progress);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deletes file within the same bucket
        /// </summary>
        /// <param name="path">An path to delet, for example `folder/image.png`.</param>
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
            var response = await Helpers.MakeRequest<List<FileObject>>(HttpMethod.Delete, $"{Url}/object/{BucketId}", data, Headers, Options.HttpRequestTimeout);

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
            var response = await Helpers.MakeRequest<CreatedUploadSignedUrlResponse>(HttpMethod.Post, url, null, Headers, Options.HttpRequestTimeout);

            if (response == null || string.IsNullOrEmpty(response.Url) || !response.Url!.Contains("token"))
                throw new Exception("Response did not return with expected data. Does this token have proper permission to generate a url?");

            var generatedUri = new Uri($"{Url}{response.Url}");
            var query = HttpUtility.ParseQueryString(generatedUri.Query);
            var token = query["token"];

            return new UploadSignedUrl(generatedUri, token, supabasePath);
        }

        private async Task<string> UploadOrUpdate(string localPath, string supabasePath, FileOptions options, EventHandler<float>? onProgress = null)
        {
            using (var client = new HttpClient { Timeout = Options.HttpUploadTimeout })
            {
                Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

                var headers = new Dictionary<string, string>(Headers);

                headers.Add("cache-control", $"max-age={options.CacheControl}");
                headers.Add("content-type", options.ContentType);

                if (options.Upsert)
                    headers.Add("x-upsert", options.Upsert.ToString().ToLower());

                var progress = new Progress<float>();

                if (onProgress != null)
                    progress.ProgressChanged += onProgress;

                var response = await client.UploadFileAsync(uri, localPath, headers, progress);

                if (response.IsSuccessStatusCode)
                    return GetFinalPath(supabasePath);
                else
                    throw new BadRequestException(response, (await response.Content.ReadAsStringAsync()));
            }
        }

        private async Task<string> UploadOrUpdate(byte[] data, string supabasePath, FileOptions options, EventHandler<float>? onProgress = null)
        {
            using (var client = new HttpClient { Timeout = Options.HttpUploadTimeout })
            {
                Uri uri = new Uri($"{Url}/object/{GetFinalPath(supabasePath)}");

                var headers = new Dictionary<string, string>(Headers);

                headers.Add("cache-control", $"max-age={options.CacheControl}");
                headers.Add("content-type", options.ContentType);

                if (options.Upsert)
                    headers.Add("x-upsert", options.Upsert.ToString().ToLower());

                var progress = new Progress<float>();

                if (onProgress != null)
                    progress.ProgressChanged += onProgress;

                var response = await client.UploadBytesAsync(uri, data, headers, progress);

                if (response.IsSuccessStatusCode)
                    return GetFinalPath(supabasePath);
                else
                    throw new BadRequestException(response, (await response.Content.ReadAsStringAsync()));
            }
        }

        private string GetFinalPath(string path) => $"{BucketId}/{path}";
    }
}