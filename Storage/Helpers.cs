using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.IO;
using Supabase.Storage.Exceptions;

[assembly: InternalsVisibleTo("SupabaseTests")]
namespace Supabase.Storage
{
    internal static class Helpers
    {
        internal static HttpClient HttpRequestClient = new HttpClient();
        internal static HttpClient HttpUploadClient = new HttpClient();
        internal static HttpClient HttpDownloadClient = new HttpClient();

        /// <summary>
        /// Helper to make a request using the defined parameters to an API Endpoint and coerce into a model. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="reqParams"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<T?> MakeRequest<T>(HttpMethod method, string url, object? data = null, Dictionary<string, string>? headers = null) where T : class
        {
            var response = await MakeRequest(method, url, data, headers);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            else
            {
                throw new BadRequestException(response, content);
            }
        }

        /// <summary>
        /// Helper to make a request using the defined parameters to an API Endpoint.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="reqParams"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> MakeRequest(HttpMethod method, string url, object? data = null, Dictionary<string, string>? headers = null)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            if (data != null && method != HttpMethod.Get)
            {
                // Case if it's a Get request the data object is a dictionary<string,string>
                if (data is Dictionary<string, string> reqParams)
                {
                    foreach (var param in reqParams)
                        query[param.Key] = param.Value;
                }
            }

            builder.Query = query.ToString();

            using (var requestMessage = new HttpRequestMessage(method, builder.Uri))
            {

                if (data != null && method != HttpMethod.Get)
                {
                    requestMessage.Content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                }

                if (headers != null)
                {
                    foreach (var kvp in headers)
                    {
                        requestMessage.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                    }
                }

                var response = await HttpRequestClient.SendAsync(requestMessage);

                return response;
            }
        }
    }

    public class GenericResponse
    {
        [JsonProperty("message")]
        public string? Message { get; set; }
    }

    public class ErrorResponse
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("error")]
        public string? Error { get; set; }
    }
}