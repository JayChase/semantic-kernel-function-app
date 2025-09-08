using System.Collections;
using Microsoft.Azure.Functions.Worker.Http;

namespace SkChat.Tests;

public class TestHttpCookies : HttpCookies, IEnumerable<IHttpCookie>
{
    public override void Append(string name, string value) { }
    public override void Append(IHttpCookie cookie) { }
    public override IHttpCookie CreateNew() => new TestHttpCookie();
    public IEnumerator<IHttpCookie> GetEnumerator() => Enumerable.Empty<IHttpCookie>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TestHttpCookie : IHttpCookie
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
