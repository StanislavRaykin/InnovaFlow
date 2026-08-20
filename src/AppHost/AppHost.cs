var builder = DistributedApplication.CreateBuilder(args);

// ---------- secrets ----------
// var aiApiKey = builder.AddParameter("ai-api-key", secret: true);

// ---------- infrastructure ----------
// var postgres = builder.AddPostgres("postgres")
//                       .WithDataVolume()          // survives restarts
//                       .WithPgAdmin();            // browse tables during the demo

// var db = postgres.AddDatabase("innovaflow");

// var rabbit = builder.AddRabbitMQ("messaging")
//                     .WithDataVolume()
//                     .WithManagementPlugin();     // queue UI on :15672

// var cache = builder.AddRedis("cache");

// ---------- services ----------
var identity = builder.AddProject<Projects.Identity>("identity");
                     //  .WithReference(db)
                     //  .WaitFor(db);

var projects = builder.AddProject<Projects.Projects>("projects"); // reference are commented for now since there is no db or redis configuration yet
                                                                  //  .WithReference(db)
                                                                  //  .WithReference(rabbit)
                                                                  //  .WaitFor(db)
                                                                  //  .WaitFor(rabbit);

var analysis = builder.AddProject<Projects.Analysis_Api>("analysis");
//  .WithReference(db)
//  .WithReference(rabbit)
//  .WithReference(cache)
//  .WaitFor(db)
//  .WaitFor(rabbit);

builder.AddProject<Projects.Analysis_Worker>("worker");
//        .WithReference(db)
//        .WithReference(rabbit)
//        .WithReference(cache)
//     //    .WithEnvironment("AI__ApiKey", aiApiKey)
//        .WaitFor(rabbit)
//        .WithReplicas(3);                         // three competing consumers

var notifier = builder.AddProject<Projects.Notifier>("notifier");
                     //  .WithReference(rabbit)
                     //  .WithReference(cache)      // SignalR backplane
                     //  .WaitFor(rabbit);

// ---------- edge ----------
var gateway = builder.AddProject<Projects.Gateway>("gateway")
                     .WithReference(identity)
                     .WithReference(projects)
                     .WithReference(analysis)
                     .WithReference(notifier)
                     .WithExternalHttpEndpoints();

builder.AddProject<Projects.Client>("client");
       // .WithReference(gateway);

builder.Build().Run();