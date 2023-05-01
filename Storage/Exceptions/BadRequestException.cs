using System.Net.Http;
using Newtonsoft.Json;

namespace Supabase.Storage.Exceptions
{
    /// <summary>
    /// If you see this error, it is likely due to permissions. Verify the supplied token has access to the requested resource.
    /// </summary>
    public class BadRequestException : SupabaseStorageException
    {
        public ErrorResponse? ErrorResponse { get; private set; }

        public HttpResponseMessage HttpResponse { get; private set; }

        public BadRequestException(HttpResponseMessage httpResponse, string content) : base(content)
        {
            HttpResponse = httpResponse;
            ErrorResponse = JsonConvert.DeserializeObject<ErrorResponse>(content);
        }
    }
}