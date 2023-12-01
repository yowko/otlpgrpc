using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtlpGrpcServer.Services;


var builder = WebApplication.CreateBuilder(args);


ActivitySource sSource = new ActivitySource(builder.Environment.ApplicationName);

// Add services to the container.
builder.Services.AddGrpc();

var tracingOtlpEndpoint = builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!;

var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: builder.Environment.ApplicationName));

// Add Tracing for ASP.NET Core and our custom ActivitySource and export to Tempo
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddSource(sSource.Name);

    if (tracingOtlpEndpoint != null)
    {
        tracing.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint); 
        });
    }
    else
    {
        tracing.AddConsoleExporter();
    }
});

var app = builder.Build();


// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

//Task.Run(() => app.Run());
app.Run();