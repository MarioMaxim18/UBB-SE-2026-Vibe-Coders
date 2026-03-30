using Microsoft.Data.SqlClient;
using VibeCoders.Models;

namespace VibeCoders.Services;

public partial class SqlDataStorage
{
    /// <inheritdoc />
    public List<EarnedAchievement> GetEarnedAchievements(int clientId)
    {
        var list = new List<EarnedAchievement>();

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT a.achievement_id, a.title, a.description
            FROM ACHIEVEMENT a
            INNER JOIN CLIENT_ACHIEVEMENT ca
                ON ca.achievement_id = a.achievement_id
            WHERE ca.client_id = @ClientId
              AND ca.unlocked = 1
            ORDER BY a.achievement_id;";

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ClientId", clientId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new EarnedAchievement
            {
                AchievementId = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.GetString(2)
            });
        }

        return list;
    }
}
