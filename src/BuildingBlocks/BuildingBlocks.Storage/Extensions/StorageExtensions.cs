using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Azure.Storage.Blobs;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MicroServiceSystem.BuildingBlocks.Storage.Abstractions;
using MicroServiceSystem.BuildingBlocks.Storage.Configuration;
using MicroServiceSystem.BuildingBlocks.Storage.Providers;

namespace MicroServiceSystem.BuildingBlocks.Storage.Extensions;

public static class StorageExtensions
{
    /// <summary>
    /// Registers a single <see cref="IFileStorage"/> implementation chosen by configuration, so callers
    /// never branch on the provider.
    /// </summary>
    public static IServiceCollection AddFrameworkStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .ValidateOnStart();

        FileStorageOptions storageOptions = configuration.GetSection(FileStorageOptions.SectionName)
            .Get<FileStorageOptions>() ?? new FileStorageOptions();

        switch (storageOptions.Provider)
        {
            case FileStorageProvider.Local:
                services.AddSingleton<IFileStorage, LocalFileStorage>();
                break;

            case FileStorageProvider.Minio:
                AddS3(services, storageOptions.Minio, nameof(FileStorageProvider.Minio));
                break;

            case FileStorageProvider.AmazonS3:
                AddS3(services, storageOptions.AmazonS3, nameof(FileStorageProvider.AmazonS3));
                break;

            case FileStorageProvider.AzureBlob:
                services.AddSingleton(_ => new BlobServiceClient(storageOptions.AzureBlob.ConnectionString));
                services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
                break;

            case FileStorageProvider.GoogleCloudStorage:
                AddGoogleCloud(services, storageOptions.GoogleCloud);
                break;

            default:
                throw new InvalidOperationException(
                    $"Storage provider '{storageOptions.Provider}' is not supported.");
        }

        return services;
    }

    private static void AddS3(IServiceCollection services, S3CompatibleStorageOptions options, string providerName)
    {
        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ForcePathStyle = options.ForcePathStyle,
                UseHttp = !options.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                config.ServiceURL = options.ServiceUrl;
            }
            else if (!string.IsNullOrWhiteSpace(options.Region))
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
            }

            return new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
        });

        services.AddSingleton<IFileStorage>(serviceProvider =>
            new S3FileStorage(serviceProvider.GetRequiredService<IAmazonS3>(), providerName));
    }

    /// <summary>
    /// Without an explicit service account key the client falls back to application default
    /// credentials, which cannot sign URLs; the storage instance then reports that limitation on use.
    /// </summary>
    private static void AddGoogleCloud(IServiceCollection services, GoogleCloudStorageOptions options)
    {
        services.AddSingleton<IFileStorage>(_ =>
        {
            GoogleCredential? credential = LoadCredential(options.CredentialsJsonPath);

            StorageClient client = credential is null
                ? StorageClient.Create()
                : StorageClient.Create(credential);

            UrlSigner? urlSigner = credential is null ? null : UrlSigner.FromCredential(credential);

            return new GoogleCloudFileStorage(client, urlSigner);
        });
    }

    private static GoogleCredential? LoadCredential(string credentialsJsonPath) =>
        string.IsNullOrWhiteSpace(credentialsJsonPath)
            ? null
            : CredentialFactory.FromFile<ServiceAccountCredential>(credentialsJsonPath).ToGoogleCredential();
}
