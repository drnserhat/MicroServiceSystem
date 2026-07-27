using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.Storage.Abstractions;
using MicroServiceSystem.Services.File.Application.Abstractions;
using MicroServiceSystem.Services.File.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.File.Application;
// Tenant comes from the caller's token, not the form. It still has to be read explicitly here because
// the storage path is partitioned by tenant.
public sealed record UploadFileCommand(string FileName,string ContentType,byte[] Content,string Container):ICommand<FileAssetResponse>;
public sealed record FileAssetResponse(Guid Id,string FileName,string Container,string Path,long SizeInBytes,string StorageProvider);
public sealed class UploadFileCommandValidator:AbstractValidator<UploadFileCommand>{public UploadFileCommandValidator(){RuleFor(x=>x.FileName).NotEmpty();RuleFor(x=>x.ContentType).NotEmpty();RuleFor(x=>x.Content).NotEmpty();RuleFor(x=>x.Container).NotEmpty();}}
public sealed class UploadFileCommandHandler(IFileAssetRepository assets,IFileStorage storage,ICurrentTenant tenant):ICommandHandler<UploadFileCommand,FileAssetResponse>{public async Task<Result<FileAssetResponse>> Handle(UploadFileCommand c,CancellationToken ct){if(tenant.Id is not Guid tenantId){return FrameworkErrors.TenantMissing();}string path=$"{tenantId:N}/{Guid.CreateVersion7():N}-{c.FileName}";await using var content=new MemoryStream(c.Content);StoredFile stored=await storage.UploadAsync(new FileUploadRequest{Container=c.Container,Path=path,Content=content,ContentType=c.ContentType},ct);var asset=FileAsset.Create(c.FileName,stored.ContentType,stored.SizeInBytes,stored.Container,stored.Path,storage.ProviderName);await assets.AddAsync(asset,ct);return new FileAssetResponse(asset.Id,asset.FileName,asset.Container,asset.Path,asset.SizeInBytes,asset.StorageProvider);}}
