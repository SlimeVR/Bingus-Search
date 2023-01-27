using AspNetCoreRateLimit;
using BingusApi;
using BingusLib.FaqHandling;
using BingusLib.HNSW;
using MessagePack;
using MessagePack.Resolvers;
using RocksDbSharp;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.json
builder.Services.AddOptions();

// Set up ratelimiting
// Needed to store rate limit counters and ip rules
builder.Services.AddMemoryCache();

// Load general configuration from appsettings.json
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// Load IP rules from appsettings.json
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

// Inject counter and rules stores
builder.Services.AddInMemoryRateLimiting();

// configuration (resolvers, counter key builders)
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add dependencies
builder.Services.AddSingleton<IEmbeddingCache>(sp => new RocksDbCache(RocksDb.Open(new DbOptions().SetCreateIfMissing(true), "embedding_cache")));
builder.Services.AddSingleton(sp => FaqConfigUtils.InitializeConfig(sp.GetService<ILogger<FaqConfig>>()));

builder.Services.AddSingleton(sp =>
{
    // Load the config
    var faqConfig = sp.GetRequiredService<FaqConfig>();

    // Setup the FAQ handler
    var modelPath = Path.Join(Environment.CurrentDirectory, faqConfig.ModelPath);
    var faqHandler = new FaqHandler(sp.GetRequiredService<ILoggerFactory>(), modelPath, embeddingCache: sp.GetService<IEmbeddingCache>());

    // Add all questions
    faqHandler.AddItems(faqConfig.QaEntryEnumerator());

    // Setup HNSW serialization stuff
    StaticCompositeResolver.Instance.Register(MessagePackSerializer.DefaultOptions.Resolver);
    StaticCompositeResolver.Instance.Register(
        new LazyKeyItemFormatter<int, float[]>(i => faqHandler.GetEntry(i).Vector!.AsArray()));
    MessagePackSerializer.DefaultOptions.WithResolver(StaticCompositeResolver.Instance);

    return faqHandler;
});

// Add services to the container
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIpRateLimiting();
// app.UseHttpsRedirection();
// app.UseAuthorization();
app.MapControllers();

// Service warmup
Task.Run(app.Services.GetService<FaqHandler>);

app.Run();
