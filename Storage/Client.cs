﻿using Storage.Interfaces;
using System;
using System.Collections.Generic;

namespace Supabase.Storage
{
    public class Client : StorageBucketApi, IStorageClient<Bucket, FileObject>
    {
        public Client(string url, Dictionary<string, string> headers) : base(url, headers)
        { }

        /// <summary>
        /// Perform a file operation in a bucket
        /// </summary>
        /// <param name="id">Bucket Id</param>
        /// <returns></returns>
        public IStorageFileApi<FileObject> From(string id) => new StorageFileApi(Url, Headers, id);
    }
}