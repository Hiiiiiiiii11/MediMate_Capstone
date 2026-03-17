namespace Share.Common
{
    public class ApiError
    {
        public string Code { get; set; } = string.Empty;
        public string? Field { get; set; }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public ApiError? Error { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Code = 200,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(
            string message,
            int httpStatus = 400,
            string? businessCode = null,
            string? field = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Code = httpStatus,
                Error = businessCode == null
                    ? null
                    : new ApiError { Code = businessCode, Field = field },
                Message = message,
                Data = default
            };
        }

        public static ApiResponse<T> ServerError(string message = "Internal Server Error")
        {
            return new ApiResponse<T>
            {
                Success = false,
                Code = 500,
                Message = message,
                Data = default
            };
        }
    }
}
