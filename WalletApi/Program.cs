using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using WalletBusiness;
using WalletDomain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<IWalletService, WalletService>();
builder.Services.AddSingleton<IRedisService>(sp =>
    new RedisService(builder.Configuration["Redis:Host"] ??
    throw new ArgumentNullException("Redis host is missing.")));

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

