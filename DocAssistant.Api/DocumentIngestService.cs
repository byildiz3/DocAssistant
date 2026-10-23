using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;

namespace DocAssistant.Api
{
    public class DocumentIngestService(IEmbeddingGenerator<string,Embedding<float>> embedder,NpgsqlDataSource db)
    {
        public async Task<int> IngestAsync(string text, CancellationToken ct = default)
        {
            var chunks = Chunker.Chunk(text, 300, 50).ToList();
            if (chunks.Count == 0) return 0;

            var embeddings= await embedder.GenerateAsync(chunks,cancellationToken:ct);

            for (int i = 0; i < chunks.Count; i++)
            {
                await using var cmd = db.CreateCommand("Insert into chunks (content,embedding) values ($1,$2)");
                cmd.Parameters.AddWithValue(chunks[i]);
                cmd.Parameters.AddWithValue(new Vector(embeddings[i].Vector));
                await cmd.ExecuteNonQueryAsync();
            }

            return chunks.Count;
        }
    }
}
