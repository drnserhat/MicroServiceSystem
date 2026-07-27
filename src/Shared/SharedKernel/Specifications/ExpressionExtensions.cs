using System.Linq.Expressions;

namespace MicroServiceSystem.SharedKernel.Specifications;

public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> AndAlso<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right) =>
        Combine(left, right, Expression.AndAlso);

    public static Expression<Func<T, bool>> OrElse<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right) =>
        Combine(left, right, Expression.OrElse);

    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression) =>
        Expression.Lambda<Func<T, bool>>(Expression.Not(expression.Body), expression.Parameters);

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), left.Parameters[0].Name);
        Expression leftBody = new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!;
        Expression rightBody = new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!;

        return Expression.Lambda<Func<T, bool>>(merge(leftBody, rightBody), parameter);
    }

    private sealed class ParameterRebinder(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
