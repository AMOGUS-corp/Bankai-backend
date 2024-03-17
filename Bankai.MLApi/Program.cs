using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Extensions.Logging;
using Asp.Versioning;
using Bankai.MLApi.Data;
using Bankai.MLApi.Services.Background.FeatureImportance;
using Bankai.MLApi.Services.Background.FeatureOptimizing;
using Bankai.MLApi.Services.Background.Training;
using Bankai.MLApi.Services.DatasetManagement;
using Bankai.MLApi.Services.FeatureImportance;
using Bankai.MLApi.Services.ModelManagement;
using Bankai.MLApi.Services.Optimizing;
using Bankai.MLApi.Services.Prediction;
using Bankai.MLApi.Services.Training;
using Bankai.MLApi.Utils;
using Bankai.MLApi.Validators;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using static System.AppContext;
using static System.TimeSpan;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

LogManager.Setup()
    .SetupExtensions(s => s.RegisterConfigSettings(config))
    .LoadConfigurationFromSection(config);

builder.WebHost.ConfigureKestrel(c => c.Limits.KeepAliveTimeout = FromHours(12));

builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddNewtonsoftJson(o =>
    {
        o.SerializerSettings.Converters.Add(new StringEnumConverter
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        });
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenNewtonsoftSupport();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Comind Space Machine Learning Api",
        Description = "Api for working with ai models"
    });
    
    options.IncludeXmlComments(Combine(BaseDirectory, $"{GetExecutingAssembly().GetName().Name}.xml"));
    options.SchemaFilter<EnumDictionaryToStringDictionarySchemaFilter>();
});

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(setup =>
    {
        setup.GroupNameFormat = "'v'VVV";
        setup.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddDbContext<MLApiDbContext>(opts =>
    opts.UseNpgsql(config.GetConnectionString("Postgres")));

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddLogging(loggingBuilder =>
{
    // configure Logging with NLog
    loggingBuilder.ClearProviders();
    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
    loggingBuilder.AddNLog(config);
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services
    .AddSingleton<MLContext>(_ => new())
    .AddScoped<IDatasetManagementService, DatasetManagementService>()
    .AddScoped<IFeatureImportanceService, FeatureImportanceService>()
    .AddScoped<IModelManagementService, ModelManagementService>()
    .AddScoped<IPredictionService, PredictionService>()
    .AddScoped<ITrainingService, TrainingService>()
    .AddScoped<IFeatureOptimizingService, FeatureOptimizingService>()
    .AddSingleton<ITrainingBackgroundService, TrainingBackgroundWorker>()
    .AddSingleton<IFeatureImportanceBackgroundService, FeatureImportanceBackgroundWorker>()
    .AddSingleton<IFeatureOptimizingBackgroundService, FeatureOptimizingBackgroundWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1"));
app.UseExceptionHandler("/error");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<MLApiDbContext>();
    if (context.Database.GetPendingMigrations().Any())
        context.Database.Migrate();
}

app.Run();
