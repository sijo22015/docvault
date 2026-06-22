using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DocVault.Api.Controllers;

[ApiController]
[Route("api/v1/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _groqApiKey;

    public ChatController(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _groqApiKey  = config["Groq:ApiKey"] ?? "";
    }

    public record ChatMessage(string Role, string Content);
    public record ChatRequest(ChatMessage[] Messages);
    public record ChatResponse(string Content);

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(_groqApiKey))
            return StatusCode(503, new { error = "Chat service unavailable." });

        var payload = JsonSerializer.Serialize(new
        {
            model       = "llama-3.1-8b-instant",
            messages    = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            max_tokens  = 350,
            temperature = 0.4,
        });

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey);

        var http = _httpFactory.CreateClient();
        var res  = await http.SendAsync(req);

        if (!res.IsSuccessStatusCode)
            return StatusCode(502, new { error = "Upstream AI service error." });

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Sorry, I could not get a response.";

        return Ok(new ChatResponse(content));
    }
}
