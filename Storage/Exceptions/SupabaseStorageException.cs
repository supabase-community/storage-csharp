using System;

namespace Supabase.Storage.Exceptions
{
    public class SupabaseStorageException : Exception
    {
        public SupabaseStorageException() { }
        public SupabaseStorageException(string message) : base(message) { }
        public SupabaseStorageException(string message, Exception innerException) : base(message, innerException) { }
    }
}