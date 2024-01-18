namespace BingusLib.HNSW
{
    public readonly struct LazyItemValue<TItem> : ILazyItem<TItem>
    {
        private readonly Func<TItem> _getValue;
        public TItem Value => _getValue();

        public LazyItemValue(Func<TItem> getValue)
        {
            _getValue = getValue;
        }

        public LazyItemValue(TItem value)
            : this(() => value) { }
    }
}
