using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// ---------- secrets and api keys ----------
var claudeKey = builder.AddParameter("CLAUDE-API-KEY", secret: true);

//---------- infrastructure ----------

var db = builder.ExecutionContext.IsPublishMode
    ? builder.AddConnectionString(builder.Configuration.GetConnectionString("InnovaFlow")!)
    : builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithHostPort(5432)
    .WithPgWeb()
    .AddDatabase("InnovaFlow");


var rabbit = builder.AddRabbitMQ("messaging")
                    .WithDataVolume()
                    .WithManagementPlugin();     // queue UI on :15672

var cache = builder.AddRedis("cache");

// ---------- services ----------
var identity = builder.AddProject<Projects.Identity>("identity")//migrations
                      .WithEnvironment("RunMigrationsOnStartup", "true")
                      .WithReference(db)
                      .WaitFor(db);

var projects = builder.AddProject<Projects.Projects>("projects")//migrations
                                                                   .WithReference(db)
                                                                   .WithReference(rabbit)
                                                                   .WithEnvironment("RunMigrationsOnStartup", "true")
                                                                   .WaitFor(db)
                                                                   .WaitFor(rabbit);

var analysis = builder.AddProject<Projects.Analysis_Api>("analysis") //migrations
 .WithReference(db)
 .WithReference(rabbit)
 .WithReference(cache)
 .WithEnvironment("RunMigrationsOnStartup", "true")
 .WaitFor(db)
 .WaitFor(rabbit);

builder.AddProject<Projects.Analysis_Worker>("worker")
       .WithReference(db)
       .WithReference(rabbit)
       .WithReference(cache)
       .WithEnvironment("CLAUDE-KEY", claudeKey)
       .WaitFor(rabbit)
       .WithReplicas(3);                         // three competing consumers

var notifier = builder.AddProject<Projects.Notifier>("notifier") //migrations
                      .WithReference(rabbit)
                      .WithReference(cache)
                      .WithEnvironment("RunMigrationsOnStartup", "true")      // SignalR backplane
                      .WaitFor(rabbit);

// ---------- edge ----------
var gateway = builder.AddProject<Projects.Gateway>("gateway")
                     .WithReference(identity)
                     .WithReference(projects)
                     .WithReference(analysis)
                     .WithReference(notifier)
                     .WithExternalHttpEndpoints();

builder.AddProject<Projects.Client>("client")
       .WithReference(gateway);

builder.Build().Run();