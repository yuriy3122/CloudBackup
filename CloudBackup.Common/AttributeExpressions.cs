using System.Linq.Expressions;

namespace CloudBackup.Common
{
    /// <summary>
    /// https://blogs.msdn.microsoft.com/meek/2008/05/02/linq-to-entities-combining-predicates/
    /// </summary>
    public static class ExpressionExtensions
    {
        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            if (first == null && second == null)
                throw new ArgumentException("First or second expressions must be set.");
            if (merge == null)
                throw new ArgumentNullException(nameof(merge));

            if (first == null) return second;
            if (second == null) return first;

            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<T, bool>> DefaultTrue<T>(this Expression<Func<T, bool>> expression)
        {
            if (expression == null)
            {
                return e => true;
            }

            return expression;
        }

        public static Expression<Func<T, bool>> DefaultFalse<T>(this Expression<Func<T, bool>> expression)
        {
            if (expression == null)
            {
                return e => false;
            }

            return expression;
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.DefaultTrue().Compose(second, Expression.AndAlso);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.DefaultFalse().Compose(second, Expression.OrElse);
        }
    }
}
