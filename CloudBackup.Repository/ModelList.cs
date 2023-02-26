
namespace CloudBackup.Repositories
{
    public class ModelList<T>
    {
        public List<T> Items { get; set; }

        public int TotalCount { get; set; }

        public ModelList()
        {
            Items = new List<T>();
        }

        public ModelList(IEnumerable<T> items)
        {
            Items = items.ToList();
            TotalCount = Items.Count;
        }

        public ModelList(IEnumerable<T> items, int totalCount)
        {
            Items = items.ToList();
            TotalCount = totalCount;
        }
    }

    public static class ModelList
    {
        public static ModelList<T> Create<T>(IEnumerable<T> items)
        {
            return new ModelList<T>(items);
        }

        public static ModelList<T> Create<T>(IEnumerable<T> items, int totalCount)
        {
            return new ModelList<T>(items, totalCount);
        }
    }
}
