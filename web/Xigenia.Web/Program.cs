using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Xigenia.Web;
using Xigenia.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<AuthState>();
builder.Services.AddSingleton<CategoryMutationState>();
builder.Services.AddSingleton<AccountTransferMutationState>();
builder.Services.AddSingleton<ObligationMutationState>();
builder.Services.AddSingleton<ProfileMutationState>();

builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var auth = sp.GetRequiredService<AuthState>();
    var apiBase = config["ApiBaseUrl"];
    if (string.IsNullOrWhiteSpace(apiBase))
    {
        apiBase = "http://localhost:5122";
    }

    var http = new HttpClient { BaseAddress = new Uri(apiBase) };
    return new ApiClient(http, auth);
});

await builder.Build().RunAsync();
