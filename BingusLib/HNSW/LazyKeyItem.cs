using MessagePack;
using MessagePack.Formatters;

namespace BingusLib.HNSW
{
    public class LazyKeyItem<TKey, TItem> : LazyItem<TItem>
    {
        private readonly TKey _key;
        public TKey Key => _key;

        public LazyKeyItem(TKey key, Func<TItem> getValue) : base(getValue)
        {
            _key = key;
        }

        public LazyKeyItem(TKey key, Func<TKey, TItem> getValue) : base(() => getValue(key))
        {
            _key = key;
        }
    }

    public class LazyKeyItemFormatter<TKey, TItem> : IMessagePackFormatter<LazyKeyItem<TKey, TItem>>
    {
        private readonly Func<TKey, TItem> _itemResolver;

        public LazyKeyItemFormatter(Func<TKey, TItem> itemResolver)
        {
            _itemResolver = itemResolver;
        }

        public TItem ResolveItem(TKey key)
        {
            return _itemResolver(key);
        }

        public void Serialize(ref MessagePackWriter writer, LazyKeyItem<TKey, TItem> value, MessagePackSerializerOptions options)
        {
            var formatter = options.Resolver.GetFormatter<TKey>();
            formatter.Serialize(ref writer, value.Key, options);
        }

        public LazyKeyItem<TKey, TItem> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var formatter = options.Resolver.GetFormatter<TKey>();
            var key = formatter.Deserialize(ref reader, options);
            return new LazyKeyItem<TKey, TItem>(key, () => ResolveItem(key));
        }
    }
}
