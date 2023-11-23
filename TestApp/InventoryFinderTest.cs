using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Windows.Media;
using BitMiracle.LibTiff.Classic;


#if !__IOS__
using Emgu.CV.Cuda;
#endif
using Emgu.CV.XFeatures2D;
using WurmInventoryOCR;
using Color = System.Drawing.Color;

namespace TestApp
{
    public static class DrawMatches
    {
        public static void FindMatch(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
        {
            int k = 2;
            double uniquenessThreshold = 0.8;
            double hessianThresh = 300;

            Stopwatch watch;
            homography = null;

            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();

#if !__IOS__
            if (CudaInvoke.HasCuda)
            {
                var surfCuda = new Emgu.CV.Features2D.ORB();
                using GpuMat gpuModelImage = new GpuMat(modelImage);

               surfCuda.DetectRaw(gpuModelImage, modelKeyPoints);
               GpuMat gpuModelDescriptors = new GpuMat();
               surfCuda.Compute(gpuModelImage, modelKeyPoints, gpuModelDescriptors);

                using CudaBFMatcher matcher = new CudaBFMatcher(DistanceType.L2);
                watch = Stopwatch.StartNew();

                // extract features from the observed image
                //using (GpuMat gpuObservedImage = new GpuMat(observedImage))
                //using (GpuMat gpuObservedKeyPoints = surfCuda.DetectKeyPointsRaw(gpuObservedImage, null))
                //using (GpuMat gpuObservedDescriptors = surfCuda.ComputeDescriptorsRaw(gpuObservedImage, null, gpuObservedKeyPoints))
                    //using (GpuMat tmp = new GpuMat())
                    //using (Stream stream = new Stream())
                {

                    surfCuda.DetectRaw(gpuModelImage, observedKeyPoints);
                    GpuMat gpuObservedDescriptors = new GpuMat();
                    surfCuda.Compute(gpuModelImage, observedKeyPoints, gpuObservedDescriptors);


                    matcher.KnnMatch(gpuObservedDescriptors, gpuModelDescriptors, matches, k);


                    mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
                            matches, mask, 1.5, 20);
                        if (nonZeroCount >= 4)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
                                observedKeyPoints, matches, mask, 2);
                    }
                }
                watch.Stop();
            }
            else
#endif
            {
                using UMat uModelImage = modelImage.GetUMat(AccessType.Read);
                using UMat uObservedImage = observedImage.GetUMat(AccessType.Read);

                //SURF surfCPU = new SURF(hessianThresh);
                SIFT surfCPU = new SIFT();
                //var surfCPU = new Emgu.CV.Features2D.MSER();

                //extract features from the object image
                UMat modelDescriptors = new UMat();
                surfCPU.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);

                watch = Stopwatch.StartNew();

                // extract features from the observed image
                UMat observedDescriptors = new UMat();
                surfCPU.DetectAndCompute(uObservedImage, null, observedKeyPoints, observedDescriptors, false);
                BFMatcher matcher = new BFMatcher(DistanceType.L2);
                matcher.Add(modelDescriptors);

                matcher.KnnMatch(observedDescriptors, matches, k, null);
                mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                mask.SetTo(new MCvScalar(255));
                Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                int nonZeroCount = CvInvoke.CountNonZero(mask);
                if (nonZeroCount >= 4)
                {
                    nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
                        matches, mask, 1.5, 20);
                    if (nonZeroCount >= 4)
                        homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
                            observedKeyPoints, matches, mask, 2);
                }

                watch.Stop();
            }
            matchTime = watch.ElapsedMilliseconds;
        }

        /*
        public static void FindMatchBEELID(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
        {
            int k = 2;
            double uniquenessThreshold = 0.8;
            double hessianThresh = 300;

            Stopwatch watch;
            homography = null;

            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();

            {
                using (UMat uModelImage = modelImage.GetUMat(AccessType.Read))
                using (UMat uObservedImage = observedImage.GetUMat(AccessType.Read))
                {
                    var surfCPU = new Emgu.CV.XFeatures2D.BEBLID(1, BEBLID.BeblidSize.BitSize256);


                    Matrix<int> indices;

                    BriefDescriptorExtractor descriptor = new BriefDescriptorExtractor();

                    Matrix<byte> mask;
                    Matrix<Byte> modelDescriptors = new Matrix<byte>();

                    //extract features from the object image
                    surfCPU.DetectRaw(modelImage, modelKeyPoints, null);
                    descriptor.Compute(modelImage, modelKeyPoints, modelDescriptors);

                    // extract features from the observed image
                    observedKeyPoints = surfCPU.DetectRaw(observedImage, null);
                    Matrix<Byte> observedDescriptors = descriptor.ComputeDescriptorsRaw(observedImage, null, observedKeyPoints);
                    var matcher = new BFMatcher(DistanceType.L2);
                    matcher.Add(modelDescriptors);

                    indices = new Matrix<int>(observedDescriptors.Rows, k);
                    using (Matrix<float> dist = new Matrix<float>(observedDescriptors.Rows, k))
                    {
                        matcher.KnnMatch(observedDescriptors, indices, dist, k, null);
                        mask = new Matrix<byte>(dist.Rows, 1);
                        mask.SetValue(255);
                        Features2DToolbox.VoteForUniqueness(dist, uniquenessThreshold, mask);
                    }

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, indices, mask, 1.5, 20);
                        if (nonZeroCount >= 4)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(
                                modelKeyPoints, observedKeyPoints, indices, mask, 2);
                    }

                    //Draw the matched keypoints
                    Image<Bgr, Byte> result = Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
                        indices, new Bgr(255, 255, 255), new Bgr(255, 255, 255), mask, Features2DToolbox.KeypointDrawType.DEFAULT);

                    watch.Stop();
                }
            }
            matchTime = watch.ElapsedMilliseconds;
        }
        */
        /// <summary>
        /// Draw the model image and observed image, the matched features and homography projection.
        /// </summary>
        /// <param name="modelImage">The model image</param>
        /// <param name="observedImage">The observed image</param>
        /// <param name="matchTime">The output total time for computing the homography matrix.</param>
        /// <returns>The model image and observed image, the matched features and homography projection.</returns>
        public static Mat Draw(Mat modelImage, Mat observedImage, out long matchTime)
        {
            Mat homography;
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            using VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            Mat mask;
            FindMatch(modelImage, observedImage, out matchTime, out modelKeyPoints, out observedKeyPoints, matches, out mask, out homography);

            //Draw the matched keypoints
            Mat result = new Mat();

            Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
                matches, result, new MCvScalar(0, 0, 255, 255), new MCvScalar(255, 255, 255, 0), mask, Features2DToolbox.KeypointDrawType.NotDrawSinglePoints);

            #region draw the projected region on the image

            if (homography != null)
            {
                //draw a rectangle along the projected model
                Rectangle rect = new Rectangle(Point.Empty, modelImage.Size);
                PointF[] pts = new PointF[]
                {
                    new PointF(rect.Left, rect.Bottom),
                    new PointF(rect.Right, rect.Bottom),
                    new PointF(rect.Right, rect.Top),
                    new PointF(rect.Left, rect.Top)
                };
                pts = CvInvoke.PerspectiveTransform(pts, homography);

                Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
                using VectorOfPoint vp = new VectorOfPoint(points);
                CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
            }

            #endregion

            return result;
        }
    }

    public class InventoryFinderTest
    {
        private static Mat GetMatFromSDImage(System.Drawing.Image image)
        {
            int stride = 0;
            Bitmap bmp = new Bitmap(image);

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            System.Drawing.Imaging.PixelFormat pf = bmp.PixelFormat;
            if (pf == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                stride = bmp.Width * 4;
            }
            else
            {
                stride = bmp.Width * 3;
            }

            Image<Bgra, byte> cvImage = new Image<Bgra, byte>(bmp.Width, bmp.Height, stride, (IntPtr)bmpData.Scan0);

            bmp.UnlockBits(bmpData);

            return cvImage.Mat;
        }

        public static List<Rectangle> Test(Image baseImage)
        {

            Mat img = GetMatFromSDImage(baseImage); //CvInvoke.Imread("D:/dev/WurmInventoryOCR/image.png", ImreadModes.AnyColor);
            var imgI = img.ToImage<Emgu.CV.Structure.Bgra, byte>();
            Mat imgPattern = CvInvoke.Imread("D:/dev/WurmInventoryOCR/pattern.png", ImreadModes.AnyColor);
            var imgPatternI = imgPattern.ToImage<Emgu.CV.Structure.Bgra, byte>();

            Mat imgPattern2 = CvInvoke.Imread("D:/dev/WurmInventoryOCR/pattern2.png", ImreadModes.AnyColor);
            var imgPattern2I = imgPattern2.ToImage<Emgu.CV.Structure.Bgra, byte>();

            //VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch();
            //DrawMatches.FindMatch(imgPattern, img, out var matchTime, out var modelKeyPoints, out var observedKeyPoints, matches, out var mask, out var homography);


            var res = img.Clone();// DrawMatches.Draw(imgPatternI.Mat, imgI.Mat, out var matchTime);

            var TRCorners = MatchTempl(imgI, imgPatternI, res);
            var BLCorners = MatchTempl(imgI, imgPattern2I, res);

            imgI.Save("P:/draw1.png");
            //img.Save("P:/img.png");
            res.Save("P:/draw.png");


            return BLCorners.Select(bl =>
            {
                var closestTR = TRCorners.Where(x => x.X > bl.X && x.Y < bl.Y).MinBy(x => x.Location.DistanceTo(bl.Location));

                // Offsets to move the rectangle into inner area of inventory screen. That depends on our pattern images
                int XOffset = 7;
                var YOffset = 36;
                int WidthOffset = 26;
                int HeightOffset = -36;

                return new Rectangle(bl.X + XOffset, closestTR.Y + YOffset, closestTR.X - bl.X + WidthOffset, bl.Y - closestTR.Y + HeightOffset);
            }).ToList();
        }

        private static List<Rectangle> MatchTempl(Image<Bgra, byte> imgI, Image<Bgra, byte> imgPattern2I, Mat res)
        {
            List<Rectangle> result = new List<Rectangle>();

            var res2 = imgI.MatchTemplate(imgPattern2I, TemplateMatchingType.CcoeffNormed);
            float[,,] matches = res2.Data;
            for (int x = 0; x < matches.GetLength(1); x++)
            {
                for (int y = 0; y < matches.GetLength(0); y++)
                {
                    double matchScore = matches[y, x, 0];
                    if (matchScore > 0.9)
                    {
                        Rectangle rect = new Rectangle(new Point(x, y), new Size(imgPattern2I.Width, imgPattern2I.Height));
                        //imgSource.Draw(rect, new Rgb(Color.Blue), 1);
                        result.Add(rect);
                        CvInvoke.Rectangle(res, rect, new Bgra(0,0,255,255).MCvScalar);
                    }
                }
            }

            return result;
        }
    }
}
