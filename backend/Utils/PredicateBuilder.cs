using System.Linq.Expressions;

namespace HackathonBackend.Utils
{
    /// <summary>
    /// PredicateBuilder для динамического построения LINQ выражений
    /// Используется для создания сложных OR/AND условий в запросах
    /// </summary>
    public static class PredicateBuilder
    {
        /// <summary>
        /// Создает предикат, который всегда возвращает true
        /// </summary>
        public static Expression<Func<T, bool>> True<T>() => param => true;

        /// <summary>
        /// Создает предикат, который всегда возвращает false
        /// </summary>
        public static Expression<Func<T, bool>> False<T>() => param => false;

        /// <summary>
        /// Объединяет два предиката с помощью OR
        /// </summary>
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

        /// <summary>
        /// Объединяет два предиката с помощью AND
        /// </summary>
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

        /// <summary>
        /// Visitor для замены параметров в выражениях
        /// </summary>
        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression? Visit(Expression? node)
            {
                if (node == _oldValue)
                    return _newValue;
                return base.Visit(node);
            }
        }
    }
}
