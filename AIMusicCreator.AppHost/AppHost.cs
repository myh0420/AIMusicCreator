var builder = DistributedApplication.CreateBuilder(args);

//var modelsStorage = builder.AddProject<Projects.AIMusicCreator_Entity>("modelsstorage");
var apiService = builder.AddProject<Projects.AIMusicCreator_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AIMusicCreator_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
