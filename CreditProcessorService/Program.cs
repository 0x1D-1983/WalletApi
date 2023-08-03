using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using WalletBusiness;
using WalletDomain;

IHost host = Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
{
    services.Configure<SchemaRegistryConfig>(hostContext.Configuration.GetSection("SchemaRegistry"));
    services.Configure<ProducerConfig>(hostContext.Configuration.GetSection("Producer"));
    services.Configure<ConsumerConfig>(hostContext.Configuration.GetSection("Consumer"));

    // schema registry
    services.AddSingleton<ISchemaRegistryClient>(sp =>
    {
        var regConfig = sp.GetRequiredService<IOptions<SchemaRegistryConfig>>();
        return new CachedSchemaRegistryClient(regConfig.Value);
    });

    // producer
    services.AddSingleton<IProducer<String, BalanceDetailsModel>>(sp =>
    {
        var config = sp.GetRequiredService<IOptions<ProducerConfig>>();

        var schemaRegistry = sp.GetRequiredService<ISchemaRegistryClient>();

        return new ProducerBuilder<String, BalanceDetailsModel>(config.Value)
            .SetValueSerializer(new JsonSerializer<BalanceDetailsModel>(schemaRegistry))
            .Build();
    });

    // consumer
    services.AddSingleton<IConsumer<String, WalletOperation>>(sp =>
    {
        var config = sp.GetRequiredService<IOptions<ConsumerConfig>>();

        return new ConsumerBuilder<String, WalletOperation>(config.Value)
            .SetValueDeserializer(new JsonDeserializer<WalletOperation>().AsSyncOverAsync())
            .Build();
    });

    // redis
    services.AddSingleton<IRedisService>(sp =>
        new RedisService(hostContext.Configuration["Redis:Host"] ??
        throw new ArgumentNullException("Redis host is missing.")));

    services.AddHostedService<CreditWalletWorker>();
}).Build();

await host.RunAsync();