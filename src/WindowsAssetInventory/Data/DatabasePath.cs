using Microsoft.Data.Sqlite;

namespace WindowsAssetInventory.Data;

public static class DatabasePath
{
    public static string ResolveConnectionString(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) ||
            builder.DataSource == ":memory:")
        {
            return connectionString;
        }

        var fullPath = Path.IsPathRooted(builder.DataSource)
            ? builder.DataSource
            : Path.GetFullPath(builder.DataSource, contentRootPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        builder.DataSource = fullPath;
        return builder.ConnectionString;
    }
}
