using MathNet.Numerics.LinearAlgebra;

namespace BingusApi
{
    public static class RocksDbSerializer
    {
        public static byte[] SerializeArray(float[] data)
        {
            var byteData = new byte[data.Length * sizeof(float)];
            Buffer.BlockCopy(data, 0, byteData, 0, byteData.Length);

            return byteData;
        }

        public static byte[] SerializeVector(Vector<float> data)
        {
            return SerializeArray(data.AsArray() ?? data.ToArray());
        }

        public static float[] DeserializeFloatArray(byte[] data)
        {
            var floatData = new float[data.Length / sizeof(float)];
            Buffer.BlockCopy(data, 0, floatData, 0, data.Length);

            return floatData;
        }

        public static Vector<float> DeserializeVector(byte[] data)
        {
            var floatData = new float[data.Length / sizeof(float)];
            Buffer.BlockCopy(data, 0, floatData, 0, data.Length);

            return Vector<float>.Build.DenseOfArray(floatData);
        }
    }
}
