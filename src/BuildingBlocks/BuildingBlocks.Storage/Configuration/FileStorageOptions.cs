namespace MicroServiceSystem.BuildingBlocks.Storage.Configuration;

public sealed class FileStorageOptions
{
    public const string SectionName = "Storage";

    public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;

    public string DefaultContainer { get; set; } = "files";

    public long MaxFileSizeInBytes { get; set; } = 50L * 1024 * 1024;

    public string[] AllowedContentTypes { get; set; } = [];

    public LocalStorageOptions Local { get; set; } = new();

    public S3CompatibleStorageOptions Minio { get; set; } = new();

    public S3CompatibleStorageOptions AmazonS3 { get; set; } = new();

    public AzureBlobStorageOptions AzureBlob { get; set; } = new();

    public GoogleCloudStorageOptions GoogleCloud { get; set; } = new();
}

public enum FileStorageProvider
{
    Local = 0,
    Minio = 1,
    AmazonS3 = 2,
    AzureBlob = 3,
    GoogleCloudStorage = 4
}

public sealed class LocalStorageOptions
{
    public string RootPath { get; set; } = "storage";

    public string PublicBaseUrl { get; set; } = string.Empty;
}

public sealed class S3CompatibleStorageOptions
{
    public string ServiceUrl { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public bool UseSsl { get; set; } = true;

    public bool ForcePathStyle { get; set; } = true;
}

public sealed class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class GoogleCloudStorageOptions
{
    public string CredentialsJsonPath { get; set; } = string.Empty;

    public string ProjectId { get; set; } = string.Empty;
}
