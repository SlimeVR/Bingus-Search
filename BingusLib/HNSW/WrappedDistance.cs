namespace BingusLib.HNSW
{
    public class WrappedDistance<TWrapper, TItem, TDistance>
    {
        public readonly Func<TWrapper, TItem> GetItem;
        public readonly Func<TItem, TItem, TDistance> Distance;

        public WrappedDistance(
            Func<TWrapper, TItem> getItem,
            Func<TItem, TItem, TDistance> distance
        )
        {
            GetItem = getItem;
            Distance = distance;
        }

        public TDistance WrappedDistanceFunc(TWrapper a, TWrapper b)
        {
            return Distance(GetItem(a), GetItem(b));
        }
    }
}
