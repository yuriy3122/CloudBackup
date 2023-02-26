namespace CloudBackup.Common
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TSource> EmptyIfNull<TSource>(this IEnumerable<TSource> source)
        {
            return source ?? Enumerable.Empty<TSource>();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;

            if (enumerable is ICollection<T> collection)
                return collection.Count == 0;

            return !enumerable.Any();
        }

        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size)
        {
            List<TSource>? batch = null;

            foreach (var item in source)
            {
                if (batch == null)
                    batch = new List<TSource>(size);

                batch.Add(item);

                if (batch.Count == size)
                {
                    yield return batch;
                    batch = null;
                }
            }

            if (batch?.Count > 0)
                yield return batch;
        }

        public static IEnumerable<IReadOnlyList<T>> GroupAdjacentBy<T>(this IEnumerable<T> source, Func<T, T, bool> predicate)
        {
            using var e = source.GetEnumerator();
            var list = new List<T>();

            while (e.MoveNext())
            {
                if (list.Any() && !predicate(list.Last(), e.Current))
                {
                    yield return list;
                    list = new List<T>();
                }

                list.Add(e.Current);
            }

            yield return list;
        }
    }
}