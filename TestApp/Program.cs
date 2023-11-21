using System.Diagnostics;
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


            var screenshot = ScreenshotTool.CaptureWindowScreenshot(ScreenshotTool.FindWurmWindow());

            screenshot.Save("P:/test.png", ImageFormat.Png);


            Application x = new Application();
            x.Run(new ScreenshotAreaSelector(screenshot, roi =>
            {
                Debugger.Break();
            }));
        }
    }
}