using Microsoft.Extensions.Options;
using MicroServiceSystem.BuildingBlocks.Storage.Abstractions;
using MicroServiceSystem.BuildingBlocks.Storage.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Storage.Providers;

/// <summary>
/// Disk backed storage for local development and single node deployments. Paths are resolved against
/// the configured root and validated so a crafted key cannot escape it.
/// </summary>
public sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly LocalStorageOptions _localOptions = options.Value.Local;

    public string ProviderName => nameof(FileStorageProvider.Local);

    public async Task<StoredFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string fullPath = ResolvePath(request.Container, request.Path);

        if (!request.Overwrite && File.Exists(fullPath))
        {
            throw new IOException($"File '{request.Path}' already exists in container '{request.Container}'.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (FileStream target = File.Create(fullPath))
        {
            await request.Content.CopyToAsync(target, cancellationToken);
        }

        var info = new FileInfo(fullPath);

        return new StoredFile(request.Container, request.Path, info.Length, request.ContentType, null);
    }

    public Task<FileDownload?> DownloadAsync(
        string container,
        string path,
        CancellationToken cancellationToken = default)
    {
        string fullPath = ResolvePath(container, path);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<FileDownload?>(null);
        }

        var info = new FileInfo(fullPath);
        Stream content = File.OpenRead(fullPath);

        return Task.FromResult<FileDownload?>(
            new FileDownload(content, ContentTypes.Resolve(path), info.Length));
    }

    public Task<bool> ExistsAsync(string container, string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(ResolvePath(container, path)));

    public Task DeleteAsync(string container, string path, CancellationToken cancellationToken = default)
    {
        string fullPath = ResolvePath(container, path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Local storage has no signing mechanism, so the URL is a plain public link and the lifetime is
    /// ignored. Use a real provider when expiring links matter.
    /// </summary>
    public Task<Uri> CreateSignedUrlAsync(
        string container,
        string path,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_localOptions.PublicBaseUrl))
        {
            throw new InvalidOperationException(
                "Storage:Local:PublicBaseUrl must be configured before local files can be linked.");
        }

        return Task.FromResult(new Uri($"{_localOptions.PublicBaseUrl.TrimEnd('/')}/{container}/{path.TrimStart('/')}"));
    }

    private string ResolvePath(string container, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string root = Path.GetFullPath(_localOptions.RootPath);
        string candidate = Path.GetFullPath(Path.Combine(root, container, path));

        if (!candidate.StartsWith(root, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException($"Path '{path}' resolves outside of the storage root.");
        }

        return candidate;
    }
}
