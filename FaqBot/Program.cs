using FaqBot.SentenceEncoding;

var modelPath = Path.Join(Environment.CurrentDirectory, "models/onnx/use-m_l_v3.onnx");
var encoder = new UniversalSentenceEncoder(modelPath);

var vectorBuffer = new float[encoder.OutputDimension];

PrintEmbedding("dog");
PrintEmbedding("Puppies are nice.");
PrintEmbedding("I enjoy taking long walks along the beach with my dog.");

void PrintEmbedding(string input)
{
    Console.WriteLine($"{input}: [{string.Join(", ", encoder.ComputeEmbedding(input, vectorBuffer))}]");
}
