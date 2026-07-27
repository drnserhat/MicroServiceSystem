using FluentValidation;
using MicroServiceSystem.BuildingBlocks.Application.Messaging;
using MicroServiceSystem.BuildingBlocks.Storage.Abstractions;
using MicroServiceSystem.Services.File.Application.Abstractions;
using MicroServiceSystem.Services.File.Domain.Aggregates;
using MicroServiceSystem.SharedKernel.Abstractions;
using MicroServiceSystem.SharedKernel.Results;
namespace MicroServiceSystem.Services.File.Application;
public sealed record UploadFileCommand(string FileName,string ContentType,byte[] Content,string Container,Guid TenantId):ICommand<FileAssetResponse>;
public sealed record FileAssetResponse(Guid Id,string FileName,string Container,string Path,long SizeInBytes,string StorageProvider);
public sealed class UploadFileCommandValidator:AbstractValidator<UploadFileCommand>{public UploadFileCommandValidator(){RuleFor(x=>x.FileName).NotEmpty();RuleFor(x=>x.ContentType).NotEmpty();RuleFor(x=>x.Content).NotEmpty();RuleFor(x=>x.Container).NotEmpty();RuleFor(x=>x.TenantId).NotEmpty();}}
public sealed class UploadFileCommandHandler(IFileAssetRepository assets,IFileStorage storage,ICurrentTenant tenant):ICommandHandler<UploadFileCommand,FileAssetResponse>{public async Task<Result<FileAssetResponse>> Handle(UploadFileCommand c,CancellationToken ct){using IDisposable scope=tenant.Change(c.TenantId);string path=$"{c.TenantId:N}/{Guid.CreateVersion7():N}-{c.FileName}";await using var content=new MemoryStream(c.Content);StoredFile stored=await storage.UploadAsync(new FileUploadRequest{Container=c.Container,Path=path,Content=content,ContentType=c.ContentType},ct);var asset=FileAsset.Create(c.FileName,stored.ContentType,stored.SizeInBytes,stored.Container,stored.Path,storage.ProviderName);asset.TenantId=c.TenantId;await assets.AddAsync(asset,ct);return new FileAssetResponse(asset.Id,asset.FileName,asset.Container,asset.Path,asset.SizeInBytes,asset.StorageProvider);}}
