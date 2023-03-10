using Serilog;
using Serilog.Formatting.Compact;
using Youtube_to_Mp3_convertor.Helper;

var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();


// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<YoutubeHelper>();
builder.Services.AddLogging(logging => logging.AddSerilog(logger));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
