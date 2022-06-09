using FaqBot.SentenceEncoding;
using Microsoft.ML;
using Microsoft.ML.Data;

var modelPath = Path.Join(Environment.CurrentDirectory, "universal-sentence-encoder-multilingual-large_3");
var encoder = new UniversalSentenceEncoder(modelPath);

var embedding = encoder.ComputeEmbedding("Test sentence!");
Console.WriteLine(embedding);
Console.WriteLine(embedding.Length);
Console.WriteLine($"[{string.Join(", ", embedding)}]");
