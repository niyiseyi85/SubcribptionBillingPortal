var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.VeltrixBookingApp_API>("veltrixbookingapp-api");

builder.Build().Run();
