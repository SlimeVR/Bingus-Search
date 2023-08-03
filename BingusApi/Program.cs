using AspNetCoreRateLimit;
using BingusApi.EmbeddingServices;
using BingusLib.Config;
using BingusLib.FaqHandling;
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
builder.Services.AddSingleton<IEmbeddingStore>(sp => new RocksDbStore(RocksDb.Open(new DbOptions().SetCreateIfMissing(true), "embedding_cache")));
builder.Services.AddSingleton(sp => BingusConfigUtils.InitializeConfig(sp.GetService<ILogger<BingusConfig>>()));
builder.Services.AddSingleton(sp => FaqConfigUtils.InitializeConfig(sp.GetService<ILogger<FaqConfig>>()));

builder.Services.AddSingleton(sp =>
{
    // Load the configs
    var bingusConfig = sp.GetRequiredService<BingusConfig>();
    var faqConfig = sp.GetRequiredService<FaqConfig>();

    // Setup the FAQ handler
    var modelPath = Path.Join(Environment.CurrentDirectory, bingusConfig.ModelPath);
    var faqHandler = new FaqHandler(sp.GetRequiredService<ILoggerFactory>(), modelPath, embeddingStore: sp.GetService<IEmbeddingStore>(), embeddingCache: sp.GetService<IEmbeddingCache>());

    // Add all questions
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
