
using Npgsql;
using Pgvector;

var builder = WebApplication.CreateBuilder(args);

var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("Db"));
dataSourceBuilder.UseVector();
builder.Services.AddSingleton(dataSourceBuilder.Build());

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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



app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
