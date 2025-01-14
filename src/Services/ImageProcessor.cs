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
        private readonly MCvScalar _teamAColor = new(0, 0, 255);    // Red
        private readonly MCvScalar _teamBColor = new(255, 0, 0);    // Blue
        private readonly MCvScalar _ballColor = new(0, 0, 0);       // Black

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

            // Detect Team A (Red)
            using var teamAMask = new Mat();
            var lowerRed = new ScalarArray(new MCvScalar(160, 100, 100));
            var upperRed = new ScalarArray(new MCvScalar(180, 255, 255));
            CvInvoke.InRange(hsvImage, lowerRed, upperRed, teamAMask);

            // Detect Team B (Blue)
            using var teamBMask = new Mat();
            var lowerBlue = new ScalarArray(new MCvScalar(100, 100, 100));
            var upperBlue = new ScalarArray(new MCvScalar(130, 255, 255));
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

            CvInvoke.FindContours(
                mask,
                contours,
                hierarchy,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            for (int i = 0; i < contours.Size; i++)
            {
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
            foreach (var player in players)
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

            // Draw ball
            CvInvoke.Circle(annotatedImage, ballPosition, 5, _ballColor, -1);

            CvInvoke.Imwrite(outputPath, annotatedImage);
        }
    }
}