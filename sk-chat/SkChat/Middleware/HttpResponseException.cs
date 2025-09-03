using System.Net;

namespace SkChat.Middleware;

public class HttpResponseException : Exception
{
    public HttpStatusCode Status { get; }

    public HttpResponseException(string message, HttpStatusCode status = HttpStatusCode.InternalServerError, Exception? innerException = null)
     : base(message, innerException)
    {
        Status = status;
    }
}
