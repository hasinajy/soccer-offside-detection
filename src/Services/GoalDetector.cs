using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;

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

        public static int DetectGoals(string imagePath)
        {
            using var image = new Mat(imagePath, ImreadModes.Color);
            using var grayImage = new Mat();
            using var edges = new Mat();
            using var dilated = new Mat();
            using var blurred = new Mat();

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

            int goalCount = 0;

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

                        // Draw bounding rectangle instead of contour
                        CvInvoke.Rectangle(
                            goalImage,
                            boundingRect,
                            new MCvScalar(255, 0, 0), // Blue color
                            2);

                        if (Math.Abs(aspectRatio - targetAspectRatio) < aspectRatioTolerance)
                        {
                            goalCount++;

                            // Draw final detected goals in green
                            CvInvoke.Rectangle(
                                goalImage,
                                boundingRect,
                                new MCvScalar(0, 255, 0), // Green color
                                3);
                        }
                    }
                }
            }

            goalImage.Save("img/7_detected_goals.jpg");

            return goalCount;
        }
    }
}