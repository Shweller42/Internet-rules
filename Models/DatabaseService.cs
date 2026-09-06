using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WpfApp1.Models;

public static class DatabaseService
{
    private static string _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "progress.db");
    private static string _connStr => $"Data Source={_dbPath}";

    public static void Initialize()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Progress (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TopicName TEXT NOT NULL,
                Percentage REAL NOT NULL DEFAULT 0,
                Grade TEXT NOT NULL DEFAULT '',
                CompletedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );
            CREATE TABLE IF NOT EXISTS TestAnswers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TopicName TEXT NOT NULL,
                QuestionText TEXT NOT NULL,
                UserAnswer TEXT NOT NULL DEFAULT '',
                CorrectAnswer TEXT NOT NULL DEFAULT ''
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public static void SaveProgress(string topicName, double percentage, string grade)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Progress (TopicName, Percentage, Grade) VALUES (@name, @pct, @grade)";
        cmd.Parameters.AddWithValue("@name", topicName);
        cmd.Parameters.AddWithValue("@pct", percentage);
        cmd.Parameters.AddWithValue("@grade", grade);
        cmd.ExecuteNonQuery();
    }

    public static void SaveTestAnswers(string topicName, List<ResultError> errors)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var tx = conn.BeginTransaction();
        foreach (var err in errors)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO TestAnswers (TopicName, QuestionText, UserAnswer, CorrectAnswer) VALUES (@name, @q, @ua, @ca)";
            cmd.Parameters.AddWithValue("@name", topicName);
            cmd.Parameters.AddWithValue("@q", err.Question);
            cmd.Parameters.AddWithValue("@ua", err.UserAnswer);
            cmd.Parameters.AddWithValue("@ca", err.CorrectAnswer);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public static List<ProgressRecord> LoadProgress()
    {
        var list = new List<ProgressRecord>();
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TopicName, MAX(Percentage) as Pct FROM Progress GROUP BY TopicName";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new ProgressRecord
            {
                TopicName = reader.GetString(0),
                ThemeName = reader.GetString(0),
                Percentage = reader.GetDouble(1)
            });
        }
        return list;
    }

    public static double GetTopicPercentage(string topicName)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(Percentage), 0) FROM Progress WHERE TopicName = @name";
        cmd.Parameters.AddWithValue("@name", topicName);
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToDouble(result) : 0;
    }

    public static void ResetAll()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Progress; DELETE FROM TestAnswers;";
        cmd.ExecuteNonQuery();
    }
}