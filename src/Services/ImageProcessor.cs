using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Models;
using System.Drawing;

namespace Services
{
    public class ImageProcessor
    {
        private readonly Mat _originalImage;
        private readonly MCvScalar _teamAColor = new MCvScalar(0, 0, 255);    // Red visualization color
        private readonly MCvScalar _teamBColor = new MCvScalar(255, 0, 0);    // Blue visualization color
        private readonly MCvScalar _ballColor = new MCvScalar(0, 0, 0);       // Black

        public ImageProcessor(string imagePath)
        {
            _originalImage = CvInvoke.Imread(imagePath);
            if (_originalImage.IsEmpty)
                throw new ArgumentException("Failed to load image", nameof(imagePath));
        }

        public List<Player> DetectPlayers()
        {
            var players = new List<Player>();

            // Convert to HSV for better color detection
            using var hsvImage = new Mat();
            CvInvoke.CvtColor(_originalImage, hsvImage, ColorConversion.Bgr2Hsv);

            // Detect Team A (Red #FF002A)
            using var teamAMask = new Mat();
            // Range for red color (considering the wrap-around in HSV)
            using var teamAMaskLower = new Mat();
            using var teamAMaskUpper = new Mat();

            // Red can wrap around the HSV cylinder, so we need two ranges
            var lowerRed1 = new ScalarArray(new MCvScalar(0, 200, 200));    // Start of red spectrum
            var upperRed1 = new ScalarArray(new MCvScalar(10, 255, 255));   // End of first red range
            var lowerRed2 = new ScalarArray(new MCvScalar(165, 200, 200));  // Start of second red range
            var upperRed2 = new ScalarArray(new MCvScalar(180, 255, 255));  // End of red spectrum

            // Create two masks for red and combine them
            CvInvoke.InRange(hsvImage, lowerRed1, upperRed1, teamAMaskLower);
            CvInvoke.InRange(hsvImage, lowerRed2, upperRed2, teamAMaskUpper);
            CvInvoke.Add(teamAMaskLower, teamAMaskUpper, teamAMask);

            // Detect Team B (Blue #00BBFF)
            using var teamBMask = new Mat();
            var lowerBlue = new ScalarArray(new MCvScalar(90, 200, 200));   // Start of blue range
            var upperBlue = new ScalarArray(new MCvScalar(105, 255, 255));  // End of blue range
            CvInvoke.InRange(hsvImage, lowerBlue, upperBlue, teamBMask);

            // Find contours for both teams
            AddPlayersFromMask(teamAMask, TeamType.TeamA, players);
            AddPlayersFromMask(teamBMask, TeamType.TeamB, players);

            return players;
        }

        private void AddPlayersFromMask(Mat mask, TeamType team, List<Player> players)
        {
            using var hierarchy = new Mat();
            using var contours = new VectorOfVectorOfPoint();

            // Apply some morphological operations to clean up the mask
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
            CvInvoke.MorphologyEx(mask, mask, MorphOp.Open, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
            CvInvoke.MorphologyEx(mask, mask, MorphOp.Close, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());

            CvInvoke.FindContours(
                mask,
                contours,
                hierarchy,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            for (int i = 0; i < contours.Size; i++)
            {
                // Filter small contours that might be noise
                double area = CvInvoke.ContourArea(contours[i]);
                if (area < 100) continue; // Adjust this threshold based on your image size

                var moments = CvInvoke.Moments(contours[i]);
                var centerX = (int)(moments.M10 / moments.M00);
                var centerY = (int)(moments.M01 / moments.M00);

                players.Add(new Player(new Point(centerX, centerY), team));
            }
        }

        public Point DetectBall()
        {
            using var ballMask = new Mat();
            var lowerBlack = new ScalarArray(new MCvScalar(0, 0, 0));
            var upperBlack = new ScalarArray(new MCvScalar(50, 50, 50));
            CvInvoke.InRange(_originalImage, lowerBlack, upperBlack, ballMask);

            using var hierarchy = new Mat();
            using var contours = new VectorOfVectorOfPoint();

            CvInvoke.FindContours(
                ballMask,
                contours,
                hierarchy,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            if (contours.Size > 0)
            {
                var moments = CvInvoke.Moments(contours[0]);
                return new Point(
                    (int)(moments.M10 / moments.M00),
                    (int)(moments.M01 / moments.M00)
                );
            }

            throw new Exception("Ball not found in image");
        }

        public void SaveAnnotatedImage(string outputPath, List<Player> players, Point ballPosition)
        {
            var annotatedImage = _originalImage.Clone();

            // Draw players
            foreach (var player in players.Where(p => p.IsAnnotated))
            {
                var color = player.Team == TeamType.TeamA ? _teamAColor : _teamBColor;
                CvInvoke.Circle(annotatedImage, player.Position, 10, color, -1);

                // Draw offside status
                var textColor = player.IsOffside ? new MCvScalar(0, 0, 255) : new MCvScalar(0, 255, 0);
                var status = player.IsOffside ? "OFF" : "CLN";
                CvInvoke.PutText(annotatedImage, status,
                    new Point(player.Position.X - 20, player.Position.Y - 20),
                    FontFace.HersheySimplex, 0.5, textColor, 2);
            }

            // Draw arrows from ball to players who are annotated and not offside
            foreach (var player in players.Where(p => p.IsAnnotated && !p.IsOffside))
            {
                CvInvoke.ArrowedLine(
                    annotatedImage,                    // The image to draw on
                    ballPosition,                      // Starting point (ball position)
                    player.Position,                   // Ending point (player position)
                    new MCvScalar(255, 0, 0),          // Arrow color (blue in BGR format)
                    2,                                 // Thickness of the arrow
                    Emgu.CV.CvEnum.LineType.AntiAlias, // Smooth anti-aliased line
                    0,                                 // No fractional shift
                    0.1                                // Arrowhead size relative to line length
                );
            }

            // Draw ball
            CvInvoke.Circle(annotatedImage, ballPosition, 5, _ballColor, -1);

            // Save the annotated image
            CvInvoke.Imwrite(outputPath, annotatedImage);
        }
    }
}