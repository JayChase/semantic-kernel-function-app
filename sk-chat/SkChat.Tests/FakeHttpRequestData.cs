
using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SkChat.Tests;

public class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext context) : base(context) { }
    public override Stream Body { get; } = new MemoryStream();
    public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = Array.Empty<IHttpCookie>();
    public override Uri Url { get; } = new Uri("http://localhost");
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method { get; } = "GET";
    public override HttpResponseData CreateResponse() => new FakeHttpResponseData(FunctionContext);
}
