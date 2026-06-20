using Orchestrator;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<FixProcessWorker>();

var host = builder.Build();
host.Run();
