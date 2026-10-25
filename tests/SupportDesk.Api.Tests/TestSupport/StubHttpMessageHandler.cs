using System.Net;
using System.Text;

namespace SupportDesk.Api.Tests.TestSupport;

/// <summary>
/// Stands in for the network under an <see cref="HttpClient"/>: it answers with whatever the test supplies and
/// records each request (URL, headers and body), so the Groq adapter can be tested without calling Groq.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        _respond = respond;
    }

    public int Calls { get; private set; }

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastBody { get; private set; }

    /// <summary>A handler that always answers with <paramref name="status"/> and a JSON <paramref name="body"/>.</summary>
    public static StubHttpMessageHandler Json(HttpStatusCode status, string body) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        }));

    /// <summary>A Groq-style success envelope whose first choice carries <paramref name="content"/>.</summary>
    public static StubHttpMessageHandler GroqReply(string content, string finishReason = "stop")
    {
        var envelope = System.Text.Json.JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { role = "assistant", content }, finish_reason = finishReason },
            },
        });
        return Json(HttpStatusCode.OK, envelope);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Calls++;
        LastRequest = request;
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return await _respond(request, cancellationToken);
    }
}
