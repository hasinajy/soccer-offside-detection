using Microsoft.Win32;
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

            // TODO: Add your image processing and score tracking logic here
            // This is where you would implement your score detection algorithm
            // and update the ScoreTeamA and ScoreTeamB TextBlocks accordingly
        }
    }
}