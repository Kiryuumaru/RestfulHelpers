var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.RestfulHelpers_Test_ApiService>("apiservice");

builder.Build().Run();
