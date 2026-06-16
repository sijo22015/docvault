using DocVault.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DocVault.Infrastructure.Services;

public class LocalDiskFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalDiskFileStorage(IConfiguration config)
    {
        _rootPath = config["Storage:RootPath"] ?? Path.Combine(Path.GetTempPath(), "docvault", "storage");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string subPath, CancellationToken ct = default)
    {
        var dir = Path.Combine(_rootPath, subPath);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, fileName);
        using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);
        return fullPath;
    }

    public Task<Stream> ReadAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Document file not found.", filePath);
        Stream stream = File.OpenRead(filePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string filePath, CancellationToken ct = default)
    {
        if (File.Exists(filePath))
            File.Move(filePath, filePath + ".deleted", overwrite: true);
        return Task.CompletedTask;
    }

    public Task RestoreAsync(string filePath, CancellationToken ct = default)
    {
        var deletedPath = filePath + ".deleted";
        if (File.Exists(deletedPath))
            File.Move(deletedPath, filePath, overwrite: true);
        return Task.CompletedTask;
    }

    public Task PurgeAsync(string filePath, CancellationToken ct = default)
    {
        var deletedPath = filePath + ".deleted";
        if (File.Exists(deletedPath)) File.Delete(deletedPath);
        if (File.Exists(filePath))    File.Delete(filePath);
        return Task.CompletedTask;
    }

    public bool Exists(string filePath) => File.Exists(filePath);
}
