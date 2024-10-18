using MathNet.Numerics.LinearAlgebra;

namespace BingusLib.FaqHandling
{
    public record class FaqEntry
    {
        public string Title { get; set; } = "";
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public Vector<float>? Vector { get; set; }
    }
}
