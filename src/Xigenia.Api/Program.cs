using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xigenia.Api.Data;
using Xigenia.Api.Endpoints;
using Xigenia.Application.Finance;
using Xigenia.Infrastructure.MetaAi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("XigeniaWeb", policy =>
    {
        // Deployment topologies (e.g. Docker Compose) may add origins via
        // Cors:AllowedOrigins without touching code. Defaults cover local dev.
        var extraOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();
        policy.WithOrigins(
                new[]
                {
                    "http://localhost:5096",
                    "https://localhost:5096",
                    "http://localhost:5097",
                    "https://localhost:5097"
                }.Concat(extraOrigins).ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
        // Bearer token does not require AllowCredentials; keep origin list explicit for local development.
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (origin.StartsWith("http://localhost:", StringComparison.Ordinal) ||
                    origin.StartsWith("https://localhost:", StringComparison.Ordinal))
                {
                    return true;
                }
                return false;
            });
        }
    });
});

builder.Services.AddScoped<IFinanceStore, EfFinanceStore>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<GlobalCategoryCatalogInitializer>();

builder.Services.Configure<AiProviderOptions>(o => AiProviderOptionsConfigurator.Configure(o, builder.Configuration));
builder.Services.Configure<MetaStandardOptions>(o => MetaStandardOptionsConfigurator.Configure(o, builder.Configuration));
builder.Services.Configure<MetaAiOptions>(o => MetaAiOptionsConfigurator.Configure(o, builder.Configuration));
builder.Services.AddHttpClient(AiClientFactory.MetaHttpClientName, (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<MetaStandardOptions>>().Value;
    var raw = string.IsNullOrWhiteSpace(opts.BaseUrl) ? "https://api.meta.ai/v1/" : opts.BaseUrl.Trim();
    if (!raw.EndsWith('/'))
    {
        raw += "/";
    }
    client.BaseAddress = new Uri(raw, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30);
});
builder.Services.AddHttpClient(AiClientFactory.BasetenHttpClientName, (sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<MetaAiOptions>>().Value;
    var raw = string.IsNullOrWhiteSpace(opts.BaseUrl) ? "https://inference.baseten.co/v1/" : opts.BaseUrl.Trim();
    if (!raw.EndsWith('/'))
    {
        raw += "/";
    }
    client.BaseAddress = new Uri(raw, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds > 0 ? opts.TimeoutSeconds : 30);
});
builder.Services.AddTransient<IAiClientFactory, AiClientFactory>();
builder.Services.AddTransient<IMetaAiClient>(sp => sp.GetRequiredService<IAiClientFactory>().CreateClient());

var app = builder.Build();
app.UseCors("XigeniaWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth")
   .MapIdentityApi<ApplicationUser>();

app.MapFinanceEndpoints();
app.MapAiEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "xigenia-api" }))
.WithName("Health");

app.MapGet("/", () => Results.Ok(new { service = "Xigenia API", version = "0.1.0" }));

using (var scope = app.Services.CreateScope())
{
    var catalog = scope.ServiceProvider.GetRequiredService<GlobalCategoryCatalogInitializer>();
    await catalog.InitializeAsync(app.Lifetime.ApplicationStopping);
}

app.Run();
