using System.Net;
using Newtonsoft.Json;

namespace VeltrixBookingApp.Application.Common.BasicResult
{
    public class BasicActionResult<T>
    {
        [JsonIgnore]
        public HttpStatusCode Status { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string? Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public PaginationInfo? PaginationData { get; set; }

        public BasicActionResult(string message, T data, PaginationInfo paginationInfo, HttpStatusCode httpStatusCode)
        {
            Status = httpStatusCode;
            Message = message;
            Data = data;
            PaginationData = paginationInfo;
        }

        public BasicActionResult(string message, T data, HttpStatusCode httpStatusCode)
        {
            Status = httpStatusCode;
            Message = message;
            Data = data;
        }

        public BasicActionResult(string message, HttpStatusCode httpStatusCode)
        {
            Status = httpStatusCode;
            Message = message;
        }

        public BasicActionResult()
        {
        }
    }

    public class PaginationInfo
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalItemCount { get; set; }
        public bool HasMoreData { get; set; }
    }

    public class BasicActionResult : BasicActionResult<string>
    {
        public static PaginationInfo CreatePaginationInfo(int pageNumber, int pageSize, int totalItem)
        {
            return new PaginationInfo { PageNumber = pageNumber, PageSize = pageSize, TotalItemCount = totalItem, HasMoreData = ((pageNumber * pageSize) < totalItem) };
        }

        public static BasicActionResult<T> SuccessResponseWithPagination<T>(string message, T data, PaginationInfo pagination, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            return new BasicActionResult<T>(message, data, pagination, httpStatusCode);
        }

        public static BasicActionResult<T> SuccessResponse<T>(string message, T data, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            return new BasicActionResult<T>(message, data, httpStatusCode);
        }

        public static BasicActionResult<T> SuccessResponse<T>(string message, HttpStatusCode httpStatusCode = HttpStatusCode.OK)
        {
            return new BasicActionResult<T>(message, httpStatusCode);
        }

        public static BasicActionResult<T> FailureResponse<T>(string errorMessage, HttpStatusCode httpStatusCode)
        {
            if (httpStatusCode == HttpStatusCode.InternalServerError)
            {
                var friendlyMessage = errorMessage;
                return new BasicActionResult<T>(friendlyMessage, default!, httpStatusCode);
            }
            return new BasicActionResult<T>(errorMessage, default!, httpStatusCode);
        }
    }
}
