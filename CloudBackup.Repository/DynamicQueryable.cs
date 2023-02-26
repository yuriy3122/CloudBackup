using System.Reflection;
using System.Linq.Expressions;

namespace CloudBackup.Repositories
{
    public static class DynamicQueryable
    {
        public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, string ordering)
        {
            if (string.IsNullOrEmpty(ordering))
            {
                return source;
            }

            var values = ordering.Split('[');
            var orderType = values.Length > 1 && values[1].ToLower().Contains("desc") ? "desc" : "asc";
            var orderColumn = values[0].Trim();

            var type = typeof(T);
            var parameter = Expression.Parameter(type, "p");

            Expression body = parameter;
            var currentType = type;

            foreach (var member in orderColumn.Split('.'))
            {
                var property = currentType.GetProperty(member, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null)
                {
                    body = Expression.MakeMemberAccess(body, property);
                    currentType = property.PropertyType;
                }
            }
            var orderByExp = Expression.Lambda(body, parameter);
            string methodName = orderType == "asc" ? "OrderBy" : "OrderByDescending";

            var resultExp = Expression.Call(typeof(Queryable), methodName, new[] { type, currentType }, source.Expression, Expression.Quote(orderByExp));

            return source.Provider.CreateQuery<T>(resultExp);
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> source, string ordering)
        {
            return source.AsQueryable().OrderBy(ordering).AsEnumerable();
        }
    }
}