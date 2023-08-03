using System.Text;
using BingusLib.FaqHandling;
using MathNet.Numerics.LinearAlgebra;
using RocksDbSharp;

namespace BingusApi.EmbeddingServices
{
    public class RocksDbStore : IEmbeddingStore
    {
        private readonly RocksDb rocksDb;

        public RocksDbStore(RocksDb rocksDb)
        {
            this.rocksDb = rocksDb;
        }

        private static byte[] SerializeString(string value) => Encoding.UTF8.GetBytes(value);

        public void Add(string key, Vector<float> embedding)
        {
            rocksDb.Put(SerializeString(key), RocksDbSerializer.SerializeVector(embedding));
        }

        public void Add(string key, float[] embedding)
        {
            rocksDb.Put(SerializeString(key), RocksDbSerializer.SerializeArray(embedding));
        }

        public byte[]? GetBytes(string key)
        {
            return rocksDb.Get(SerializeString(key));
        }

        public Vector<float>? Get(string key)
        {
            var result = GetBytes(key);
            return result != null ? RocksDbSerializer.DeserializeVector(result) : null;
        }

        public float[]? GetRaw(string key)
        {
            var result = GetBytes(key);
            return result != null ? RocksDbSerializer.DeserializeFloatArray(result) : null;
        }

        public bool Has(string key)
        {
            return rocksDb.HasKey(SerializeString(key));
        }
    }
}
