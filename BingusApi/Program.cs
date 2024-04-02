using AspNetCoreRateLimit;
using BingusApi.EmbeddingServices;
using BingusLib.Config;
using BingusLib.FaqHandling;
using BingusLib.SentenceEncoding;
using BingusLib.SentenceEncoding.Api;
using RocksDbSharp;

var builder = WebApplication.CreateBuilder(args);

var configDir = Path.GetFullPath("config");
var appSettings = "appsettings.json";
var appSettingsEnv = $"appsettings.{builder.Environment.EnvironmentName}.json";
var bingusConfig = "bingus_config.json";
var faqConfig = "faq_config.json";

builder.Configuration.SetBasePath(configDir);
string GetConfig(string fileName)
{
    return builder.Configuration.GetFileProvider().GetFileInfo(fileName).PhysicalPath
        ?? Path.Combine(configDir, fileName);
}

// Load appsettings.json
builder.Configuration.AddJsonFile(GetConfig(appSettings), false, true);
builder.Configuration.AddJsonFile(GetConfig(appSettingsEnv), true, true);
builder.Services.AddOptions();

// Set up ratelimiting
// Needed to store rate limit counters and ip rules
builder.Services.AddMemoryCache();

// Load general configuration from appsettings.json
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// Load IP rules from appsettings.json
builder.Services.Configure<IpRateLimitPolicies>(
    builder.Configuration.GetSection("IpRateLimitPolicies")
);

// Inject counter and rules stores
builder.Services.AddInMemoryRateLimiting();

// Configuration (resolvers, counter key builders)
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add dependencies
builder.Services.AddSingleton<IEmbeddingStore>(sp => new RocksDbStore(
    RocksDb.Open(new DbOptions().SetCreateIfMissing(true), "embedding_cache")
));
builder.Services.AddSingleton(sp =>
    new JsonConfigHandler<BingusConfig>(GetConfig(bingusConfig)).InitializeConfig(
        BingusConfig.Default,
        sp.GetService<ILogger<BingusConfig>>()
    )
);
builder.Services.AddSingleton(sp =>
    new JsonConfigHandler<FaqConfig>(GetConfig(faqConfig)).InitializeConfig(
        FaqConfig.Default,
        sp.GetService<ILogger<FaqConfig>>()
    )
);
builder.Services.AddSingleton(sp => new HttpClient());

builder.Services.AddSingleton<SentenceEncoder>(sp =>
{
    // Load the config
    var bingusConfig = sp.GetRequiredService<BingusConfig>();

    // Select and set up the sentence encoder based on the config
    switch (bingusConfig.EncoderType.ToLower())
    {
        case "use":
            var modelPath = Path.Join(Environment.CurrentDirectory, bingusConfig.UseModelPath);
            return new UniversalSentenceEncoder(
                modelPath,
                sp.GetService<ILogger<UniversalSentenceEncoder>>()
            );

        case "api":
            return new ApiSentenceEncoder(
                sp.GetRequiredService<HttpClient>(),
                new Uri(bingusConfig.ApiUri)
            );

        default:
            throw new Exception("No valid sentence encoder type was selected.");
    }
});

builder.Services.AddSingleton(sp =>
{
    // Set up the FAQ handler
    var faqConfig = sp.GetRequiredService<FaqConfig>();
    var faqHandler = new FaqHandler(
        sp.GetRequiredService<SentenceEncoder>(),
        sp.GetService<IEmbeddingStore>(),
        sp.GetService<IEmbeddingCache>(),
        sp.GetService<ILogger<FaqHandler>>()
    );

    // Add all questions from the config
    faqHandler.AddItems(faqConfig.QaEntryEnumerator());

    return faqHandler;
});

// Add services to the container
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SupportNonNullableReferenceTypes());

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseIpRateLimiting();

// app.UseHttpsRedirection();
// app.UseAuthorization();
app.MapControllers();

// Service warmup (these tasks do not need to be awaited, it will run in the background)
#pragma warning disable CS4014
Task.Run(app.Services.GetService<FaqHandler>);
#pragma warning restore CS4014

app.Run();
