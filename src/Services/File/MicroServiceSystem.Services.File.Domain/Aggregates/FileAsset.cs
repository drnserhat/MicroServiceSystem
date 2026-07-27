using MicroServiceSystem.SharedKernel.Guards;
using MicroServiceSystem.SharedKernel.Primitives;
namespace MicroServiceSystem.Services.File.Domain.Aggregates;
public sealed class FileAsset : TenantAggregateRoot<Guid>
{
    private FileAsset() { }
    private FileAsset(Guid id, string fileName, string contentType, long size, string container, string path, string provider) : base(id) { FileName=fileName; ContentType=contentType; SizeInBytes=size; Container=container; Path=path; StorageProvider=provider; }
    public string FileName { get; private set; } = string.Empty; public string ContentType { get; private set; } = string.Empty; public long SizeInBytes { get; private set; } public string Container { get; private set; } = string.Empty; public string Path { get; private set; } = string.Empty; public string StorageProvider { get; private set; } = string.Empty;
    public static FileAsset Create(string fileName, string contentType, long size, string container, string path, string provider) { Ensure.NotNullOrWhiteSpace(fileName); Ensure.NotNullOrWhiteSpace(contentType); Ensure.Positive(checked((int)size)); Ensure.NotNullOrWhiteSpace(container); Ensure.NotNullOrWhiteSpace(path); return new(Guid.CreateVersion7(), fileName, contentType, size, container, path, provider); }
}
