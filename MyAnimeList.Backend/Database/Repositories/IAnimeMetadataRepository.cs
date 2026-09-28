using Dapper;
using Npgsql;
using YourProjectNamespace.Entities;

namespace YourProjectNamespace.Repositories;

public interface IAnimeMetadataRepository
{
    Task<AnimeMetadata?> GetByMalIdAsync(int malId);
    Task UpsertAsync(AnimeMetadata metadata);
}
public class AnimeMetadataRepository : IAnimeMetadataRepository
{
    private readonly string _connectionString;

    public AnimeMetadataRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<AnimeMetadata?> GetByMalIdAsync(int malId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        const string sql = @"
            SELECT 
                id AS Id,
                malid AS MalId,
                demographic AS Demographic,
                themes AS Themes,
                genres AS Genres,
                lastupdatedutc AS LastUpdatedUtc
            FROM animemetadata
            WHERE malid = @malId";

        return await connection.QuerySingleOrDefaultAsync<AnimeMetadata>(sql, new { malId });
    }

    public async Task UpsertAsync(AnimeMetadata metadata)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        metadata.LastUpdatedUtc = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO animemetadata (malid, demographic, themes, genres, lastupdatedutc)
            VALUES (@MalId, @Demographic, @Themes, @Genres, @LastUpdatedUtc)
            ON CONFLICT (malid) DO UPDATE 
            SET demographic = EXCLUDED.demographic,
                themes = EXCLUDED.themes,
                genres = EXCLUDED.genres,
                lastupdatedutc = EXCLUDED.lastupdatedutc;";

        await connection.ExecuteAsync(sql, metadata);
    }
}