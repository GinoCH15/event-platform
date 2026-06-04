using MassTransit;
using NotificationService.Application.Consumers;
using NotificationService.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ─── MongoDB ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<NotificationMongoContext>();

// ─── MassTransit + RabbitMQ ───────────────────────────────────────────────────
builder.Services.AddMassTransit(mt =>
{
    mt.AddConsumer<EventCreatedConsumer>();

    mt.UsingRabbitMq((ctx, cfg) =>
    {
        var rmq = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(rmq["Host"] ?? "rabbitmq", rmq["VHost"] ?? "/", h =>
        {
            h.Username(rmq["Username"] ?? "guest");
            h.Password(rmq["Password"] ?? "guest");
        });

        // Cola dedicada para este consumidor
        cfg.ReceiveEndpoint("notification-service-event-created", ep =>
        {
            ep.ConfigureConsumer<EventCreatedConsumer>(ctx);
            ep.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)
            ));
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "NotificationService" }));

app.Run();
