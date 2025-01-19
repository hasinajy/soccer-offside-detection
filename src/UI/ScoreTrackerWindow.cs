using Microsoft.Win32;
using Models;
using Services;
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

                // Check if ball holder is offside
                if (ballHolder.IsOffside)
                {
                    UpdateHistory("Goal missed (offside)");
                    MessageBox.Show("Offside position detected - No goal awarded",
                                  "Offside",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                    return;
                }

                // Check for goal in 'after' image
                var goalFields = GoalDetector.DetectGoals(afterTempPath, afterPlayers);
                var ballStatus = IsPointInGoal(afterBallPosition, goalFields, ballHolder);

                if (ballStatus.isGoal)
                {
                    // Award point to the attacking team
                    if (ballHolder.Team == TeamType.TeamA)
                    {
                        int currentScore = int.Parse(ScoreTeamA.Text);
                        ScoreTeamA.Text = (currentScore + 1).ToString();
                        UpdateHistory("Goal scored by Team A");
                    }
                    else
                    {
                        int currentScore = int.Parse(ScoreTeamB.Text);
                        ScoreTeamB.Text = (currentScore + 1).ToString();
                        UpdateHistory("Goal scored by Team B");
                    }

                    MessageBox.Show($"Goal scored by {ballHolder.Team}!",
                                  "Goal!",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else
                {
                    UpdateHistory("Goal missed (outside)");
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

        private static (bool isGoal, TeamType? scoringTeam) IsPointInGoal(System.Drawing.Point ballPosition, List<Goal> goals, Player ballHolder)
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
                    // Only count it as a goal if:
                    // 1. The ball is in a goal
                    // 2. The ball holder's team is different from the goal's team
                    if (ballHolder.Team != goal.Team)
                    {
                        return (true, ballHolder.Team);
                    }
                    // Ball is in own team's goal - not a valid goal
                    return (false, null);
                }
            }

            return (false, null);
        }

        private static void UpdateHistory(string message)
        {
            // Implement history tracking (e.g., add to a ListBox or save to a file)
            // You'll need to add a control to the XAML to display this
            Debug.WriteLine($"History: {message}");
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