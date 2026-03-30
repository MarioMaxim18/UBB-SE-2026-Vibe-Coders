using Microsoft.Data.SqlClient;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage 
    {
        /// <summary>
        /// Persists a Notification row (e.g. plateau / deload alert).
        /// </summary>
        public bool SaveNotification(Notification notification)
        {
            const string sql = @"
                INSERT INTO NOTIFICATION 
                    (title, message, type, related_id, date_created, is_read, client_id)
                VALUES 
                    (@Title, @Message, @Type, @RelatedId, @DateCreated, @IsRead, @ClientId);";

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Title", notification.Title);
            cmd.Parameters.AddWithValue("@Message", notification.Message);
            cmd.Parameters.AddWithValue("@Type", notification.Type.ToString());
            cmd.Parameters.AddWithValue("@RelatedId", notification.RelatedId);
            cmd.Parameters.AddWithValue("@DateCreated", notification.DateCreated);
            cmd.Parameters.AddWithValue("@IsRead", notification.IsRead);
            cmd.Parameters.AddWithValue("@ClientId", notification.ClientId);

            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected > 0;
        }

        /// <summary>
        /// Returns all notifications for the given client, ordered by date descending.
        /// </summary>
        public List<Notification> GetNotifications(int clientId)
        {
            const string sql = @"
                SELECT 
                    id,
                    title,
                    message,
                    type,
                    related_id,
                    date_created,
                    is_read
                FROM NOTIFICATION
                WHERE client_id = @ClientId
                ORDER BY date_created DESC;";

            var notifications = new List<Notification>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ClientId", clientId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                notifications.Add(new Notification
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Message = reader.GetString(2),
                    Type = Enum.Parse<NotificationType>(reader.GetString(3)),
                    RelatedId = reader.GetInt32(4),
                    DateCreated = reader.GetDateTime(5),
                    IsRead = reader.GetBoolean(6),
                    ClientId = clientId
                });
            }

            return notifications;
        }
    }
}