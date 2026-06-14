var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password");

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume()
    .WithPgAdmin(pgAdmin => pgAdmin.WithUrlForEndpoint("http", url => url.DisplayText = "pgAdmin"))
    .WithEndpoint(port: 5432, targetPort: 5432);

var applicationDatabase = postgres.AddDatabase("app");

var redis = builder.AddRedis("Cache")
    .WithRedisCommander(redisCommander =>
        redisCommander.WithUrlForEndpoint("http", url => url.DisplayText = "Redis Commander"))
    .WithEndpoint(port: 6379, targetPort: 6379);

#pragma warning disable ASPIRECERTIFICATES001 // WithHttpsDeveloperCertificate
var api = builder.AddProject<Projects.Solution_Api>("api", launchProfileName: "https")
    .WithReference(applicationDatabase)
    .WithReference(redis)
    .WaitFor(postgres)
    .WaitFor(redis)
    .WithHttpsDeveloperCertificate()
    .WithExternalHttpEndpoints()
    .WithUrl("/scalar", "Scalar")
    .WithUrls(context =>
    {
        for (var index = context.Urls.Count - 1; index >= 0; index--)
        {
            if (string.Equals(context.Urls[index].Endpoint?.EndpointName, "http", StringComparison.OrdinalIgnoreCase))
            {
                context.Urls.RemoveAt(index);
            }
        }
    });
#pragma warning restore ASPIRECERTIFICATES001

#pragma warning disable ASPIRECERTIFICATES001 // WithHttpsDeveloperCertificate
var web = builder.AddViteApp("web", "../../web")
    .WithYarn(installArgs: ["--frozen-lockfile"])
    .WithReference(api)
    .WaitFor(api)
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5000;
        endpoint.TargetPort = 5000;
        endpoint.UriScheme = "https";
        endpoint.IsProxied = false;
    })
    .WithHttpsDeveloperCertificate()
    .WithExternalHttpEndpoints()
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("https"))
    .WithUrls(context =>
    {
        foreach (var url in context.Urls)
        {
            url.DisplayText = "Frontend";
        }
    });
#pragma warning restore ASPIRECERTIFICATES001

api.WithEnvironment("Cors__AllowedOrigins__0", "https://localhost:5000");

builder.Build().Run();
