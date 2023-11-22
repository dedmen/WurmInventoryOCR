using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using IronOcr;
using IronSoftware.Drawing;
using static IronOcr.OcrResult;
using Color = IronSoftware.Drawing.Color;
using Image = System.Drawing.Image;
using RectangleF = System.Drawing.RectangleF;

namespace WurmInventoryOCR
{
    public struct WurmItemLine
    {
        public string ItemName = "";
        public string ItemMaterial = "";
        public uint ItemCount = 0;
        public float ItemQuality = 0;

        // Confidence between 0-100, of the Name part
        public double NameConfidence = 0;

        // Confidence between 0-100, of the Count part
        public double CountConfidence = 0;

        // Confidence between 0-100, of the Material part
        public double MaterialConfidence = 100;  // If we didn't find material, its probably an item with no material so it'll be 100% correct

        // Confidence between 0-100, of the Quality number
        public double QualityConfidence = 0;

        public WurmItemLine()
        {
        }
    }

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

        private static readonly Regex _QualityMatch = new Regex("\\d*\\.\\d\\d", RegexOptions.Compiled);
        private static readonly Regex _CountMatch = new Regex("\\((\\d*)x\\)", RegexOptions.Compiled);
        private static readonly Regex _MaterialMatch = new Regex("(.*), (.*)", RegexOptions.Compiled);
        private static readonly Regex _NameStartCleanup = new Regex("^([a-zA-Z0-9])[\\w,.]{0,2} (\\w+)", RegexOptions.Compiled);

        // OcrResult.Line copied here, because I need to make copies and modifications and it doesn't let me
        public class Line : OcrResult.OcrResultTextElement
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public CropRectangle Location { get; set; }
            
            public OcrResult.TextFlow TextDirection { get; set; }
            public string Text { get; set; }
            public virtual double Confidence { get; set; }
            

            public OcrResult.Block Block;
            public OcrResult.Paragraph Paragraph;
            public double BaselineAngle;
            public double BaselineOffset;
            public OcrResult.Word[] Words { get; set; }
            public OcrResult.Character[] Characters { get; set; }
            public int LineNumber { get; set; }

            public static Line FromOcrLine(OcrResult.Line src)
            {
                return new Line()
                {
                    Width = src.Width,
                    Height = src.Height,
                    X = src.X,
                    Y = src.Y,
                    Location = src.Location,
                    TextDirection = src.TextDirection,
                    Text = src.Text,
                    Confidence = src.Confidence,
                    Block = src.Block,
                    Paragraph = src.Paragraph,
                    BaselineAngle = src.BaselineAngle,
                    BaselineOffset = src.BaselineOffset,
                    Words = src.Words,
                    Characters = src.Characters,
                    LineNumber = src.LineNumber
                };
            }

            public Line Clone()
            {
                return new Line()
                {
                    Width = Width,
                    Height = Height,
                    X = X,
                    Y = Y,
                    Location = Location,
                    TextDirection = TextDirection,
                    Text = Text,
                    Confidence = Confidence,
                    Block = Block,
                    Paragraph = Paragraph,
                    BaselineAngle = BaselineAngle,
                    BaselineOffset = BaselineOffset,
                    Words = Words,
                    Characters = Characters,
                    LineNumber = LineNumber
                };
            }

            public Line MergeWith(Line secondLine)
            {
                // Assume second line is on same height as we are

                var result = Clone();
                result.Width = secondLine.X + secondLine.Width - X;
                result.Location = new CropRectangle(X, Y, result.Width, Height);
                result.Text = Text + secondLine.Text;
                result.Confidence = Confidence * secondLine.Confidence;
                result.Words = Words.Concat(secondLine.Words).ToArray();
                result.Characters = Characters.Concat(secondLine.Characters).ToArray();

                return result;
            }

            public (Line, Line) SplitAtWord(OcrResult.Word word)
            {
                Line line1 = Clone();
                Line line2 = Clone();
                // Left line
                {
                    line1.Width = word.X - line1.X;
                    line1.Location = new CropRectangle(line1.X, line1.Y, line1.Width, line1.Height);

                    // Word string should only appear once
                    if (line1.Text.CountOccurencesOf(word.Text) != 1)
                    {
                        line1.Confidence = 0;
                    }

                    line1.Text = Text.Substring(0, Text.IndexOf(word.Text, StringComparison.Ordinal));
                    line1.Words = Words.TakeWhile(x => x != word).ToArray();
                    line1.Characters = Characters.TakeWhile(x => x != word.Characters[0]).ToArray();
                }

                // Right line
                {
                    line2.X = word.X;
                    line2.Width = X + Width - line2.X;
                    line2.Location = new CropRectangle(line2.X, line2.Y, line2.Width, line2.Height);

                    // Word string should only appear once
                    if (line2.Text.CountOccurencesOf(word.Text) != 1)
                    {
                        line2.Confidence = 0;
                    }

                    line2.Text = Text.Substring(Text.IndexOf(word.Text, StringComparison.Ordinal));
                    line2.Words = Words.SkipWhile(x => x != word).ToArray();
                    line2.Characters = Characters.SkipWhile(x => x != word.Characters[0]).ToArray();
                }

                return (line1, line2);
            }

        }

        public static List<WurmItemLine> DoStuff(Image image, ROI region)
        {
            var ocr = new IronTesseract();

            using var ocrInput = new OcrInput();

            image = CropImage(image, region);
           // image.Save("P:/test2.png", ImageFormat.Png);

            ocrInput.AddImage(image);

            ocr.Configuration.WhiteListCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ(),. 0123456789-\"";
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

            //ocrInput.HighlightTextAndSaveAsImages(ocr, "P:/test1.png");

            var res1 = ocr.Read(ocrInput);




            // cleanup lines
            // We sometimes get missinterpretation of word boundary, reading a "bulk storage" line as a "Bi bi" and a "ulk storage" line.
            // When two consecutive lines are on same height, merge them
            var lines = res1.Lines.Select(Line.FromOcrLine);

            var mergedLines = lines.Merge(
                (x, y) => Math.Abs(x.Y - y.Y) < 4, // Less than 4 pixel apart counts as same height
                (x, y) => x.MergeWith(y)
            ).ToArray();

            if (mergedLines.Length == 0)
            {
                return new List<WurmItemLine>();
            }

            // Sometimes the items table is scanned with the columns separated, first all item lines, then all Quality lines
            // Sometimes when scanning whole inventory, it doesn't seperate columns and takes the whole row as one line
            // We need to split out the itemname&count part, and the QL part
            // I have not yet seen it mix the two. If it would intermittently split a line into two, the above merger would catch that so hope that doesn't happen

            // We just detect what the line mode is, if the first line contains a QL, then everything will be full rows

            List<Line> names = new List<Line>();
            List<Line> qualities = new List<Line>();
            var result = new List<WurmItemLine>();

            // false == split into blocks by column
            bool fullRowMode = mergedLines.First().Words.Any(firstLineWord => _QualityMatch.IsMatch(firstLineWord.Text));

            if (fullRowMode)
            {
                Console.WriteLine("FullRow Mode!");
                // Split the full row lines apart into two parts for name and QL
                foreach (var mergedLine in mergedLines)
                {
                    var splitWord = mergedLine.Words.FirstOrDefault(x => _QualityMatch.IsMatch(x.Text));
                    if (splitWord == null)
                    {
                        // We completely failed here, we expect fullRow but didn't get one.
                        // We don't want to loose any data so just throw it out as zero confidence

                        result.Add(new WurmItemLine()
                        {
                            ItemName = mergedLine.Text,
                            NameConfidence = 0,
                            MaterialConfidence = 0,
                            CountConfidence = 0,
                            QualityConfidence = 0
                        });
                        continue;
                    }

                    var x = mergedLine.SplitAtWord(splitWord);
                    names.Add(x.Item1);
                    qualities.Add(x.Item2);
                }
            }
            else
            {
                // There will be two blocks of lines. The first block will all start on the left half of the image
                // The second block, the QL's, are always right of center
                // If whole inventory was selected, there might be more blocks for damage/volume, we don't care about them, they'll just be ignored in the zip

                names = mergedLines.Where(x => x.X < image.Width / 2).ToList();
                qualities = mergedLines.Where(x => x.X > image.Width / 2).ToList();
            }

            var zippedLines = names.Zip(qualities, (name, quality) =>
            {
                var lineResult = new WurmItemLine
                {
                    ItemName = name.Text,
                    NameConfidence = name.Confidence
                };


                /// Name cleanup

                // Some very basic name cleanup, when a icon gets missinterpreted
                {
                    lineResult.ItemName = lineResult.ItemName.TrimStart(' ', ',', '.', '-');
                    // Really needs something more sophisticated. But this catches the bulk already

                    // Item names don't start with uppercase letters. If its uppercase letter at start, followed by zero or one more letters and a space, its a hallucinated icon
                    lineResult.ItemName = _NameStartCleanup.Replace(lineResult.ItemName, "$2");
                }

                /// Quality

                // We use word0 instead of quality.Text, because in fullRow mode, the quality line could contain 3 numbers QL,Damage,Volume. We don't want to accidentally catch the second word
                var qualityMatch = _QualityMatch.Match(quality.Words[0].Text); 
                if (qualityMatch.Success)
                {
                    bool qualityParsed = float.TryParse(qualityMatch.Value, CultureInfo.InvariantCulture, out var qualityFloat);

                    if (qualityParsed)
                    {
                        lineResult.ItemQuality = qualityFloat;
                        lineResult.QualityConfidence = quality.Confidence;
                    }
                }

                /// Count

                var countMatch = _CountMatch.Match(name.Text);
                if (countMatch.Success)
                {
                    bool countParsed = uint.TryParse(countMatch.Groups[1].Value, CultureInfo.InvariantCulture, out var count); //#TODO check

                    if (countParsed)
                    {
                        // Okey we got the count, cut it off from the name

                        // After the count should only be whitespace, if there is something else we did a parsing error. in FullRow mode this can also happen when the QL incorrectly gets merged into name

                        string remainingText = lineResult.ItemName.Substring(lineResult.ItemName.IndexOf(countMatch.Value, StringComparison.Ordinal) + countMatch.Value.Length);
                        remainingText = remainingText.Trim(' ');

                        if (remainingText.Length > 0)
                        {
                            lineResult.NameConfidence = 0;
                            lineResult.QualityConfidence = 0;
                        }

                        lineResult.ItemName = lineResult.ItemName.Substring(0, lineResult.ItemName.IndexOf(countMatch.Value, StringComparison.Ordinal)).Trim(' ');

                        lineResult.ItemCount = count;
                        lineResult.CountConfidence = name.Confidence;
                    }
                }
                else
                {
                    // No count found is also fine
                    lineResult.ItemCount = 1;
                    lineResult.CountConfidence = 100;
                }

                /// Material

                var materialMatch = _MaterialMatch.Match(lineResult.ItemName);
                if (materialMatch.Success)
                {
                    //#TODO check if material is found in list of possible materials, and don't use it if it isn't

                    lineResult.ItemName = materialMatch.Groups[1].Value;
                    lineResult.ItemMaterial = materialMatch.Groups[2].Value;
                    lineResult.MaterialConfidence = name.Confidence;
                }

                return lineResult;
            });

            result.AddRange(zippedLines);

            return result.ToList();
        }
    }
}
