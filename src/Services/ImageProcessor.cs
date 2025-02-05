using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Models;
using System.Drawing;

namespace Services
{
    public record ColorConfiguration
    {
        public required MCvScalar TeamA { get; init; }
        public required MCvScalar TeamB { get; init; }
        public required MCvScalar Ball { get; init; }
    }

    public record HsvBounds(MCvScalar Lower, MCvScalar Upper);

    public record HsvRange
    {
        public HsvBounds Lower { get; }
        public HsvBounds? Upper { get; }
        public bool HasTwoRanges => Upper != null;

        public HsvRange(HsvBounds lower, HsvBounds? upper = null)
        {
            Lower = lower;
            Upper = upper;
        }
    }

    public class ImageProcessor
    {
        private readonly Mat _originalImage;
        private static readonly ColorConfiguration _colorConfig = new()
        {
            TeamA = new MCvScalar(0, 0, 255),    // Red visualization
            TeamB = new MCvScalar(255, 0, 0),    // Blue visualization
            Ball = new MCvScalar(0, 0, 0)        // Black
        };

        private static readonly HsvRange _teamARange = new(
            new HsvBounds(new MCvScalar(0, 50, 50), new MCvScalar(10, 255, 255)),     // Lower red range
            new HsvBounds(new MCvScalar(170, 50, 50), new MCvScalar(180, 255, 255))   // Upper red range
        );

        private static readonly HsvRange _teamBRange = new(
            new HsvBounds(new MCvScalar(85, 50, 50), new MCvScalar(125, 255, 255))
        );

        public ImageProcessor(string imagePath)
        {
            _originalImage = CvInvoke.Imread(imagePath);
            if (_originalImage.IsEmpty)
                throw new ArgumentException("Failed to load image", nameof(imagePath));
        }

        public List<Player> DetectPlayers()
        {
            var players = new List<Player>();
            using var hsvImage = ConvertToHSV(_originalImage);

            // Detect players for both teams
            var teamAPlayers = DetectTeamPlayers(hsvImage, _teamARange, TeamType.TeamA);
            var teamBPlayers = DetectTeamPlayers(hsvImage, _teamBRange, TeamType.TeamB);

            players.AddRange(teamAPlayers);
            players.AddRange(teamBPlayers);

            return players;
        }

        public Point DetectBall()
        {
            using var ballMask = new Mat();
            // Detect objects within the specified color range
            CvInvoke.InRange(
                _originalImage,
                new ScalarArray(new MCvScalar(0, 0, 0)),
                new ScalarArray(new MCvScalar(50, 50, 50)),
                ballMask
            );

            using var contours = FindContours(ballMask);
            if (contours.Size == 0)
                throw new InvalidOperationException("No objects found matching the color criteria");

            // Find the most circular contour
            var mostCircularContour = FindMostCircularContour(contours) ?? throw new InvalidOperationException("No circular objects found matching the criteria");
            return CalculateCentroid(mostCircularContour);
        }

        private static VectorOfPoint? FindMostCircularContour(VectorOfVectorOfPoint contours)
        {
            double bestCircularity = 0;
            VectorOfPoint? mostCircularContour = null;
            double minCircularity = 0.7; // Adjust this threshold as needed (1.0 is perfect circle)

            for (int i = 0; i < contours.Size; i++)
            {
                var contour = contours[i];
                double circularity = CalculateCircularity(contour);

                if (circularity > minCircularity && circularity > bestCircularity)
                {
                    bestCircularity = circularity;
                    mostCircularContour = contour;
                }
            }

            return mostCircularContour;
        }

        private static double CalculateCircularity(VectorOfPoint contour)
        {
            double area = CvInvoke.ContourArea(contour);
            double perimeter = CvInvoke.ArcLength(contour, true);

            // Circularity formula: 4π * area / (perimeter^2)
            // Perfect circle has circularity of 1.0
            if (perimeter > 0)
                return (4 * Math.PI * area) / (perimeter * perimeter);
            return 0;
        }

        public void SaveAnnotatedImage(string outputPath, List<Player> players, Point ballPosition)
        {
            using var annotatedImage = _originalImage.Clone();
            DrawPlayers(annotatedImage, players);
            DrawPassingLines(annotatedImage, players, ballPosition);
            DrawBall(annotatedImage, ballPosition);

            CvInvoke.Imwrite(outputPath, annotatedImage);
        }

        private static Mat ConvertToHSV(Mat image)
        {
            var hsvImage = new Mat();
            CvInvoke.CvtColor(image, hsvImage, ColorConversion.Bgr2Hsv);
            return hsvImage;
        }

        private static List<Player> DetectTeamPlayers(Mat hsvImage, HsvRange range, TeamType team)
        {
            using var mask = CreateTeamMask(hsvImage, range);
            return DetectPlayersFromMask(mask, team);
        }

        private static Mat CreateTeamMask(Mat hsvImage, HsvRange range)
        {
            var mask = new Mat();

            if (range.HasTwoRanges && range.Upper != null)
            {
                using var lowerMask = new Mat();
                using var upperMask = new Mat();

                CvInvoke.InRange(hsvImage,
                    new ScalarArray(range.Lower.Lower),
                    new ScalarArray(range.Lower.Upper),
                    lowerMask);

                CvInvoke.InRange(hsvImage,
                    new ScalarArray(range.Upper.Lower),
                    new ScalarArray(range.Upper.Upper),
                    upperMask);

                CvInvoke.Add(lowerMask, upperMask, mask);
            }
            else
            {
                CvInvoke.InRange(hsvImage,
                    new ScalarArray(range.Lower.Lower),
                    new ScalarArray(range.Lower.Upper),
                    mask);
            }

            ApplyMorphologicalOperations(mask);
            return mask;
        }

        private static void ApplyMorphologicalOperations(Mat mask)
        {
            using var kernel = CvInvoke.GetStructuringElement(
                ElementShape.Rectangle,
                new Size(3, 3),
                new Point(-1, -1));

            CvInvoke.MorphologyEx(mask, mask, MorphOp.Open, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
            CvInvoke.MorphologyEx(mask, mask, MorphOp.Close, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar());
        }

        private static List<Player> DetectPlayersFromMask(Mat mask, TeamType team)
        {
            using var contours = FindContours(mask);
            var players = new List<Player>();

            for (int i = 0; i < contours.Size; i++)
            {
                if (CvInvoke.ContourArea(contours[i]) > 1)  // Filter noise
                {
                    players.Add(new Player(CalculateCentroid(contours[i]), team));
                }
            }

            return players;
        }

        private static VectorOfVectorOfPoint FindContours(Mat mask)
        {
            using var hierarchy = new Mat();
            var contours = new VectorOfVectorOfPoint();

            CvInvoke.FindContours(
                mask,
                contours,
                hierarchy,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            return contours;
        }

        private static Point CalculateCentroid(VectorOfPoint contour)
        {
            var moments = CvInvoke.Moments(contour);
            return new Point(
                (int)(moments.M10 / moments.M00),
                (int)(moments.M01 / moments.M00)
            );
        }

        private static void DrawPlayers(Mat image, List<Player> players)
        {
            foreach (var player in players.Where(p => p.IsAnnotated))
            {
                var color = player.Team == TeamType.TeamA ? _colorConfig.TeamA : _colorConfig.TeamB;
                CvInvoke.Circle(image, player.Position, 10, color, -1);

                var statusColor = player.IsOffside ? new MCvScalar(0, 0, 255) : new MCvScalar(0, 255, 0);
                var status = player.IsOffside ? "OFF" : "CLN";
                DrawPlayerStatus(image, player.Position, status, statusColor);
            }
        }

        private static void DrawPlayerStatus(Mat image, Point position, string status, MCvScalar color)
        {
            CvInvoke.PutText(
                image,
                status,
                new Point(position.X - 20, position.Y - 20),
                FontFace.HersheySimplex,
                0.5,
                color,
                2);
        }

        private static void DrawPassingLines(Mat image, List<Player> players, Point ballPosition)
        {
            foreach (var player in players.Where(p => p.IsAnnotated && !p.IsOffside))
            {
                CvInvoke.ArrowedLine(
                    image,
                    ballPosition,
                    player.Position,
                    new MCvScalar(255, 0, 0),
                    2,
                    LineType.AntiAlias,
                    0,
                    0.1
                );
            }
        }

        private static void DrawBall(Mat image, Point position)
        {
            CvInvoke.Circle(image, position, 5, _colorConfig.Ball, -1);
        }
    }
}