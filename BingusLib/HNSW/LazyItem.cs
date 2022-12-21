namespace BingusLib.HNSW
{
    public class LazyItem<TItem> : ILazyItem<TItem>
    {
        private readonly Func<TItem> _getValue;
        public TItem Value => _getValue();

        public LazyItem(Func<TItem> getValue)
        {
            _getValue = getValue;
        }

        public LazyItem(TItem value) : this(() => value)
        {
        }
    }
}
