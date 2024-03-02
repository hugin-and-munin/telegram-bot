using Bot;
using static ReportProvider.ReportProvider;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var config = builder.Configuration;
var appOptions = config.GetSection(AppOptions.Name).Get<AppOptions>() ?? throw new InvalidOperationException();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks().AddCheck<HealthCheck>("Health");
builder.Services.AddHostedService<Worker>();
builder.Services.Configure<AppOptions>(config.GetSection(AppOptions.Name));
builder.Services.AddGrpcClient<ReportProviderClient>(o => o.Address = new Uri(appOptions.ReportProviderUri));

var app = builder.Build();

app.MapHealthChecks("/health");

await app.RunAsync();
