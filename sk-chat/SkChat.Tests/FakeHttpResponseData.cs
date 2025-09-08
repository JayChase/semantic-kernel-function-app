using System.Net;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SkChat.Tests;

public class FakeHttpResponseData : HttpResponseData
{
    private MemoryStream _bodyStream = new();
    public FakeHttpResponseData(FunctionContext functionContext, HttpStatusCode statusCode = HttpStatusCode.OK) : base(functionContext)
    {
        StatusCode = statusCode;
    }
    public override HttpStatusCode StatusCode { get; set; }
    private HttpHeadersCollection _headers = new HttpHeadersCollection();
    public override HttpHeadersCollection Headers { get => _headers; set => _headers = value; }
    public override Stream Body { get => _bodyStream; set => _bodyStream = (MemoryStream)value; }
    public override HttpCookies Cookies { get; } = new TestHttpCookies();
    public static async Task<string> ReadBodyAsStringAsync(HttpResponseData response)
    {
        if (response is FakeHttpResponseData fake)
        {
            fake._bodyStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(fake._bodyStream, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }
        response.Body.Seek(0, SeekOrigin.Begin);
        using var fallbackReader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        return await fallbackReader.ReadToEndAsync();
    }
}
