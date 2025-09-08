using System.Collections;
using System.Collections.Immutable;
using Microsoft.Azure.Functions.Worker;

namespace SkChat.Tests;

public class FakeTraceContext : TraceContext
{
    public override string TraceParent => string.Empty;
    public override string TraceState => string.Empty;
}

public class FakeBindingContext : BindingContext
{
    public override IReadOnlyDictionary<string, object?> BindingData => new Dictionary<string, object?>();
}

public class FakeRetryContext : RetryContext
{
    public override int RetryCount => 0;
    public override int MaxRetryCount => 0;
}

public class FakeFunctionDefinition : FunctionDefinition
{
    public override string PathToAssembly => string.Empty;
    public override string EntryPoint => string.Empty;
    public override string Id => "FakeFunction";
    public override IImmutableDictionary<string, BindingMetadata> InputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    public override IImmutableDictionary<string, BindingMetadata> OutputBindings => ImmutableDictionary<string, BindingMetadata>.Empty;
    public override string Name => "FakeFunction";
    public override ImmutableArray<FunctionParameter> Parameters => ImmutableArray<FunctionParameter>.Empty;
}

public class FakeInvocationFeatures : IInvocationFeatures, IEnumerable<KeyValuePair<Type, object>>
{
    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => Enumerable.Empty<KeyValuePair<Type, object>>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    T IInvocationFeatures.Get<T>() => default!;
    void IInvocationFeatures.Set<T>(T instance) { }
}

public class MinimalServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
