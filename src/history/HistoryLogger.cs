using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace LiveCaptionsTranslator.models
{
    public class TranslationHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public required string SourceText { get; set; }
        public required string TranslatedText { get; set; }
        public required string TargetLanguage { get; set; }
        public required string ApiUsed { get; set; }

        public TranslationHistoryEntry()
        {
            Timestamp = DateTime.Now;
            SourceText = string.Empty;
            TranslatedText = string.Empty;
            TargetLanguage = string.Empty;
            ApiUsed = string.Empty;
        }
    }

    public static class SQLiteHistoryLogger
    {
        private static readonly string ConnectionString = "Data Source=translation_history.db;Version=3;";
        private const int PAGE_SIZE = 100; // 每页显示的记录数

        static SQLiteHistoryLogger()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
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
                    )";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static async Task ClearHistoryAsync()
        {
            await Task.Run(() =>
            {
                using var connection = new SQLiteConnection(ConnectionString);
                connection.Open();
                using var command = new SQLiteCommand("DELETE FROM TranslationHistory", connection);
                command.ExecuteNonQuery();
            });
        }

        public static async Task LogTranslationAsync(string sourceText, string translatedText, string targetLanguage,
            string apiUsed)
        {
            if (!App.Settings.EnableLogging)
            {
                return;
            }

            await Task.Run(() =>
            {
                using var connection = new SQLiteConnection(ConnectionString);
                connection.Open();
                using var command = new SQLiteCommand(
                    @"INSERT INTO TranslationHistory (Timestamp, SourceText, TranslatedText, TargetLanguage, ApiUsed) 
                      VALUES (@timestamp, @sourceText, @translatedText, @targetLanguage, @apiUsed)",
                    connection
                );

                command.Parameters.AddWithValue("@timestamp", DateTime.Now);
                command.Parameters.AddWithValue("@sourceText", sourceText);
                command.Parameters.AddWithValue("@translatedText", translatedText);
                command.Parameters.AddWithValue("@targetLanguage", targetLanguage);
                command.Parameters.AddWithValue("@apiUsed", apiUsed);

                command.ExecuteNonQuery();
            });
        }

        public static async Task<List<TranslationHistoryEntry>> LoadHistoryAsync(int page = 1)
        {
            return await Task.Run(() =>
            {
                var history = new List<TranslationHistoryEntry>();
                using var connection = new SQLiteConnection(ConnectionString);
                connection.Open();
                using var command = new SQLiteCommand(@"
                    WITH RankedHistory AS (
                        SELECT *,
                            ROW_NUMBER() OVER (PARTITION BY SourceText ORDER BY Timestamp DESC) as rn
                        FROM TranslationHistory
                    )
                    SELECT Timestamp, SourceText, TranslatedText, TargetLanguage, ApiUsed
                    FROM RankedHistory
                    WHERE rn = 1
                    ORDER BY Timestamp DESC
                    LIMIT @pageSize OFFSET @offset",
                    connection
                );

                command.Parameters.AddWithValue("@pageSize", PAGE_SIZE);
                command.Parameters.AddWithValue("@offset", (page - 1) * PAGE_SIZE);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    history.Add(new TranslationHistoryEntry
                    {
                        Timestamp = DateTime.Parse(reader["Timestamp"]?.ToString() ?? DateTime.Now.ToString()),
                        SourceText = reader["SourceText"]?.ToString() ?? string.Empty,
                        TranslatedText = reader["TranslatedText"]?.ToString() ?? string.Empty,
                        TargetLanguage = reader["TargetLanguage"]?.ToString() ?? string.Empty,
                        ApiUsed = reader["ApiUsed"]?.ToString() ?? string.Empty
                    });
                }

                return history;
            });
        }

        public static async Task<int> GetTotalPagesAsync()
        {
            return await Task.Run(() =>
            {
                using var connection = new SQLiteConnection(ConnectionString);
                connection.Open();
                using var command = new SQLiteCommand(@"
                    SELECT COUNT(DISTINCT SourceText) as total 
                    FROM TranslationHistory",
                    connection
                );
                var total = Convert.ToInt32(command.ExecuteScalar());
                return (total + PAGE_SIZE - 1) / PAGE_SIZE;
            });
        }
    }
}