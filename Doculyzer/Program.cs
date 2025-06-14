using Doculyzer.Core.Configuration;
using Doculyzer.Core.Infrastructure.Factories;
using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Mediator;
using Doculyzer.Core.Services;
using Doculyzer.Request;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IMediator, Mediator>();

// Register Azure service factories
builder.Services.AddSingleton<IAzureServiceFactory, AzureServiceFactory>();

// Register repositories
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IInvoiceSearchRepository, InvoiceSearchRepository>();

// Register services
builder.Services.AddScoped<IInvoiceAnalysisService, InvoiceAnalysisService>();
builder.Services.AddScoped<IAzureOpenAIService, AzureOpenAIService>();

// Register handlers
builder.Services.AddTransient<IRequestHandler<DocumentQueryRequest, DocumentQueryResult>, DocumentQueryHandler>();
builder.Services.AddTransient<IRequestHandler<ProcessInvoiceMetadataRequest, ProcessInvoiceMetadataResult>, ProcessInvoiceMetadataHandler>();

builder.Services.AddOptions<ServicesConfig>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection("ServicesConfig").Bind(settings);
    });

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
{
    // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
    // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/azure/azure-monitor/app/worker-service#ilogger-logs
    LoggerFilterRule defaultRule = options.Rules.FirstOrDefault(rule => rule.ProviderName
        == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
    if (defaultRule is not null)
    {
        options.Rules.Remove(defaultRule);
    }
});

builder.Build().Run();
