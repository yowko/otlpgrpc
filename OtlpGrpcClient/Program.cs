using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtlpGrpcServer;

var builder = WebApplication.CreateBuilder(args);
ActivitySource sSource = new ActivitySource(builder.Environment.ApplicationName);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddGrpcClient<Greeter.GreeterClient>(o => o.Address = new Uri("https://localhost:7150"));


var tracingOtlpEndpoint = builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!;
var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: builder.Environment.ApplicationName));
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    //tracing.AddHttpClientInstrumentation();
    tracing.AddGrpcClientInstrumentation();
    tracing.AddSource(sSource.Name );

    if (tracingOtlpEndpoint != null)
    {
        tracing.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint); 
        });
    }
    // else
    // {
    tracing.AddConsoleExporter();
    //}
});

var app = builder.Build();

// // Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/getweatherforecastgrpc", async () =>
    {
        using Activity activity = sSource.StartActivity("call GetWeatherAsync via grpc");
        return await GetWeatherGrpcAsync();
    })
    .WithName("GetWeatherForecast");

app.Run();

async Task<HelloReply?> GetWeatherGrpcAsync()
{
    using Activity activity = sSource.StartActivity("exec GetWeatherAsync via grpc");
    activity?.SetTag("yowko", "tag grpc OK");

    var grpcClient= app.Services.GetService<Greeter.GreeterClient>();
    return await grpcClient.SayHelloAsync(new HelloRequest
    {
        Name = ".NET 8"
    });
}