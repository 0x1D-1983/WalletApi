using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using WalletBusiness;
using WalletDomain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IRedisService, RedisService>();
builder.Services.AddSingleton<IWalletService, WalletService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Kafka
builder.Services.Configure<ProducerConfig>(builder.Configuration.GetSection("Producer"));
builder.Services.Configure<SchemaRegistryConfig>(builder.Configuration.GetSection("SchemaRegistry"));

builder.Services.AddSingleton<IProducer<String, WalletOperation>>(sp =>
{
    var config = sp.GetRequiredService<IOptions<ProducerConfig>>();

    var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();

    return new ProducerBuilder<String, WalletOperation>(config.Value)
        .SetValueSerializer(new JsonSerializer<WalletOperation>(schemaRegistry))
        .Build();
});

builder.Services.AddSingleton<ISchemaRegistryClient>(sp =>
{
    var regConfig = sp.GetRequiredService<IOptions<SchemaRegistryConfig>>();
    return new CachedSchemaRegistryClient(regConfig.Value);
});

// redis
builder.Services.AddSingleton<IDatabase>(sp =>
{
    var redisConnMultiplex = ConnectionMultiplexer.Connect(
        builder.Configuration["Redis:Host"] ??
        throw new ArgumentNullException("Redis host is missing."));
    return redisConnMultiplex.GetDatabase();
});

// redlock
builder.Services.AddSingleton<IDistributedLockFactory>(sp =>
{
    var redisConnMultiplex = ConnectionMultiplexer.Connect(builder.Configuration["Redis:Host"] ??
        throw new ArgumentNullException("Redis host is missing."));
    var multiplexers = new List<RedLockMultiplexer> { redisConnMultiplex };
    return RedLockFactory.Create(multiplexers);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

