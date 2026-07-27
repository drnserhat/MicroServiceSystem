using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using MicroServiceSystem.BuildingBlocks.Storage.Abstractions;
using MicroServiceSystem.BuildingBlocks.Storage.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Storage.Providers;

public sealed class AzureBlobFileStorage(BlobServiceClient client) : IFileStorage
{
    public string ProviderName => nameof(FileStorageProvider.AzureBlob);

    public async Task<StoredFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        BlobClient blob = await GetBlobAsync(request.Container, request.Path, createContainer: true, cancellationToken);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = request.ContentType },
            Metadata = request.Metadata.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal)
        };

        if (!request.Overwrite)
        {
            uploadOptions.Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All };
        }

        Response<BlobContentInfo> response = await blob.UploadAsync(request.Content, uploadOptions, cancellationToken);

        return new StoredFile(
            request.Container,
            request.Path,
            request.Content.CanSeek ? request.Content.Length : 0,
            request.ContentType,
            response.Value.ETag.ToString());
    }

    public async Task<FileDownload?> DownloadAsync(
        string container,
        string path,
        CancellationToken cancellationToken = default)
    {
        BlobClient blob = await GetBlobAsync(container, path, createContainer: false, cancellationToken);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            return null;
        }

        Response<BlobDownloadStreamingResult> response = await blob.DownloadStreamingAsync(
            cancellationToken: cancellationToken);

        return new FileDownload(
            response.Value.Content,
            response.Value.Details.ContentType ?? ContentTypes.Resolve(path),
            response.Value.Details.ContentLength);
    }

    public async Task<bool> ExistsAsync(string container, string path, CancellationToken cancellationToken = default)
    {
        BlobClient blob = await GetBlobAsync(container, path, createContainer: false, cancellationToken);

        return await blob.ExistsAsync(cancellationToken);
    }

    public async Task DeleteAsync(string container, string path, CancellationToken cancellationToken = default)
    {
        BlobClient blob = await GetBlobAsync(container, path, createContainer: false, cancellationToken);

        await blob.DeleteIfExistsAsync(
            DeleteSnapshotsOption.IncludeSnapshots,
            cancellationToken: cancellationToken);
    }

    public async Task<Uri> CreateSignedUrlAsync(
        string container,
        string path,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        BlobClient blob = await GetBlobAsync(container, path, createContainer: false, cancellationToken);

        if (!blob.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "The configured Azure Blob credential cannot generate SAS URIs; use a shared key connection string.");
        }

        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(lifetime));
    }

    private async Task<BlobClient> GetBlobAsync(
        string container,
        string path,
        bool createContainer,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(container);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        BlobContainerClient containerClient = client.GetBlobContainerClient(container);

        if (createContainer)
        {
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }

        return containerClient.GetBlobClient(path);
    }
}
