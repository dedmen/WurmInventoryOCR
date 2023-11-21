using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IronOcr;
using IronSoftware.Drawing;
using Color = IronSoftware.Drawing.Color;
using Image = System.Drawing.Image;
using RectangleF = System.Drawing.RectangleF;

namespace WurmInventoryOCR
{
    public class OCRTool
    {
        public static Bitmap CropImage(Image source, ROI section)
        {
            var bitmap = new Bitmap((int)section.Width, (int)section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(source,
                    new RectangleF(0,0, (float)section.Width, (float)section.Height),
                    new RectangleF((float)section.X, (float)section.Y, (float)section.Width, (float)section.Height),
                    GraphicsUnit.Pixel
                    );
                return bitmap;
            }
        }

        public static void DoStuff(Image image, ROI region)
        {
            var ocr = new IronTesseract();

            OcrResult res1;

            using (var ocrInput = new OcrInput())
            {
                image = CropImage(image, region);
                image.Save("P:/test2.png", ImageFormat.Png);


                ocrInput.AddImage(image); // , new CropRectangle((int)region.X, (int)region.Y, (int)region.Width, (int)region.Height)

                ocr.Configuration.WhiteListCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ(),. 0123456789";
                //ocrInput.SelectTextColors(new[]
                //{
                //    new Color(240, 240, 240),
                //    new Color(230, 230, 230),
                //    new Color(220, 220, 220),
                //    new Color(210, 210, 210),
                //    new Color(200, 200, 200),
                //    new Color(190, 190, 190),
                //    new Color(180, 180, 180),
                //    new Color(170, 170, 170),
                //    new Color(160, 160, 160),
                //    new Color(150, 150, 150),
                //    new Color(140, 140, 140),
                //    new Color(130, 130, 130),
                //    new Color(120, 120, 120),
                //}, 30);

                ocrInput.HighlightTextAndSaveAsImages(ocr, "P:/test1.png");

                res1 = ocr.Read(ocrInput);
                Debugger.Break();
            }
        }
    }
}
