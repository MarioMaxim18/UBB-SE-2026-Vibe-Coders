using Microsoft.Data.SqlClient;
using VibeCoders.Models;

namespace VibeCoders.Services;

public partial class SqlDataStorage
{
    /// <inheritdoc />
    public List<AchievementShowcaseItem> GetAchievementShowcaseForClient(int clientId)
    {
        var list = new List<AchievementShowcaseItem>();

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        // Full catalog for the client; NULL join means not unlocked yet.
        const string sql = @"
            SELECT a.achievement_id, a.title, a.description,
                   CASE WHEN ISNULL(ca.unlocked, 0) = 1 THEN 1 ELSE 0 END
            FROM ACHIEVEMENT a
            LEFT JOIN CLIENT_ACHIEVEMENT ca
                ON ca.achievement_id = a.achievement_id AND ca.client_id = @ClientId
            ORDER BY ISNULL(ca.unlocked, 0) DESC, a.achievement_id";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AchievementShowcaseItem
            {
                AchievementId = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2),
                IsUnlocked = reader.GetInt32(3) != 0
            });
        }

        return list;
    }
}
