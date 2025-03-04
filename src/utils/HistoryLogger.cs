using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.utils
{
    public static class SQLiteHistoryLogger
    {
        private static readonly string CONNECTION_STRING = "Data Source=translation_history.db;";

        static SQLiteHistoryLogger()
        {
            using (var connection = new SqliteConnection(CONNECTION_STRING))
            {
                connection.Open();
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS TranslationHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Timestamp TEXT,
                        SourceText TEXT,
                        TranslatedText TEXT,
                        TargetLanguage TEXT,
                        ApiUsed TEXT
                    );";
                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static async Task LogTranslation(string sourceText, string translatedText, 
            string targetLanguage, string apiUsed, CancellationToken token = default)
        {
            using (var connection = new SqliteConnection(CONNECTION_STRING))
            {
                await connection.OpenAsync(token);
                string insertQuery = @"
                    INSERT INTO TranslationHistory (Timestamp, SourceText, TranslatedText, TargetLanguage, ApiUsed)
                    VALUES (@Timestamp, @SourceText, @TranslatedText, @TargetLanguage, @ApiUsed)";

                using (var command = new SqliteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    command.Parameters.AddWithValue("@SourceText", sourceText);
                    command.Parameters.AddWithValue("@TranslatedText", translatedText);
                    command.Parameters.AddWithValue("@TargetLanguage", targetLanguage);
                    command.Parameters.AddWithValue("@ApiUsed", apiUsed);
                    await command.ExecuteNonQueryAsync(token);
                }
            }
        }

        public static async Task<(List<TranslationHistoryEntry>, int)> LoadHistoryAsync(
            int page, int maxRow, string searchText, CancellationToken token = default)
        {
            var history = new List<TranslationHistoryEntry>();
            int maxPage = 1;

            using (var connection = new SqliteConnection(CONNECTION_STRING))
            {
                await connection.OpenAsync(token);

                // Get max page
                using (var command = new SqliteCommand(@$"SELECT COUNT() AS maxPage
                    FROM TranslationHistory
                    WHERE SourceText LIKE '%{searchText}%' OR TranslatedText LIKE '%{searchText}%'",
                    connection))
                    maxPage = Convert.ToInt32(command.ExecuteScalar()) / maxRow;

                // Get table
                using (var command = new SqliteCommand(@$"
                    SELECT Timestamp, SourceText, TranslatedText, TargetLanguage, ApiUsed
                    FROM TranslationHistory
                    WHERE SourceText LIKE '%{searchText}%' OR TranslatedText LIKE '%{searchText}%'
                    ORDER BY Timestamp DESC
                    LIMIT " + maxRow + " OFFSET " + (page * maxRow - maxRow),
                    connection))
                using (var reader = await command.ExecuteReaderAsync(token))
                {
                    while (await reader.ReadAsync(token))
                    {
                        string unixTime = reader.GetString(reader.GetOrdinal("Timestamp"));
                        DateTime localTime;
                        try
                        {
                            localTime = DateTimeOffset.FromUnixTimeSeconds((long)Convert.ToDouble(unixTime)).LocalDateTime;
                        }
                        catch (FormatException)
                        {
                            // DEPRECATED
                            await MigrateOldTimestampFormat(connection);
                            return await LoadHistoryAsync(page, maxRow, string.Empty);
                        }
                        history.Add(new TranslationHistoryEntry
                        {
                            Timestamp = localTime.ToString("MM/dd HH:mm"),
                            TimestampFull = localTime.ToString("MM/dd/yy, HH:mm:ss"),
                            SourceText = reader.GetString(reader.GetOrdinal("SourceText")),
                            TranslatedText = reader.GetString(reader.GetOrdinal("TranslatedText")),
                            TargetLanguage = reader.GetString(reader.GetOrdinal("TargetLanguage")),
                            ApiUsed = reader.GetString(reader.GetOrdinal("ApiUsed"))
                        });
                    }
                }
            }
            return (history, maxPage);
        }
        public static async Task ClearHistory(CancellationToken token = default)
        {
            using (var connection = new SqliteConnection(CONNECTION_STRING))
            {
                await connection.OpenAsync(token);
                string selectQuery = "DELETE FROM TranslationHistory; DELETE FROM sqlite_sequence WHERE NAME='TranslationHistory'";
                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static async Task<string> LoadLatestSourceText(CancellationToken token = default)
        {
            using (var connection = new SqliteConnection(CONNECTION_STRING))
            {
                await connection.OpenAsync(token);
                string selectQuery = @"
                    SELECT Id, SourceText
                    FROM TranslationHistory
                    ORDER BY Id DESC
                    LIMIT 1";

                using (var command = new SqliteCommand(selectQuery, connection))
                using (var reader = await command.ExecuteReaderAsync(token))
                {
                    if (await reader.ReadAsync(token))
                        return reader.GetString(reader.GetOrdinal("SourceText"));
                    else
                        return string.Empty;
                }
            }
        }

        public static async Task DeleteLatestTranslation(CancellationToken token = default)
        {
            using (var connection = new SqliteConnection(CONNECTION_STRING))
            {
                await connection.OpenAsync(token);
                using (var command = new SqliteCommand(@"
                    DELETE FROM TranslationHistory
                    WHERE Id IN ( SELECT Id FROM TranslationHistory ORDER BY Id DESC LIMIT 1)", 
                    connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static async Task ExportToCSV(string filePath, CancellationToken token = default)
        {
            var history = new List<TranslationHistoryEntry>();

            using (var connection = new SqliteConnection(CONNECTION_STRING))
            {
                await connection.OpenAsync(token);

                string selectQuery = @"
                    SELECT Timestamp, SourceText, TranslatedText, TargetLanguage, ApiUsed
                    FROM TranslationHistory
                    ORDER BY Timestamp DESC";

                using (var command = new SqliteCommand(selectQuery, connection))
                using (var reader = await command.ExecuteReaderAsync(token))
                {
                    while (await reader.ReadAsync(token))
                    {
                        string unixTime = reader.GetString(reader.GetOrdinal("Timestamp"));
                        DateTime localTime = DateTimeOffset.FromUnixTimeSeconds((long)Convert.ToDouble(unixTime)).LocalDateTime;
                        history.Add(new TranslationHistoryEntry
                        {
                            Timestamp = localTime.ToString("MM/dd HH:mm"),
                            TimestampFull = localTime.ToString("MM/dd/yy, HH:mm:ss"),
                            SourceText = reader.GetString(reader.GetOrdinal("SourceText")),
                            TranslatedText = reader.GetString(reader.GetOrdinal("TranslatedText")),
                            TargetLanguage = reader.GetString(reader.GetOrdinal("TargetLanguage")),
                            ApiUsed = reader.GetString(reader.GetOrdinal("ApiUsed"))
                        });
                    }
                }
            }

            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,SourceText,TranslatedText,TargetLanguage,ApiUsed");
            foreach (var entry in history)
                csv.AppendLine($"{entry.Timestamp},{entry.SourceText},{entry.TranslatedText},{entry.TargetLanguage},{entry.ApiUsed}");

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        // DEPRECATED
        private static async Task MigrateOldTimestampFormat(SqliteConnection connection)
        {
            var records = new List<(long id, string timestamp)>();
            using (var command = new SqliteCommand("SELECT Id, Timestamp FROM TranslationHistory", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    long id = reader.GetInt64(reader.GetOrdinal("Id"));
                    string timestamp = reader.GetString(reader.GetOrdinal("Timestamp"));
                    records.Add((id, timestamp));
                }
            }
            
            foreach (var (id, timestamp) in records)
            {
                if (DateTime.TryParse(timestamp, out DateTime dt))
                {
                    long unixTime = ((DateTimeOffset)dt).ToUnixTimeSeconds();
                    using var updateCommand = new SqliteCommand(
                        "UPDATE TranslationHistory SET Timestamp = @Timestamp WHERE Id = @Id",
                        connection);
                    updateCommand.Parameters.AddWithValue("@Id", id);
                    updateCommand.Parameters.AddWithValue("@Timestamp", unixTime.ToString());
                    await updateCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
}