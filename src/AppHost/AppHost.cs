using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// ---------- secrets and api keys ----------
var claudeKey = builder.AddParameter("CLAUDE-API-KEY", secret: true);
var bzKeyId = builder.AddParameter("BZ-KEY-ID", secret: true);
var bzAppKey = builder.AddParameter("BZ-APP-KEY", secret: true);
var ghCliSecret =  builder.AddParameter("GH-CLI-SECRET", secret: true);

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
                      .WithEnvironment("GH-CLI-SECRET", ghCliSecret)
                      .WithReference(db)
                      .WaitFor(db);

var projects = builder.AddProject<Projects.Projects>("projects")//migrations
                                                                   .WithReference(db)
                                                                   .WithReference(rabbit)
                                                                   .WithEnvironment("RunMigrationsOnStartup", "true")
                                                                   .WithEnvironment("BZ-KEY-ID", bzKeyId)
 .WithEnvironment("BZ-APP-KEY", bzAppKey)
                                                                   .WaitFor(db)
                                                                   .WaitFor(rabbit);

var analysis = builder.AddProject<Projects.Analysis_Api>("analysis") //migrations
 .WithReference(db)
 .WithReference(rabbit)
 .WithReference(cache)
 .WithEnvironment("RunMigrationsOnStartup", "true")
 .WithEnvironment("BZ-KEY", bzKeyId)
 .WithEnvironment("BZ-APP", bzAppKey)
 .WaitFor(db)
 .WaitFor(rabbit);

builder.AddProject<Projects.Analysis_Worker>("worker")
       .WithReference(db)
       .WithReference(rabbit)
       .WithReference(cache)
       .WithEnvironment("CLAUDE-KEY", claudeKey)
       .WithEnvironment("BZ-KEY-ID", bzKeyId)
 .WithEnvironment("BZ-APP-KEY", bzAppKey)
       .WaitFor(rabbit)
       .WithReplicas(3);                         // three competing consumers

var notifier = builder.AddProject<Projects.Notifier>("notifier") //migrations
                      .WithReference(rabbit)
                      .WithReference(cache)
                      .WithReference(db)
                      .WithEnvironment("RunMigrationsOnStartup", "true")      
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