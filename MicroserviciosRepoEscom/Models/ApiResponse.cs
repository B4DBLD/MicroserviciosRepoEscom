namespace MicroserviciosRepoEscom.Models
{
    public class ApiResponse<T>
    {
        public bool Ok { get; set; }
        public T? Data { get; set; } // Nullable if T is a reference type or Nullable<value type>
        public string? Message { get; set; } // General message, can be used for errors or success messages
        public List<string>? Errors { get; set; } // For detailed error messages or validation errors

        // Static factory methods for convenience

        public static ApiResponse<T> Success(T data, string? message = null)
        {
            return new ApiResponse<T> { Ok = true, Data = data, Message = message };
        }

        public static ApiResponse<object> Failure(string message, List<string>? errors = null) // Non-generic for failures where T might not be relevant
        {
            return new ApiResponse<object> { Ok = false, Message = message, Errors = errors, Data = null };
        }

        // If you want a failure method that still conforms to ApiResponse<T>
        public static ApiResponse<T> Failure(string message, List<string>? errors = null, T? data = default)
        {
            return new ApiResponse<T> { Ok = false, Message = message, Errors = errors, Data = data };
        }
    }

    public class ApiResponse
    {
        public bool Ok { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse Success(string? message = null)
        {
            return new ApiResponse { Ok = true, Message = message };
        }

        public static ApiResponse Failure(string message, List<string>? errors = null)
        {
            return new ApiResponse { Ok = false, Message = message, Errors = errors };
        }
    }
}
