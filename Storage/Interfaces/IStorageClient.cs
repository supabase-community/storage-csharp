using Supabase.Storage;

namespace Storage.Interfaces
{
    public interface IStorageClient<TBucket, TFileObject>
        where TBucket : Bucket
        where TFileObject : FileObject
    {
        IStorageFileApi<TFileObject> From(string id);
    }
}