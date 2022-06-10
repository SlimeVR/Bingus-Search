using FaqBot.SentenceEncoding;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => { builder.AddSimpleConsole(options => { options.TimestampFormat = "[hh:mm:ss] "; }); });
var logger = loggerFactory.CreateLogger<Program>();

var modelPath = Path.Join(Environment.CurrentDirectory, "models/onnx/use-m_l_v3.onnx");
using var encoder = new UniversalSentenceEncoder(loggerFactory.CreateLogger<UniversalSentenceEncoder>(), modelPath);

var vectorBuffer = new float[encoder.OutputDimension];

PrintEmbedding("dog");
PrintEmbedding("Puppies are nice.");
PrintEmbedding("I enjoy taking long walks along the beach with my dog.");

void PrintEmbedding(string input)
{
    logger.LogInformation($"{input}: [{string.Join(", ", encoder.ComputeEmbedding(input, vectorBuffer))}]");
}
