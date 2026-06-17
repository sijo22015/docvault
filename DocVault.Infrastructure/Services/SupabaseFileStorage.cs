using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocVault.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocVault.Infrastructure.Services;

public class SupabaseFileStorage : IFileStorage
{
    private readonly HttpClient _http;
    private readonly string _storageUrl;
    private readonly string _serviceKey;
    private readonly string _bucket;
    private readonly ILogger<SupabaseFileStorage> _logger;

    public SupabaseFileStorage(HttpClient http, IConfiguration config, ILogger<SupabaseFileStorage> logger)
    {
        _http = http;
        _serviceKey = config["Supabase:ServiceKey"]
            ?? throw new InvalidOperationException("Supabase:ServiceKey is not configured.");
        var projectUrl = (config["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is not configured.")).TrimEnd('/');
        _storageUrl = $"{projectUrl}/storage/v1";
        _bucket = config["Supabase:StorageBucket"] ?? "documents";
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string subPath, CancellationToken ct = default)
    {
        var objectPath = (subPath.Replace('\\', '/').TrimStart('/') + "/" + fileName).TrimStart('/');

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        ms.Position = 0;

        var res = await UploadAsync(objectPath, ms, ct);

        // If bucket doesn't exist yet, create it and retry once
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            if ((int)res.StatusCode == 404 || body.Contains("Bucket") || body.Contains("bucket"))
            {
                await CreateBucketIfMissingAsync(ct);
                ms.Position = 0;
                res = await UploadAsync(objectPath, ms, ct);
            }
        }

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("Supabase upload failed {Status}: {Body}", res.StatusCode, err);
            throw new InvalidOperationException("Failed to save document. Please try again.");
        }

        _logger.LogInformation("Saved {Path} to Supabase", objectPath);
        return objectPath;
    }

    public async Task<Stream> ReadAsync(string filePath, CancellationToken ct = default)
    {
        var objectPath = filePath.Replace('\\', '/').TrimStart('/');

        var req = new HttpRequestMessage(HttpMethod.Get, $"{_storageUrl}/object/{_bucket}/{objectPath}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);

        var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("Supabase file not found {Path} ({Status})", filePath, res.StatusCode);
            throw new FileNotFoundException("Document file not found.", filePath);
        }

        // Copy to MemoryStream so the HTTP connection is freed before the stream is consumed
        var ms = new MemoryStream();
        await (await res.Content.ReadAsStreamAsync(ct)).CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    // Soft delete is tracked in the database; no file movement needed
    public Task DeleteAsync(string filePath, CancellationToken ct = default) => Task.CompletedTask;
    public Task RestoreAsync(string filePath, CancellationToken ct = default) => Task.CompletedTask;

    public async Task PurgeAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            var objectPath = filePath.Replace('\\', '/').TrimStart('/');
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_storageUrl}/object/delete/{_bucket}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);
            req.Content = new StringContent(
                JsonSerializer.Serialize(new { prefixes = new[] { objectPath } }),
                Encoding.UTF8, "application/json");
            await _http.SendAsync(req, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to purge {Path} from Supabase", filePath);
        }
    }

    public bool Exists(string filePath) => true; // Rely on ReadAsync for authoritative check

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> UploadAsync(string objectPath, Stream content, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_storageUrl}/object/{_bucket}/{objectPath}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);
        req.Headers.TryAddWithoutValidation("x-upsert", "true");
        req.Content = new StreamContent(content);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return await _http.SendAsync(req, ct);
    }

    private async Task CreateBucketIfMissingAsync(CancellationToken ct)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_storageUrl}/bucket");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceKey);
            req.Content = new StringContent(
                JsonSerializer.Serialize(new { id = _bucket, name = _bucket, @public = false }),
                Encoding.UTF8, "application/json");
            await _http.SendAsync(req, ct);
            _logger.LogInformation("Created Supabase storage bucket: {Bucket}", _bucket);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create Supabase bucket {Bucket}", _bucket);
        }
    }
}
