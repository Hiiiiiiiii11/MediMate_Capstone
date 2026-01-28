using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        // Constructor tiện ích
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

        public static ApiResponse<T> Fail(string message, int code = 400)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Code = code,
                Message = message,
                Data = default
            };
        }
    }
}
