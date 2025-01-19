using Microsoft.Win32;
using Models;
using Services;
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
                string beforeTempPath = System.IO.Path.GetRandomFileName() + ".jpg";
                string afterTempPath = System.IO.Path.GetRandomFileName() + ".jpg";

                SaveImageToFile(beforeImageSource, beforeTempPath);
                SaveImageToFile(afterImageSource, afterTempPath);

                // Create detector and process images
                int beforeGoals = GoalDetector.DetectGoals(beforeTempPath);
                int afterGoals = GoalDetector.DetectGoals(afterTempPath);

                // Update the UI with results
                MessageBox.Show($"Goals detected:\nBefore image: {beforeGoals}\nAfter image: {afterGoals}",
                              "Detection Results",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);

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