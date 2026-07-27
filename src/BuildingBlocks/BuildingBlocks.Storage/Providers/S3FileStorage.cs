using Amazon.S3;
using Amazon.S3.Model;
using MicroServiceSystem.BuildingBlocks.Storage.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Storage.Providers;

/// <summary>
/// Works against any S3 compatible endpoint, which covers both Amazon S3 and MinIO. Only the client
/// configuration differs between the two.
/// </summary>
public sealed class S3FileStorage(IAmazonS3 client, string providerName) : IFileStorage
{
    public string ProviderName => providerName;

    public async Task<StoredFile> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Overwrite && await ExistsAsync(request.Container, request.Path, cancellationToken))
        {
            throw new IOException($"Object '{request.Path}' already exists in bucket '{request.Container}'.");
        }

        var putRequest = new PutObjectRequest
        {
            BucketName = request.Container,
            Key = request.Path,
            InputStream = request.Content,
            ContentType = request.ContentType,
            AutoCloseStream = false
        };

        foreach ((string key, string value) in request.Metadata)
        {
            putRequest.Metadata.Add(key, value);
        }

        PutObjectResponse response = await client.PutObjectAsync(putRequest, cancellationToken);

        return new StoredFile(
            request.Container,
            request.Path,
            request.Content.CanSeek ? request.Content.Length : 0,
            request.ContentType,
            response.ETag);
    }

    public async Task<FileDownload?> DownloadAsync(
        string container,
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            GetObjectResponse response = await client.GetObjectAsync(container, path, cancellationToken);

            return new FileDownload(
                response.ResponseStream,
                response.Headers.ContentType ?? ContentTypes.Resolve(path),
                response.ContentLength);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string container, string path, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetObjectMetadataAsync(container, path, cancellationToken);

            return true;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task DeleteAsync(string container, string path, CancellationToken cancellationToken = default) =>
        client.DeleteObjectAsync(container, path, cancellationToken);

    public Task<Uri> CreateSignedUrlAsync(
        string container,
        string path,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        string url = client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = container,
            Key = path,
            Expires = DateTime.UtcNow.Add(lifetime),
            Verb = HttpVerb.GET
        });

        return Task.FromResult(new Uri(url));
    }
}
