using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// ---------- secrets and api keys ----------
var claudeKey = builder.AddParameter("CLAUDE-API-KEY", secret: true);
var ghCliSecret = builder.AddParameter("GH-CLI-SECRET", secret: true);

var ghCliId = builder.AddParameter("GH-CLI-ID", secret: true);
var bzKeyId = builder.AddParameter("BZ-KEY-ID", secret: true);
var bzAppKey = builder.AddParameter("BZ-APP-KEY", secret: true);
var jwtPrivKey = builder.AddParameter("JWT-PRIVATE-KEY", secret: true);
var jwtPubKey = builder.AddParameter("JWT-PUBLIC-KEY");

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


var projects = builder.AddProject<Projects.Projects>("projects")//migrations
                                                                   .WithReference(db)
                                                                   .WithReference(rabbit)
                                                                   .WithEnvironment("RunMigrationsOnStartup", "true")
                                                                   .WithEnvironment("BACKBLAZE-KEY-ID", bzKeyId)
                                                                   .WithEnvironment("BACKBLAZE-APP-KEY", bzAppKey)
                                                                   .WaitFor(db)
                                                                   .WaitFor(rabbit);

var analysis = builder.AddProject<Projects.Analysis_Api>("analysis") //migrations
 .WithReference(db)
 .WithReference(rabbit)
 .WithReference(cache)
 .WithEnvironment("RunMigrationsOnStartup", "true")
 .WithEnvironment("BACKBLAZE-KEY", bzKeyId)
 .WithEnvironment("BACKBLAZE-APP", bzAppKey)
 .WaitFor(db)
 .WaitFor(rabbit);

builder.AddProject<Projects.Analysis_Worker>("worker")
       .WithReference(db)
       .WithReference(rabbit)
       .WithReference(cache)
       .WithEnvironment("CLAUDE-KEY", claudeKey)
       .WithEnvironment("BACKBLAZE-KEY-ID", bzKeyId)
 .WithEnvironment("BACKBLAZE-APP-KEY", bzAppKey)
       .WaitFor(rabbit)
       .WithReplicas(2);                         // three competing consumers

var notifier = builder.AddProject<Projects.Notifier>("notifier") //migrations
                      .WithReference(rabbit)
                      .WithReference(cache)
                      .WithReference(db)
                      .WithEnvironment("RunMigrationsOnStartup", "true")      
                      .WaitFor(rabbit);



var identity = builder.AddProject<Projects.Identity>("identity")//migrations
                      .WithEnvironment("RunMigrationsOnStartup", "true")
                      .WithEnvironment("GH-CLI-SECRET", ghCliSecret)
                      .WithEnvironment("GH-CLI-ID", ghCliId)
                      .WithEnvironment("Jwt__PrivateKey", jwtPrivKey)
                      .WithReference(db)
                      .WithReference(cache)
                      .WaitFor(db)
                      .WaitFor(cache);


// ---------- edge ----------
var gateway = builder.AddProject<Projects.Gateway>("gateway")
                     .WithReference(identity)
                     .WithReference(projects)
                     .WithReference(analysis)
                     .WithReference(notifier)
                     .WithReference(cache)
                     .WaitFor(cache)
                     .WithExternalHttpEndpoints();

var client = builder.AddProject<Projects.Client>("client")
       .WithReference(gateway);

identity.WithEnvironment("Client__BaseUrl", client.GetEndpoint("https"));
client.WithReference(identity).WaitFor(identity);


foreach(var s in new[] { notifier, projects, analysis, identity, gateway })
{
       s.WithEnvironment("Jwt__PublicKey", jwtPubKey);
}



builder.Build().Run();