using System;
using System.Collections.Specialized;

namespace Storage.Extensions
{
	public static class TransformOptionsExtension
	{
        /// <summary>
        /// Transforms options into a NameValueCollecto to be used with a <see cref="UriBuilder"/>
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static NameValueCollection ToQueryCollection(this TransformOptions transform)
        {
            var query = new NameValueCollection();

            if (transform.Width != null)
                query.Add("width", transform.Width.ToString());

            if (transform.Height != null)
                query.Add("height", transform.Height.ToString());

            if (transform.Format != null)
                query.Add("format", transform.Format);

            var mapResizeTo = Supabase.Core.Helpers.GetMappedToAttr(transform.Resize);
            query.Add("resize", mapResizeTo.Mapping);

            query.Add("quality", transform.Quality.ToString());

            return query;
        }
    }
}

