
using DocAssistant.Api;
using Microsoft.Extensions.AI;
using Npgsql;
using OllamaSharp;
using Pgvector;
using System.Runtime.Intrinsics.Arm;

var builder = WebApplication.CreateBuilder(args);

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("Db"));
dataSourceBuilder.UseVector();
builder.Services.AddSingleton(dataSourceBuilder.Build());
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new OllamaApiClient(new Uri("http://localhost:11434"), "nomic-embed-text"));
builder.Services.AddSingleton<IChatClient>(
    new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2"));
builder.Services.AddSingleton<DocumentIngestService>();
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.Use(async (ctx, next) =>
{
    ctx.Response.OnStarting(() =>
    {
        var ct = ctx.Response.ContentType;
        if (ct is not null && ct.StartsWith("text/event-stream") && !ct.Contains("charset"))
            ctx.Response.ContentType = "text/event-stream; charset=utf-8";
        return Task.CompletedTask;
    });
    await next();
});
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

//app.MapGet("/db-test", async (NpgsqlDataSource db) =>
//{
//    var fake = Enumerable.Range(0, 1536).Select(i => (float)(i % 10) / 10).ToArray();

//    await using var insert = db.CreateCommand("Insert into chunks (content,embedding) values($1,$2)");
//    insert.Parameters.AddWithValue("Deneme metni");
//    insert.Parameters.AddWithValue(new Vector(fake));
//    await insert.ExecuteNonQueryAsync();

//    await using var select = db.CreateCommand("select id,content from chunks order by embedding <=> $1 limit 50");
//    select.Parameters.AddWithValue(new Vector(fake));

//    var results = new List<object>();
//    await using var reader = await select.ExecuteReaderAsync();
//    while (await reader.ReadAsync())
//        results.Add(new { Id = reader.GetInt32(0), Content = reader.GetString(1) });

//    return results;

//});

app.MapGet("/embed-test", async (IEmbeddingGenerator<string, Embedding<float>> gen) =>
{
    var vec = await gen.GenerateVectorAsync("merhaba dünya!");
    return vec.Length;
});


app.MapPost("/documents", async (DocumentRequest req, DocumentIngestService svc,CancellationToken ct) =>
    Results.Ok(new { chunkCount = await svc.IngestAsync(req.text, ct) }));

app.MapGet("/search", async (string q, IEmbeddingGenerator<string, Embedding<float>> embedder, NpgsqlDataSource db) =>
{
    var queryVector = await embedder.GenerateVectorAsync(q);

    await using var cmd = db.CreateCommand("select content,embedding <=> $1 as distance from chunks order by embedding <=> $1 limit 3");
    cmd.Parameters.AddWithValue(new Vector(queryVector));

    var results = new List<object>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        results.Add(new { Content = reader.GetString(0), Distance = reader.GetDouble(1) });
    }
    return results;
});


app.MapGet("/ask", (string q, IChatClient chat, IEmbeddingGenerator<string, Embedding<float>> embedder, NpgsqlDataSource db, CancellationToken ct) => 
    TypedResults.ServerSentEvents(Rag.AskStream(q, chat, embedder, db, ct)));


app.Run();
record DocumentRequest(string text);

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
