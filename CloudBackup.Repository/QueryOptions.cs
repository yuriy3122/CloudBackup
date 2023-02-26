using System.Linq.Expressions;

namespace CloudBackup.Repositories
{
    public class QueryOptions
    {
        public EntitiesPage Page { get; set; } = null!;

        public string OrderBy { get; set; } = null!;

        public string Filter { get; set; } = null!;

        public QueryOptions()
        {
        }

        public QueryOptions(EntitiesPage page, string order, string filter)
        {
            Page = page;
            OrderBy = order;
            Filter = filter;
        }

        public virtual QueryOptions Clone()
        {
            return new QueryOptions
            {
                Page = Page,
                OrderBy = OrderBy,
                Filter = Filter
            };
        }
    }

    public class QueryOptions<T> : QueryOptions
    {
        public Expression<Func<T, bool>> FilterExpression { get; set; } = null!;

        public QueryOptions()
        {
        }

        public QueryOptions(EntitiesPage page, string order, string filter)
            : base(page, order, filter)
        {
        }

        public QueryOptions(EntitiesPage page, string order, string filter, Expression<Func<T, bool>> filterExpression)
            : base(page, order, filter)
        {
            FilterExpression = filterExpression;
        }

        public override QueryOptions Clone()
        {
            return new QueryOptions<T>
            {
                Page = Page,
                OrderBy = OrderBy,
                Filter = Filter,
                FilterExpression = FilterExpression
            };
        }
    }
}
