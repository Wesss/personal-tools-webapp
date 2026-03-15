using Utils.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddControllers();
// allow webapp to hit API
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAnyOriginPolicy",
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    );
});

var app = builder.Build();

// expose logger globally
GlobalLogger.LoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

// Configure the HTTP request pipeline.
app.UseAuthorization();
// allow webapp to hit API
app.UseCors("AllowAnyOriginPolicy");
app.MapControllers();
app.Run();
