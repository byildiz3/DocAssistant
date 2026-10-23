using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;
using System.Runtime.CompilerServices;

namespace DocAssistant.Api
{
    public class Rag
    {
        public static async IAsyncEnumerable<string> AskStream(string question,IChatClient chat,IEmbeddingGenerator<string,Embedding<float>> embedder,NpgsqlDataSource db, [EnumeratorCancellation] CancellationToken ct)
        {
            var queryVector = await embedder.GenerateVectorAsync(question, cancellationToken: ct);
             
            var results=new List<(string content,double distance)>();
            await using (var cmd = db.CreateCommand("select content,embedding <=> $1 as distance from chunks order by embedding <=> $1 limit 3"))
            {
                cmd.Parameters.AddWithValue(new Vector(queryVector));
                await using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                    results.Add((reader.GetString(0),reader.GetDouble(1)));
            }
            const double maxDistance = 0.5;
            var chunks = results.Where(r => r.distance <= maxDistance).Select(r => r.content).ToList();
            if (chunks.Count == 0)
            {
                yield return "Bilmiyorum: bu soruyla ilgili bilgi dökümanlarda bulunamadı.";
                yield break;
            }
            var context = string.Join("\n---\n", chunks);
            var prompt = $"""
                 Sadece aşağıdaki bağlama dayanarak türkçe cevap ver. Cevap bağlamda yoksa "Bilmiyorum" de.
                
                 Bağlam:
                 {context}

                 Soru: {question} 
                 """;
            var options = new ChatOptions { Temperature = 0f };
            await foreach (var update in chat.GetStreamingResponseAsync(prompt, options,cancellationToken: ct))
                yield return update.Text;

        }
    }
}
