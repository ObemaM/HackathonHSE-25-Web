using System.Linq.Expressions;

namespace HackathonBackend.Utils
{
    // Работа c LINQ, используется для создания сложных OR/AND условий в запросах
    public static class LINQBuilder
    {
        // Предикат True для всех
        public static Expression<Func<T, bool>> True<T>() => param => true;

        // Предикат False для всех
        public static Expression<Func<T, bool>> False<T>() => param => false;

        // Объединяет два предиката с помощью OR
        public static Expression<Func<T, bool>> Or<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(left!, right!), parameter);
        }

        // Объединяет два предиката с помощью AND
        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
            var left = leftVisitor.Visit(expr1.Body);

            var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
            var right = rightVisitor.Visit(expr2.Body);

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(left!, right!), parameter);
        }

        // Visitor для замены параметров в выражениях
        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression oldValue;
            private readonly Expression newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                this.oldValue = oldValue;
                this.newValue = newValue;
            }

            public override Expression? Visit(Expression? node)
            {
                if (node == oldValue)
                    return newValue;
                return base.Visit(node);
            }
        }
    }
}
