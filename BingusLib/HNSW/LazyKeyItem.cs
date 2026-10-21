namespace BingusLib.HNSW
{
    public record class LazyKeyItem<TKey, TItem> : LazyItem<TItem>
    {
        private readonly TKey _key;
        public TKey Key => _key;

        public LazyKeyItem(TKey key, Func<TItem> getValue)
            : base(getValue)
        {
            _key = key;
        }

        public LazyKeyItem(TKey key, Func<TKey, TItem> getValue)
            : base(() => getValue(key))
        {
            _key = key;
        }
    }
}
