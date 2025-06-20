using Doculyzer.Core.Configuration;
using Doculyzer.Core.Infrastructure.Factories;
using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Mediator;
using Doculyzer.Core.Services;
using Doculyzer.Request;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IMediator, Mediator>();

// Register Azure service factories
builder.Services.AddSingleton<IAzureServiceFactory, AzureServiceFactory>();

// Register CosmosClient using IAzureServiceFactory
builder.Services.AddSingleton(sp =>
{
    var serviceFactory = sp.GetRequiredService<IAzureServiceFactory>();
    return serviceFactory.CreateCosmosClient();
});

// Register repositories
builder.Services.AddSingleton<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddSingleton<IInvoiceSearchRepository, InvoiceSearchRepository>();
builder.Services.AddSingleton<IEvaluationMetricsRepository>(sp =>
{
    var config = sp.GetRequiredService<IOptions<ServicesConfig>>().Value;
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    return new EvaluationMetricsRepository(cosmosClient, config.CosmosDBDatabaseName, config.CosmosDBContainerName);
});

// Register services
builder.Services.AddSingleton<IInvoiceAnalysisService, InvoiceAnalysisService>();
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();

// Register handlers
builder.Services.AddTransient<IRequestHandler<DocumentQueryRequest, DocumentQueryResult>, DocumentQueryHandler>();
builder.Services.AddTransient<IRequestHandler<ProcessInvoiceMetadataRequest, ProcessInvoiceMetadataResult>, ProcessInvoiceMetadataHandler>();

builder.Services.AddOptions<ServicesConfig>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection("ServicesConfig").Bind(settings);
    });

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
