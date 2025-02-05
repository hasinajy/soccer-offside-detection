using Microsoft.Win32;
using Models;
using Services;
using Services.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace UI
{
    public partial class ScoreTrackerWindow : Window
    {
        private BitmapImage? beforeImageSource;
        private BitmapImage? afterImageSource;
        // Add these fields at the class level
        private int teamABlocks = 0;
        private int teamBBlocks = 0;

        public ScoreTrackerWindow()
        {
            InitializeComponent();
        }

        private void ImportBeforeImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    beforeImageSource = new BitmapImage(new Uri(openFileDialog.FileName));
                    BeforeImage.Source = beforeImageSource;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportAfterImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    afterImageSource = new BitmapImage(new Uri(openFileDialog.FileName));
                    AfterImage.Source = afterImageSource;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProcessImages_Click(object sender, RoutedEventArgs e)
        {
            if (beforeImageSource == null || afterImageSource == null)
            {
                MessageBox.Show("Please import both before and after images before processing.",
                              "Missing Images",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Save the BitmapSource to temporary files for processing
                string beforeTempPath = "img/tmp/" + System.IO.Path.GetRandomFileName() + ".jpg";
                string afterTempPath = "img/tmp/" + System.IO.Path.GetRandomFileName() + ".jpg";

                SaveImageToFile(beforeImageSource, beforeTempPath);
                SaveImageToFile(afterImageSource, afterTempPath);

                // Process the 'before' image
                var beforeProcessor = new ImageProcessor(beforeTempPath);
                var beforePlayers = beforeProcessor.DetectPlayers();
                var beforeBallPosition = beforeProcessor.DetectBall();

                // Process the 'after' image
                var afterProcessor = new ImageProcessor(afterTempPath);
                var afterPlayers = afterProcessor.DetectPlayers();
                var afterBallPosition = afterProcessor.DetectBall();

                // Analyze offside positions in the 'before' image
                OffsideAnalyzer.AnalyzeOffside(beforePlayers, beforeBallPosition);

                // Find ball holder in 'before' image
                var ballHolder = beforePlayers.FirstOrDefault(p => p.HasBall);
                if (ballHolder == null) return;

                // Create configuration
                var config = new GoalHistoryConfiguration
                {
                    Format = StorageFormat.Json,
                    LogDirectory = "log",
                    FileName = "goal-history"
                };

                // Initialize tracker
                var tracker = new GoalHistoryTracker(config);

                // Check if ball holder is offside
                if (ballHolder.IsOffside)
                {
                    // TODO: Log offside event
                    MessageBox.Show("Offside position detected - No goal awarded",
                                  "Offside",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                    return;
                }

                var goalFields = GoalDetector.DetectGoals(afterTempPath, afterPlayers);
                var goalAttempt = IsPointInGoal(afterBallPosition, goalFields, ballHolder, afterPlayers);

                if (goalAttempt.IsBlocked)
                {
                    // Determine which team made the block (opposite of ball holder)
                    if (ballHolder.Team == TeamType.TeamA)
                    {
                        teamBBlocks++;
                    }
                    else
                    {
                        teamABlocks++;
                    }

                    MessageBox.Show($"Team A {teamABlocks} - {teamBBlocks} Team B",
                                  "Blocked",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else if (goalAttempt.IsGoal)
                {
                    // Award point to the attacking team
                    if (ballHolder.Team == TeamType.TeamA)
                    {
                        int currentScore = int.Parse(ScoreTeamA.Text);
                        ScoreTeamA.Text = (currentScore + 1).ToString();
                        tracker.UpdateHistory(TeamType.TeamA, afterBallPosition);
                    }
                    else
                    {
                        int currentScore = int.Parse(ScoreTeamB.Text);
                        ScoreTeamB.Text = (currentScore + 1).ToString();
                        tracker.UpdateHistory(TeamType.TeamB, afterBallPosition);
                    }

                    MessageBox.Show($"Goal scored by {ballHolder.Team}!",
                                  "Goal!",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else
                {
                    // TODO: Log missed shot event
                    MessageBox.Show("Shot missed - Ball not in goal",
                                  "Miss",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }

                // Save annotated images
                beforeProcessor.SaveAnnotatedImage("img/before_annotated.jpg", beforePlayers, beforeBallPosition);
                afterProcessor.SaveAnnotatedImage("img/after_annotated.jpg", afterPlayers, afterBallPosition);

                // Clean up temporary files
                try
                {
                    System.IO.File.Delete(beforeTempPath);
                    System.IO.File.Delete(afterTempPath);
                }
                catch { /* Ignore cleanup errors */ }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing images: {ex.Message}",
                              "Processing Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        public record GoalAttemptResult(bool IsGoal, bool IsBlocked, TeamType? ScoringTeam);

        private static GoalAttemptResult IsPointInGoal(
            System.Drawing.Point ballPosition,
            List<GoalField> goals,
            Player ballHolder,
            List<Player> players)
        {
            foreach (var goal in goals)
            {
                // Add some padding to the goal bounds to be more lenient with detection
                var expandedBounds = new Rectangle(
                    goal.Bounds.X - 5,
                    goal.Bounds.Y - 5,
                    goal.Bounds.Width + 10,
                    goal.Bounds.Height + 10
                );

                if (expandedBounds.Contains(ballPosition))
                {
                    // Check if this is the opposing team's goal
                    if (ballHolder.Team != goal.Team)
                    {
                        // Check for goalkeeper block
                        if (IsBlockedByGoalkeeper(ballPosition, goal.Team, players))
                        {
                            return new GoalAttemptResult(false, true, null);
                        }
                        return new GoalAttemptResult(true, false, ballHolder.Team);
                    }
                    // Ball is in own team's goal - not a valid goal
                    return new GoalAttemptResult(false, false, null);
                }
            }

            return new GoalAttemptResult(false, false, null);
        }

        private static bool IsBlockedByGoalkeeper(System.Drawing.Point ballPosition, TeamType defendingTeam, List<Player> players)
        {
            var goalkeeper = FindGoalkeeper(defendingTeam, players);
            if (goalkeeper == null) return false;

            // Define goalkeeper's reach radius (adjust as needed)
            const int goalkeeperReachRadius = 30;

            // Calculate distance between ball and goalkeeper
            double distance = CalculateDistance(ballPosition, goalkeeper.Position);

            // First check if the goalkeeper is within reach radius
            if (distance > (goalkeeperReachRadius + 15)) return false;

            // Check if the ball is in front of the goalkeeper based on team position
            bool isBallInFront = defendingTeam == TeamType.TeamA
                ? (ballPosition.Y > goalkeeper.Position.Y && ballPosition.X > (goalkeeper.Position.X - 15) && ballPosition.X < (goalkeeper.Position.X + 15))  // For Team A (top), ball must be below goalkeeper
                : (ballPosition.Y < goalkeeper.Position.Y && ballPosition.X > (goalkeeper.Position.X - 15) && ballPosition.X < (goalkeeper.Position.X + 15)); // For Team B (bottom), ball must be above goalkeeper

            return isBallInFront;
        }

        private static Player? FindGoalkeeper(TeamType team, List<Player> players)
        {
            // Get all players from the defending team
            var teamPlayers = players.Where(p => p.Team == team).ToList();
            if (!teamPlayers.Any()) return null;

            // Order players by Y position
            var orderedPlayers = teamPlayers.OrderBy(p => p.Position.Y).ToList();

            // If this is Team A (top team), return the player with lowest Y (top of the field)
            // If this is Team B (bottom team), return the player with highest Y (bottom of the field)
            return team == TeamType.TeamA
                ? orderedPlayers.FirstOrDefault()    // Top goalkeeper
                : orderedPlayers.LastOrDefault();  // Bottom goalkeeper
        }

        private static double CalculateDistance(System.Drawing.Point p1, System.Drawing.Point p2)
        {
            int dx = p1.X - p2.X;
            int dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static void SaveImageToFile(BitmapSource bitmapSource, string filePath)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
    }
}