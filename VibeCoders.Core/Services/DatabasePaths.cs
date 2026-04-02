namespace VibeCoders.Services;

/// <summary>
/// Resolves database connection settings used by the application.
/// </summary>
public static class DatabasePaths
{
    /// <summary>
    /// SQLite connection string. The database file is created next to the executable.
    /// </summary>
    public static string GetConnectionString() =>
        "Data Source=VibeCoders.db";
}
