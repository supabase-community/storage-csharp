using System;
using System.Collections.Specialized;
using System.Web;

namespace Supabase.Storage.Extensions;

public static class PurgeCacheOptionsExtension
{
    /// <summary>
        /// Transforms options into a NameValueCollection to be used with a <see cref="UriBuilder"/>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static NameValueCollection ToQueryCollection(this PurgeCacheOptions options)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (options.Transformations == null)
            {
                return query;
            }
            
            query.Add("transformations", options.Transformations.ToString().ToLower());

            return query;
        }
}