using Microsoft.Azure.Functions.Worker;

namespace SkChat.Tests;

public class FakeFunctionContext : FunctionContext
{
    private IDictionary<object, object> _items = new Dictionary<object, object>();
    private IServiceProvider _instanceServices;
    private Microsoft.AspNetCore.Http.HttpContext? _httpContext;

    public FakeFunctionContext(IServiceProvider? instanceServices = null)
    {
        _instanceServices = instanceServices ?? new MinimalServiceProvider();
    }

    public void SetHttpContext(Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        _httpContext = httpContext;
        // Store in Items with the key that GetHttpContext extension method expects
        Items["HttpRequestContext"] = httpContext;
    }

    public Microsoft.AspNetCore.Http.HttpContext? GetHttpContext()
    {
        return _httpContext;
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
