namespace VillageShop.Common.Models;

public class PostResponse
{
    public string ViewAsString { get; set; } = string.Empty;

    public bool Status { get; set; }

    public int StatusCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public string RedirectURL { get; set; } = string.Empty;

    public long ID { get; set; }

    public string AdditionalMessage { get; set; } = string.Empty;

    public static PostResponse Success(string message = "Operation completed successfully.", long id = 0, string additionalMessage = "")
    {
        return new PostResponse
        {
            Status = true,
            StatusCode = 200,
            Message = message,
            ID = id,
            AdditionalMessage = additionalMessage
        };
    }

    public static PostResponse Error(string message, int statusCode = 400, long id = 0, string additionalMessage = "")
    {
        return new PostResponse
        {
            Status = false,
            StatusCode = statusCode,
            Message = message,
            ID = id,
            AdditionalMessage = additionalMessage
        };
    }
}
