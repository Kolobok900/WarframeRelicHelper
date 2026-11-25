using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Graphics;

namespace WarframeRelicHelper
{
    class OcrService
    {
       
        public async Task<List<string>> RecognizeBitmapAsync(Bitmap bitmap, OcrEngine engine)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;

            using var ras = ms.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(ras);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var ocrResult = await engine.RecognizeAsync(softwareBitmap);

            var allWords = ocrResult.Lines
                .SelectMany(l => l.Words)
                .OrderBy(w => w.BoundingRect.X + w.BoundingRect.Width/2.0)
                .ToList();

            var cards = new List<List<OcrWord>>();
            foreach (var word in allWords) {
                double wordCenter = word.BoundingRect.X + word.BoundingRect.Width / 2.0;
                if (cards.Count == 0)
                {
                    cards.Add(new List<OcrWord> { word });
                    continue;
                }
                var lastCard = cards[^1];
                double lastCardCenter = lastCard.Average(w => w.BoundingRect.X + w.BoundingRect.Width / 2.0);

                var abs = Math.Abs(wordCenter - lastCardCenter);
                if(abs < 120)
                {
                    lastCard.Add(word);
                }
                else
                {
                    cards.Add(new List<OcrWord> { word });
                }
            }
            var result = new List<string>();
            foreach (var card in cards) {
                var orderedWords = card
                    .OrderBy(w => Math.Round(w.BoundingRect.Y / 10.0))
                    .ThenBy(w => w.BoundingRect.X)
                    .Select(w => w.Text);

                result.Add(string.Join(" ", orderedWords));
            }

            return result;
        }

        public Bitmap CaptureScreen()
        {
            var bmp = new Bitmap(1920, 1080);

            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(0, 0, 0, 0, bmp.Size);

            return bmp;
        }
    }
}
