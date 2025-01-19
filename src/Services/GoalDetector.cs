using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;
using Models;

namespace Services
{
    public class GoalDetector
    {
        // Adjusted parameters
        private static readonly double cannyThreshold1 = 30;  // Lowered threshold
        private static readonly double cannyThreshold2 = 90;  // Lowered threshold
        private static readonly double epsilon = 0.01;  // Less aggressive approximation
        private static readonly double minRectangleArea = 100;  // Lowered minimum area
        private static readonly double maxRectangleArea = 50000;  // Adjusted maximum area
        private static readonly double targetAspectRatio = 3.5;  // Adjusted for these goals
        private static readonly double aspectRatioTolerance = 1;  // Increased tolerance

        private GoalDetector() { }

        public static List<Goal> DetectGoals(string imagePath, List<Player> players)
        {
            using var image = new Mat(imagePath, ImreadModes.Color);
            using var grayImage = new Mat();
            using var edges = new Mat();
            using var dilated = new Mat();
            using var blurred = new Mat();

            // Get the furthest players (top and bottom)
            var orderedPlayers = players.OrderBy(p => p.Position.Y).ToList();
            var topPlayer = orderedPlayers.First();
            var bottomPlayer = orderedPlayers.Last();

            // Convert to grayscale
            CvInvoke.CvtColor(image, grayImage, ColorConversion.Bgr2Gray);
            grayImage.Save("img/2_grayscale.jpg");

            // Blur
            CvInvoke.GaussianBlur(grayImage, blurred, new Size(3, 3), 0);
            blurred.Save("img/3_blurred.jpg");

            // Edge detection
            CvInvoke.Canny(blurred, edges, cannyThreshold1, cannyThreshold2);
            edges.Save("img/4_edges.jpg");

            // Dilate edges
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2, 2), new Point(-1, -1));
            CvInvoke.Dilate(edges, dilated, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
            dilated.Save("img/5_dilated.jpg");

            using var contours = new VectorOfVectorOfPoint();
            using var goalImage = image.Clone();

            CvInvoke.FindContours(
                dilated,
                contours,
                null,
                RetrType.External,
                ChainApproxMethod.ChainApproxSimple);

            var detectedGoals = new List<Goal>();

            for (int i = 0; i < contours.Size; i++)
            {
                using var contour = contours[i];
                double perimeter = CvInvoke.ArcLength(contour, true);
                using var approx = new VectorOfPoint();

                CvInvoke.ApproxPolyDP(
                    contour,
                    approx,
                    epsilon * perimeter,
                    true);

                if (approx.Size >= 3 && approx.Size <= 8)
                {
                    double area = CvInvoke.ContourArea(approx);

                    if (area > minRectangleArea && area < maxRectangleArea)
                    {
                        Rectangle boundingRect = CvInvoke.BoundingRectangle(approx);
                        double aspectRatio = (double)boundingRect.Width / boundingRect.Height;

                        // Draw initial detection in blue
                        CvInvoke.Rectangle(
                            goalImage,
                            boundingRect,
                            new MCvScalar(255, 0, 0),
                            2);

                        if (Math.Abs(aspectRatio - targetAspectRatio) < aspectRatioTolerance)
                        {
                            // Get goal center Y position
                            int goalCenterY = boundingRect.Y + boundingRect.Height / 2;

                            // Determine which team's goal this is based on closest player
                            // The goal belongs to the opposite team of the closest player
                            var team = Math.Abs(goalCenterY - topPlayer.Position.Y) < Math.Abs(goalCenterY - bottomPlayer.Position.Y)
                                ? topPlayer.Team
                                : bottomPlayer.Team;

                            var goal = new Goal(boundingRect, team);
                            detectedGoals.Add(goal);

                            // Draw final detection in green with team indicator
                            CvInvoke.Rectangle(
                                goalImage,
                                boundingRect,
                                new MCvScalar(0, 255, 0),
                                3);

                            CvInvoke.PutText(
                                goalImage,
                                team.ToString(),
                                new Point(boundingRect.X, boundingRect.Y - 10),
                                FontFace.HersheySimplex,
                                1,
                                new MCvScalar(0, 255, 0),
                                2);
                        }
                    }
                }
            }

            goalImage.Save("img/7_detected_goals.jpg");

            return detectedGoals;
        }
    }
}