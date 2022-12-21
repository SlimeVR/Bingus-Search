namespace FaqBot.HNSW
{
    public interface ILazyItem<TItem>
    {
        public TItem Value { get; }
    }
}
