using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using WalletBusiness;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IWalletService, WalletService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

