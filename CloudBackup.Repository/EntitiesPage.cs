
namespace CloudBackup.Repositories
{
    public class EntitiesPage
    {
        public int NumberOfEntitiesOnPage { get; private set; }

        /// <summary>
        /// 1-based number of current page.
        /// </summary>
        public int CurrentPage { get; private set; }

        public EntitiesPage(int numberOfEntitiesOnPage, int currentPage)
        {
            if (numberOfEntitiesOnPage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfEntitiesOnPage));
            }

            NumberOfEntitiesOnPage = numberOfEntitiesOnPage;

            if (currentPage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentPage));
            }

            CurrentPage = currentPage;
        }
    }

    public static class EntitiesPageExtensions
    {
        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, EntitiesPage page)
        {
            if (page == null)
            {
                return query;
            }

            var skip = (page.CurrentPage - 1) * page.NumberOfEntitiesOnPage;

            return query.Skip(skip).Take(page.NumberOfEntitiesOnPage);
        }

        public static IEnumerable<T> ApplyPaging<T>(this IEnumerable<T> query, EntitiesPage page)
        {
            return ApplyPaging(query.AsQueryable(), page).AsEnumerable();
        }
    }
}