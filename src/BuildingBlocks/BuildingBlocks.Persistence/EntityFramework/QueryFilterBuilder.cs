using System.Linq.Expressions;
using System.Reflection;
using MicroServiceSystem.SharedKernel.Abstractions;

namespace MicroServiceSystem.BuildingBlocks.Persistence.EntityFramework;

/// <summary>
/// Builds the tenant and soft delete global query filters for an entity type. Filters are generated
/// from the interfaces the entity implements so a new aggregate is protected the moment it is mapped.
/// </summary>
internal static class QueryFilterBuilder
{
    public static LambdaExpression? Build(Type entityType, FrameworkDbContext context)
    {
        ParameterExpression parameter = Expression.Parameter(entityType, "entity");

        Expression? body = null;

        if (typeof(ITenantEntity).IsAssignableFrom(entityType))
        {
            body = BuildTenantFilter(entityType, parameter, context);
        }

        if (typeof(ISoftDeletable).IsAssignableFrom(entityType))
        {
            Expression softDeleteFilter = BuildSoftDeleteFilter(entityType, parameter, context);

            body = body is null ? softDeleteFilter : Expression.AndAlso(body, softDeleteFilter);
        }

        return body is null ? null : Expression.Lambda(body, parameter);
    }

    /// <summary>
    /// A null ambient tenant means the caller is a migration, seeder or a tenant independent worker, so
    /// the filter opens up instead of hiding every row.
    /// </summary>
    private static Expression BuildTenantFilter(Type entityType, Expression parameter, FrameworkDbContext context)
    {
        MemberExpression currentTenantId = Expression.Property(
            Expression.Constant(context),
            nameof(FrameworkDbContext.CurrentTenantId));

        MemberExpression entityTenantId = Expression.Property(
            parameter,
            GetProperty(entityType, nameof(ITenantEntity.TenantId)));

        return Expression.OrElse(
            Expression.Equal(currentTenantId, Expression.Constant(null, typeof(Guid?))),
            Expression.Equal(Expression.Convert(entityTenantId, typeof(Guid?)), currentTenantId));
    }

    private static Expression BuildSoftDeleteFilter(Type entityType, Expression parameter, FrameworkDbContext context)
    {
        MemberExpression includeDeleted = Expression.Property(
            Expression.Constant(context),
            nameof(FrameworkDbContext.IncludeSoftDeleted));

        MemberExpression isDeleted = Expression.Property(
            parameter,
            GetProperty(entityType, nameof(ISoftDeletable.IsDeleted)));

        return Expression.OrElse(includeDeleted, Expression.Not(isDeleted));
    }

    private static PropertyInfo GetProperty(Type entityType, string propertyName) =>
        entityType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
        ?? throw new InvalidOperationException(
            $"Entity '{entityType.FullName}' does not expose a public '{propertyName}' property.");
}
