using System.Text.Json;
using System.Text.Json.Nodes;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

// === STEP 1: Merge Ocelot JSONs into one config ===

string sourceDirectory = "Services";
string outputFile = "ocelot.json";

var mergedRoutes = new JsonArray();
JsonObject? globalConfig = null;

var files = Directory.GetFiles(sourceDirectory, "ocelot.*.json", SearchOption.AllDirectories)
    .Concat(new[] { Path.Combine(sourceDirectory, "ocelot.global.json") })
    .Where(File.Exists);

Console.WriteLine("🔄 Merging Ocelot config files:");

foreach (var file in files)
{
    try
    {
        var json = JsonNode.Parse(File.ReadAllText(file));

        if (json?["Routes"] is JsonArray routes)
        {
            foreach (var route in routes)
            {
                if (route is JsonObject r)
                {
                    var upstream = r["UpstreamPathTemplate"]?.ToString();
                    var downstream = r["DownstreamPathTemplate"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(upstream) && !string.IsNullOrWhiteSpace(downstream))
                    {
                        mergedRoutes.Add(r.DeepClone());
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Skipping invalid route in {file}: missing path template");
                        Console.WriteLine(r.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
            }
        }

        if (json?["GlobalConfiguration"] is JsonObject configu)
        {
            globalConfig = configu;
        }

        Console.WriteLine($"✅ Processed: {file}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error reading file {file}: {ex.Message}");
    }
}

var mergedConfig = new JsonObject
{
    ["Routes"] = mergedRoutes
};

if (globalConfig is not null)
{
    mergedConfig["GlobalConfiguration"] = globalConfig.DeepClone();
}

File.WriteAllText(outputFile, mergedConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"✅ Merged config written to {outputFile}");



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// var config = new ConfigurationBuilder()
//     .AddJsonFile("Services/AuthService/ocelot.auth.json", optional: false)
//     .AddJsonFile("Services/FileService/ocelot.file.json", optional: false)
//     .AddJsonFile("Services/ProjectService/ocelot.project.json", optional: false)
//     .AddJsonFile("Services/ProjectService/ocelot.study.json", optional: false)
//     .AddJsonFile("Services/ProjectService/ocelot.taskitem.json", optional: false)
//     .AddJsonFile("Services/ocelot.global.json", optional: false)
//     .Build();
var config = new ConfigurationBuilder()
    .AddJsonFile("ocelot.json", optional: false)
    .Build();

builder.Services.AddOcelot(config);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors("AllowAll");

await app.UseOcelot();

await app.RunAsync();