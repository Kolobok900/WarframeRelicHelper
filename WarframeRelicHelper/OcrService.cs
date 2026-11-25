using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace WarframeRelicHelper
{
    class OcrService
    {
       
        public async Task<string> RecognizeBitmapAsync(Bitmap bitmap, OcrEngine engine)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;

            using var ras = ms.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(ras);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var ocrResult = await engine.RecognizeAsync(softwareBitmap);

            return string.Join(" ", ocrResult.Lines.Select(l => l.Text));
        }
    }
}
