using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xigenia.Api.Data;
using Xigenia.Api.Endpoints;
using Xigenia.Application.Finance;

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

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth")
   .MapIdentityApi<ApplicationUser>();

app.MapFinanceEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "xigenia-api" }))
.WithName("Health");

app.MapGet("/", () => Results.Ok(new { service = "Xigenia API", version = "0.1.0" }));

app.Run();
