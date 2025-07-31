using System.Text;
using BingusLib.FaqHandling;
using MathNet.Numerics.LinearAlgebra;
using RocksDbSharp;

namespace BingusApi.EmbeddingServices
{
    public class RocksDbStore : IEmbeddingStore, IDisposable
    {
        public const string ModelUidKey = "<model_uid>";

        private readonly RocksDb rocksDb;

        public RocksDbStore(RocksDb rocksDb)
        {
            this.rocksDb = rocksDb;
        }

        private static byte[] SerializeString(string value) => Encoding.UTF8.GetBytes(value);

        private static string DeserializeString(byte[] value) => Encoding.UTF8.GetString(value);

        public void Put(string key, Vector<float> embedding)
        {
            Put(key, RocksDbSerializer.SerializeVector(embedding));
        }

        public void Put(string key, float[] embedding)
        {
            Put(key, RocksDbSerializer.SerializeArray(embedding));
        }

        public void Put(string key, byte[] data)
        {
            rocksDb.Put(SerializeString(key), data);
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

        public void Dispose()
        {
            rocksDb?.Dispose();
            GC.SuppressFinalize(this);
        }

        public static RocksDbStore Create(
            string dbPath,
            string modelUid,
            ILogger<RocksDbStore>? logger
        )
        {
            var preexisting = Directory.Exists(dbPath) && Directory.GetFiles(dbPath).Length > 0;

            var options = new DbOptions().SetCreateIfMissing(true);
            var db = RocksDb.Open(options, dbPath);
            var store = new RocksDbStore(db);

            var lastModelUidB = store.GetBytes(ModelUidKey);
            if (lastModelUidB != null)
            {
                var lastModelUid = DeserializeString(lastModelUidB);
                if (lastModelUid != modelUid)
                {
                    logger?.LogWarning(
                        "Model UID has changed from \"{LastModelUid}\" to \"{ModelUid}\". Deleting the store.",
                        lastModelUid,
                        modelUid
                    );

                    // Delete the store
                    store.Dispose();
                    foreach (var file in Directory.GetFiles(dbPath))
                    {
                        File.Delete(file);
                    }

                    // Recreate the store
                    return Create(dbPath, modelUid, logger);
                }
            }
            else
            {
                if (preexisting)
                {
                    logger?.LogWarning(
                        "No model UID found in existing store. Writing current model UID \"{ModelUid}\".",
                        modelUid
                    );
                }
                store.Put(ModelUidKey, SerializeString(modelUid));
            }

            return store;
        }
    }
}
