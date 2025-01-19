using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using Models;
using Services.Configuration;

namespace Services
{
    public class GoalHistoryTracker
    {
        private readonly GoalHistoryConfiguration _config;
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public GoalHistoryTracker(GoalHistoryConfiguration config)
        {
            _config = config;
            _filePath = Path.Combine(
                config.LogDirectory,
                $"{config.FileName}.{GetFileExtension()}"
            );
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        private string GetFileExtension() => _config.Format == StorageFormat.Json ? "json" : "log";

        public void UpdateHistory(TeamType scoringTeam, Point ballPosition)
        {
            var goal = new Goal(scoringTeam, ballPosition, DateTime.Now);

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_filePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (_config.Format == StorageFormat.Json)
                {
                    UpdateJsonHistory(goal);
                }
                else
                {
                    UpdatePlainTextHistory(goal);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to goal history: {ex.Message}");
                throw;
            }
        }

        private void UpdateJsonHistory(Goal goal)
        {
            List<Goal> goals;

            if (File.Exists(_filePath))
            {
                var jsonContent = File.ReadAllText(_filePath);
                goals = JsonSerializer.Deserialize<List<Goal>>(jsonContent, _jsonOptions) ?? new List<Goal>();
            }
            else
            {
                goals = new List<Goal>();
            }

            goals.Add(goal);
            var jsonString = JsonSerializer.Serialize(goals, _jsonOptions);

            File.WriteAllText(_filePath, jsonString);
        }

        private void UpdatePlainTextHistory(Goal goal)
        {
            var message = FormatGoalMessage(goal);
            File.AppendAllText(_filePath, message + Environment.NewLine);
        }

        public List<Goal> GetGoalHistory()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new List<Goal>();
                }

                if (_config.Format == StorageFormat.Json)
                {
                    var jsonContent = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<List<Goal>>(jsonContent, _jsonOptions) ?? new List<Goal>();
                }
                else
                {
                    var lines = File.ReadAllLines(_filePath);
                    return ParsePlainTextHistory(lines);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading goal history: {ex.Message}");
                throw;
            }
        }

        private static List<Goal> ParsePlainTextHistory(string[] lines)
        {
            var goals = new List<Goal>();
            foreach (var line in lines)
            {
                // Example format: [2024-01-19 15:30:45.123] GOAL! TeamA scored at position X:100, Y:200
                var match = System.Text.RegularExpressions.Regex.Match(line,
                    @"\[(.*?)\] GOAL! (.*?) scored at position X:(\d+), Y:(\d+)");

                if (match.Success)
                {
                    var timestamp = DateTime.Parse(match.Groups[1].Value);
                    var team = Enum.Parse<TeamType>(match.Groups[2].Value);
                    var x = int.Parse(match.Groups[3].Value);
                    var y = int.Parse(match.Groups[4].Value);

                    goals.Add(new Goal(team, new Point(x, y), timestamp));
                }
            }
            return goals;
        }

        private static string FormatGoalMessage(Goal goal)
        {
            return $"[{goal.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] GOAL! {goal.ScoringTeam} scored at position X:{goal.BallPosition.X}, Y:{goal.BallPosition.Y}";
        }

        public void ClearHistory()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing goal history: {ex.Message}");
                throw;
            }
        }
    }
}