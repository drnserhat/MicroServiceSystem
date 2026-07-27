namespace MicroServiceSystem.BuildingBlocks.Storage.Abstractions;

public interface IFileStorage
{
    string ProviderName { get; }

    Task<StoredFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default);

    Task<FileDownload?> DownloadAsync(string container, string path, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string container, string path, CancellationToken cancellationToken = default);

    Task DeleteAsync(string container, string path, CancellationToken cancellationToken = default);

    Task<Uri> CreateSignedUrlAsync(
        string container,
        string path,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);
}

public sealed record FileUploadRequest
{
    public required string Container { get; init; }

    public required string Path { get; init; }

    public required Stream Content { get; init; }

    public required string ContentType { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public bool Overwrite { get; init; } = true;
}

public sealed record StoredFile(string Container, string Path, long SizeInBytes, string ContentType, string? ETag);

public sealed record FileDownload(Stream Content, string ContentType, long SizeInBytes);
