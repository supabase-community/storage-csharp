using Storage.Interfaces;
using Supabase.Core;
using Supabase.Core.Extensions;
using Supabase.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace Supabase.Storage
{
    public class Client : StorageBucketApi, IStorageClient<Bucket, FileObject>
    {
        public new Dictionary<string, string> Headers
        {
            get => GetHeaders != null ? GetHeaders().MergeLeft(_headers) : _headers;
            set
            {
                _headers = value;

                if (!_headers.ContainsKey("X-Client-Info"))
                    _headers.Add("X-Client-Info", Util.GetAssemblyVersion(typeof(Client)));
            }
        }

        /// <summary>
        /// Function that can be set to return dynamic headers.
        /// 
        /// Headers specified in the constructor will ALWAYS take precendece over headers returned by this function.
        /// </summary>
        public Func<Dictionary<string, string>>? GetHeaders { get; set; }

        public Client(string url, Dictionary<string, string>? headers = null) : base(url, headers)
        { }

        /// <summary>
        /// Perform a file operation in a bucket
        /// </summary>
        /// <param name="id">Bucket Id</param>
        /// <returns></returns>
        public IStorageFileApi<FileObject> From(string id) => new StorageFileApi(Url, Headers, id);
    }
}
