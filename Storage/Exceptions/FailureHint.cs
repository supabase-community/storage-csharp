using System.Linq;
using static Supabase.Storage.Exceptions.FailureHint.Reason;

namespace Supabase.Storage.Exceptions
{
    public static class FailureHint
    	{
    		public enum Reason
    		{
    			Unknown,
    			NotAuthorized,
    			Internal,
                NotFound,
                AlreadyExists,
                InvalidInput
    		}
    
    		public static Reason DetectReason(SupabaseStorageException storageException)
    		{
    			if (storageException.Content == null)
    				return Unknown;
    
    			return storageException.StatusCode switch
    			{
	                400 when storageException.Content.Contains("Invalid") => InvalidInput,
    				400 when storageException.Content.Contains("authorization") => NotAuthorized,
                    400 when storageException.Content.Contains("malformed") => NotAuthorized,
                    400 when storageException.Content.Contains("invalid signature") => NotAuthorized,
    				401 => NotAuthorized,
                    404 when storageException.Content.Contains("Not found") => NotFound,
	                409 when storageException.Content.Contains("exists") => AlreadyExists,
    				500 => Internal,
    				_ => Unknown
    			};
    		}
    	}
   
}