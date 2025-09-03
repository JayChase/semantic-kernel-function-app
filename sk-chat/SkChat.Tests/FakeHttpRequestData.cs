using System.Collections;
using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SkChat.Tests;

// Minimal fake for HttpRequestData: this could not be mocked because Moq forbids mocking WriteStringAsync
// TODO: could try to mock everything else in here. fffff

class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData() : base(new FakeFunctionContext()) { }
    public FakeHttpRequestData(FunctionContext context) : base(context) { }
    public override Stream Body { get; } = new MemoryStream();
    public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = Array.Empty<IHttpCookie>();
    public override Uri Url { get; } = new Uri("http://localhost");
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method { get; } = "GET";
    public override HttpResponseData CreateResponse() => new FakeHttpResponseData(FunctionContext);
}

class FakeHttpResponseData : HttpResponseData
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

class TestHttpCookies : HttpCookies, IEnumerable<IHttpCookie>
{
    public override void Append(string name, string value) { }
    public override void Append(IHttpCookie cookie) { }
    public override IHttpCookie CreateNew() => new TestHttpCookie();
    public IEnumerator<IHttpCookie> GetEnumerator() => Enumerable.Empty<IHttpCookie>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

class TestHttpCookie : IHttpCookie
{
    public string Name => string.Empty;
    public string Value => string.Empty;
    public DateTimeOffset? Expires => null;
    public string Path => string.Empty;
    public string Domain => string.Empty;
    public bool? Secure => false;
    public bool? HttpOnly => false;
    public double? MaxAge => null;
    public SameSite SameSite => SameSite.Lax;
}

// Minimal fake for FunctionContext
class FakeFunctionContext : FunctionContext
{
    private IDictionary<object, object> _items = new Dictionary<object, object>();
    private IServiceProvider _instanceServices;
    public FakeFunctionContext(IServiceProvider? instanceServices = null)
    {
        _instanceServices = instanceServices ?? new MinimalServiceProvider();
    }
    public override string InvocationId => Guid.NewGuid().ToString();
    public override string FunctionId => Guid.NewGuid().ToString();
    public override TraceContext TraceContext { get; } = new FakeTraceContext();
    public override BindingContext BindingContext { get; } = new FakeBindingContext();
    public override IServiceProvider InstanceServices { get => _instanceServices; set => _instanceServices = value; }
    public override FunctionDefinition FunctionDefinition => new FakeFunctionDefinition();
    public override IInvocationFeatures Features { get; } = new FakeInvocationFeatures();
    public override IDictionary<object, object> Items { get => _items; set => _items = value; }
    public override CancellationToken CancellationToken => CancellationToken.None;
    public override RetryContext RetryContext { get; } = new FakeRetryContext();
}

class FakeTraceContext : TraceContext
{
    public override string TraceParent => string.Empty;
    public override string TraceState => string.Empty;
}

class FakeBindingContext : BindingContext
{
    public override IReadOnlyDictionary<string, object?> BindingData => new Dictionary<string, object?>();
}

class FakeRetryContext : RetryContext
{
    public override int RetryCount => 0;
    public override int MaxRetryCount => 0;
}

class FakeFunctionDefinition : FunctionDefinition
{
    public override string PathToAssembly => string.Empty;
    public override string EntryPoint => string.Empty;
    public override string Id => "FakeFunction";
    public override IImmutableDictionary<string, BindingMetadata> InputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    public override string Name => "FakeFunction";
    public override ImmutableArray<FunctionParameter> Parameters => ImmutableArray<FunctionParameter>.Empty;
}

class FakeInvocationFeatures : IInvocationFeatures, IEnumerable<KeyValuePair<Type, object>>
{
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => Enumerable.Empty<KeyValuePair<Type, object>>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    T IInvocationFeatures.Get<T>() => default!;
    void IInvocationFeatures.Set<T>(T instance) { }
}

// Minimal IServiceProvider for context
class MinimalServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
