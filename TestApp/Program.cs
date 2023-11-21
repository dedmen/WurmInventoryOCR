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

            ROI region = new ROI();

            Application x = new Application();
            x.Run(new ScreenshotAreaSelector(screenshot, roi =>
            {
                region = roi;
            }));


            OCRTool.DoStuff(screenshot, region);
        }
    }
}