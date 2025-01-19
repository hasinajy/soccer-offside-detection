using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Models;

namespace Services
{
    public class GoalHistoryTracker
    {
        private GoalHistoryTracker() { }

        private static readonly string LogFilePath = Path.Combine(
            "log",
            "goal-history.log"
        );

        public static void UpdateHistory(TeamType scoringTeam, Point ballPosition)
        {
            var message = FormatGoalMessage(scoringTeam, ballPosition, DateTime.Now);

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(LogFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Append to file with automatic file handling
                File.AppendAllText(LogFilePath, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error writing to goal history: {ex.Message}");
                throw;
            }
        }

        public static List<string> GetGoalHistory()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return new List<string>();
                }

                return File.ReadAllLines(LogFilePath).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading goal history: {ex.Message}");
                throw;
            }
        }

        private static string FormatGoalMessage(TeamType scoringTeam, Point ballPosition, DateTime timestamp)
        {
            return $"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] GOAL! {scoringTeam} scored at position X:{ballPosition.X}, Y:{ballPosition.Y}";
        }

        public static void ClearHistory()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
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