using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddScoped<IFinanceStore, EfFinanceStore>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<GlobalCategoryCatalogInitializer>();

builder.Services.Configure<MetaAiOptions>(o => MetaAiOptionsConfigurator.Configure(o, builder.Configuration));
builder.Services.AddHttpClient<IMetaAiClient, MetaAiClient>((sp, client) =>
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

var app = builder.Build();
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
