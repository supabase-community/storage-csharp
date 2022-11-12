﻿using Supabase.Core.Interfaces;
using Supabase.Storage;

namespace Storage.Interfaces
{
    public interface IStorageClient<TBucket, TFileObject> : IStorageBucketApi<TBucket>, IGettableHeaders
        where TBucket : Bucket
        where TFileObject : FileObject
    {
        IStorageFileApi<TFileObject> From(string id);
    }
}