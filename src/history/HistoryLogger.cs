using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Windows.Controls;

namespace LiveCaptionsTranslator.models
{
    public class TranslationHistoryEntry
    {
        public string Timestamp { get; set; }
        public string TimestampFull { get; set; }
        public string SourceText { get; set; }
        public string TranslatedText { get; set; }
        public string TargetLanguage { get; set; }
        public string ApiUsed { get; set; }
    }

    public static class SQLiteHistoryLogger
    {
        private static readonly string ConnectionString = "Data Source=translation_history.db;";

        static SQLiteHistoryLogger()
        {
            using (var connection = new SqliteConnection(ConnectionString))
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

        public static async Task LogTranslation(string sourceText, string translatedText, string targetLanguage,
            string apiUsed)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                await connection.OpenAsync();
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
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public static async Task<(List<TranslationHistoryEntry>, int)> LoadHistoryAsync(int page, int maxRow)
        {
            var history = new List<TranslationHistoryEntry>();
            int maxPage = 1;

            using (var connection = new SqliteConnection(ConnectionString))
            {
                await connection.OpenAsync();

                // Get max page
                using (var command = new SqliteCommand("SELECT COUNT() AS maxPage FROM TranslationHistory", connection))
                    maxPage = Convert.ToInt32(command.ExecuteScalar()) / maxRow;

                // Get table
                using (var command = new SqliteCommand(@"
                    SELECT Timestamp, SourceText, TranslatedText, TargetLanguage, ApiUsed
                    FROM TranslationHistory
                    ORDER BY Timestamp DESC
                    LIMIT " + maxRow + " OFFSET " + ((page * maxRow) - maxRow), connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
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
            return (history, maxPage);
        }
        public static async Task ClearHistory()
        {

            using (var connection = new SqliteConnection(ConnectionString))
            {
                await connection.OpenAsync();
                string selectQuery = "DELETE FROM TranslationHistory; DELETE FROM sqlite_sequence WHERE NAME='TranslationHistory";
                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

            }
        }
        
        public static async Task ExportToCsv(string filePath)
        {
            var history = new List<TranslationHistoryEntry>();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                await connection.OpenAsync();

                string selectQuery = @"
                SELECT Timestamp, SourceText, TranslatedText, TargetLanguage, ApiUsed
                FROM TranslationHistory
                ORDER BY Timestamp DESC";
                using (var command = new SqliteCommand(selectQuery, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
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
            {
                csv.AppendLine($"{entry.Timestamp},{entry.SourceText},{entry.TranslatedText},{entry.TargetLanguage},{entry.ApiUsed}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }
    }
}