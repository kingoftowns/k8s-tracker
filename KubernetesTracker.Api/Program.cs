using System.Text.Json.Serialization;
using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSubdomains",
        builder =>
        {
            builder
                .WithOrigins(
                    "https://clusters.k8s.blacktoaster.com",
                    "http://localhost:3000"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.EnableRetryOnFailure())
    .EnableDetailedErrors());

#if !DEBUG
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(80);
});
#endif

builder.Services.AddScoped<IClusterService, ClusterService>();
builder.Services.AddScoped<IIngressService, IngressService>();
builder.Services.AddScoped<IKubernetesService, KubernetesService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowSubdomains");

app.MapControllers();
app.Run();