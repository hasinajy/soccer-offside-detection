using System.Drawing;
using Services;

namespace Main
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Initialize services
                var imagePath = "img/test-offside.jpg";  // Replace with your image path
                var imageProcessor = new ImageProcessor(imagePath);
                var offsideAnalyzer = new OffsideAnalyzer();

                // Step 1: Detect players
                Console.WriteLine("Detecting players...");
                var players = imageProcessor.DetectPlayers();
                Console.WriteLine($"Found {players.Count} players");

                // Step 2: Detect ball
                Console.WriteLine("Detecting ball...");
                var ballPosition = imageProcessor.DetectBall();
                Console.WriteLine($"Ball found at position: ({ballPosition.X}, {ballPosition.Y})");

                // Step 3: Analyze offside positions
                Console.WriteLine("Analyzing offside positions...");
                offsideAnalyzer.AnalyzeOffside(players, ballPosition);

                // Step 4: Save annotated image
                Console.WriteLine("Saving annotated image...");
                imageProcessor.SaveAnnotatedImage("img/output-offside.jpg", players, ballPosition);

                Console.WriteLine("Processing complete! Check output.jpg for results.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}