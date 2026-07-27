using Google;
using Google.Cloud.Storage.V1;
using MicroServiceSystem.BuildingBlocks.Storage.Abstractions;
using MicroServiceSystem.BuildingBlocks.Storage.Configuration;

namespace MicroServiceSystem.BuildingBlocks.Storage.Providers;

public sealed class GoogleCloudFileStorage(StorageClient client, UrlSigner? urlSigner) : IFileStorage
{
    public string ProviderName => nameof(FileStorageProvider.GoogleCloudStorage);

    public async Task<StoredFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uploadOptions = new UploadObjectOptions();

        if (!request.Overwrite)
        {
            uploadOptions.IfGenerationMatch = 0;
        }

        Google.Apis.Storage.v1.Data.Object stored = await client.UploadObjectAsync(
            request.Container,
            request.Path,
            request.ContentType,
            request.Content,
            uploadOptions,
            cancellationToken);

        return new StoredFile(
            request.Container,
            request.Path,
            (long)(stored.Size ?? 0),
            request.ContentType,
            stored.ETag);
    }

    public async Task<FileDownload?> DownloadAsync(
        string container,
        string path,
        CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();

        try
        {
            Google.Apis.Storage.v1.Data.Object stored = await client.DownloadObjectAsync(
                container,
                path,
                buffer,
                cancellationToken: cancellationToken);

            buffer.Position = 0;

            return new FileDownload(buffer, stored.ContentType ?? ContentTypes.Resolve(path), buffer.Length);
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await buffer.DisposeAsync();

            return null;
        }
    }

    public async Task<bool> ExistsAsync(string container, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetObjectAsync(container, path, cancellationToken: cancellationToken);

            return true;
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteAsync(string container, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.DeleteObjectAsync(container, path, cancellationToken: cancellationToken);
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Deleting an absent object is the desired end state.
        }
    }

    public async Task<Uri> CreateSignedUrlAsync(
        string container,
        string path,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        if (urlSigner is null)
        {
            throw new InvalidOperationException(
                "Storage:GoogleCloud:CredentialsJsonPath must point to a service account key to sign URLs.");
        }

        string url = await urlSigner.SignAsync(container, path, lifetime, cancellationToken: cancellationToken);

        return new Uri(url);
    }
}
