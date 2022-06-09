using FaqBot.SentenceEncoding;

var modelPath = Path.Join(Environment.CurrentDirectory, "models/onnx/use-m_l_v3.onnx");
var encoder = new UniversalSentenceEncoder(modelPath);

var embedding = encoder.ComputeEmbedding("Test sentence!");

Console.WriteLine($"[{string.Join(", ", embedding)}]");
