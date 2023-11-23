using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using WurmInventoryOCR;

namespace TestApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");


            //var screenshot = ScreenshotTool.CaptureWindowScreenshot(ScreenshotTool.FindWurmWindow());
            //screenshot.Save("P:/test.png", ImageFormat.Png);

            var screenshot = Bitmap.FromFile("D:/dev/WurmInventoryOCR/image.png");
            //var screenshot = Bitmap.FromFile("P:/test2.png");

            var inventoryRects = InventoryFinderTest.Test(screenshot);

            Application x = new Application();

            List<ROI> predefinedROIs = new List<ROI>();
            foreach (var inventoryRect in inventoryRects)
            {
                predefinedROIs.Add(new ROI() { X = inventoryRect.X, Y = inventoryRect.Y, Width = inventoryRect.Width, Height = inventoryRect.Height, ScaleFactor = 1, Shape = Shapes.Square });
            }


            ROI region = new ROI() { X = 0, Y = 0, Width = screenshot.Width, Height = screenshot.Height, ScaleFactor = 1, Shape = Shapes.Square };

            x.Run(new ScreenshotAreaSelector(screenshot, predefinedROIs, roi =>
            {
                region = roi;
            }));

            var results = OCRTool.DoStuff(screenshot, region);

            Console.WriteLine($"|{"Name",37}|{"Material",22}|{"Count",11}|{"QL",13}|");

            foreach (var wurmItemLine in results)
            {
                Console.WriteLine($"|{wurmItemLine.ItemName,30}({wurmItemLine.NameConfidence / 100,5:P0})|{wurmItemLine.ItemMaterial,15}({wurmItemLine.MaterialConfidence / 100,5:P0})|{wurmItemLine.ItemCount,4}({wurmItemLine.CountConfidence / 100,5:P0})|{wurmItemLine.ItemQuality,6:F}({wurmItemLine.QualityConfidence / 100,5:P0})|");
            }
        }
    }
}