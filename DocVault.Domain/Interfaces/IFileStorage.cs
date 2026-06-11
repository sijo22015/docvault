namespace DocVault.Domain.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, string subPath, CancellationToken ct = default);
    Task<Stream> ReadAsync(string filePath, CancellationToken ct = default);
    Task DeleteAsync(string filePath, CancellationToken ct = default);
    Task RestoreAsync(string filePath, CancellationToken ct = default);
    bool Exists(string filePath);
}
