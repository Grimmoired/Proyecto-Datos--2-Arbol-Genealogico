using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Proyecto__2_Datos_Arbol_Genealogico.Utils
{
    public static class ImageHelpers
    {
        public static CroppedBitmap CreateSquareCrop(BitmapImage source, int size)
        {
            if (source == null) return null;
            int min = Math.Min(source.PixelWidth, source.PixelHeight);
            int x = (source.PixelWidth - min) / 2;
            int y = (source.PixelHeight - min) / 2;
            var rect = new Int32Rect(x, y, min, min);
            return new CroppedBitmap(source, rect);
        }

        public static BitmapSource Resize(BitmapSource source, int width, int height)
        {
            if (source == null) return null;
            double scaleX = (double)width / source.PixelWidth;
            double scaleY = (double)height / source.PixelHeight;
            var transform = new ScaleTransform(scaleX, scaleY);
            var tb = new TransformedBitmap(source, transform);
            tb.Freeze();
            return tb;
        }
    }
}
