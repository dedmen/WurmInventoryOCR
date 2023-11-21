using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IronOcr;
using IronSoftware.Drawing;
using Color = IronSoftware.Drawing.Color;

namespace WurmInventoryOCR
{
    public class OCRTool
    {
        void DoStuff()
        {
            var ocr = new IronTesseract();
​
            OcrResult res1;
            OcrResult res2;
​
            using (var ocrInput = new OcrInput())
            {
                ocrInput.AddImage("firefox_NGq4WPRBzz.png"); //, new CropRectangle(40,4, 200, 344));
​
                ocr.Configuration.WhiteListCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ(),. 0123456789";
                ocrInput.SelectTextColors(new[]
                {
                    new Color(240, 240, 240),
                    new Color(230, 230, 230),
                    new Color(220, 220, 220),
                    new Color(210, 210, 210),
                    new Color(200, 200, 200),
                    new Color(190, 190, 190),
                    new Color(180, 180, 180),
                    new Color(170, 170, 170),
                    new Color(160, 160, 160),
                    new Color(150, 150, 150),
                    new Color(140, 140, 140),
                    new Color(130, 130, 130),
                    new Color(120, 120, 120),
                }, 30);
​
                ocrInput.HighlightTextAndSaveAsImages(ocr, "D:/temp/test1.png");
​
                res1 = ocr.Read(ocrInput);
            }
        }
    }
}
