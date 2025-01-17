using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Services;

namespace UI
{
    public partial class OffsideDetectorWindow : Window
    {
        private string? _currentImagePath;
        private ImageProcessor? _imageProcessor;
        private readonly OffsideAnalyzer _offsideAnalyzer;

        public OffsideDetectorWindow()
        {
            InitializeComponent();
            _offsideAnalyzer = new OffsideAnalyzer();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*",
                Title = "Select an image file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _currentImagePath = openFileDialog.FileName;
                    var bitmap = new BitmapImage(new Uri(_currentImagePath));
                    InputImage.Source = bitmap;
                    ProcessButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessButton.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;

                // Process the image
                _imageProcessor = new ImageProcessor(_currentImagePath);
                var players = _imageProcessor.DetectPlayers();
                var ballPosition = _imageProcessor.DetectBall();
                _offsideAnalyzer.AnalyzeOffside(players, ballPosition);

                // Save and display the result
                string outputPath = Path.Combine(
                    Path.GetDirectoryName(_currentImagePath),
                    "output_" + Path.GetFileName(_currentImagePath)
                );
                _imageProcessor.SaveAnnotatedImage(outputPath, players, ballPosition);

                // Display the output image
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(outputPath);
                bitmap.EndInit();
                OutputImage.Source = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProcessButton.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }
    }
}