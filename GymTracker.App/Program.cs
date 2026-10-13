using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using GymTracker.App;
using GymTracker.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();

var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url is not configured.");
var supabaseAnonKey = builder.Configuration["Supabase:AnonKey"]
    ?? throw new InvalidOperationException("Supabase:AnonKey is not configured.");

builder.Services.AddHttpClient("Supabase.Auth", client =>
{
    client.BaseAddress = new Uri($"{supabaseUrl}/auth/v1/");
    client.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
});

builder.Services.AddHttpClient("Supabase.Rest", client =>
{
    client.BaseAddress = new Uri($"{supabaseUrl}/rest/v1/");
    client.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
    client.Timeout = TimeSpan.FromSeconds(8);
});

// Registered as scoped (rather than AddHttpClient<TClient,TImpl>, which is transient) so every
// component shares the same AuthService instance and its in-memory session - otherwise each
// injection site would get its own never-logged-in copy.
builder.Services.AddScoped<IAuthService>(sp => new AuthService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Supabase.Auth"),
    sp.GetRequiredService<ILocalStorageService>()));

builder.Services.AddScoped<ICloudSyncService>(sp => new CloudSyncService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Supabase.Rest"),
    sp.GetRequiredService<IAuthService>()));

builder.Services.AddScoped<IWorkoutStorageService, WorkoutStorageService>();

await builder.Build().RunAsync();
