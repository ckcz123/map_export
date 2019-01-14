using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace map_export
{
    class Util
    {
        public static Color addHue(Color color, int hue)
        {
            int a = color.A;
            float h = color.GetHue();
            float s = color.GetSaturation();
            float b = color.GetBrightness();

            h += hue;
            if (h >= 360) h -= 360;

            float fMax, fMid, fMin;
            int iSextant, iMax, iMid, iMin;

            if (0.5 < b)
            {
                fMax = b - (b * s) + s;
                fMin = b + (b * s) - s;
            }
            else
            {
                fMax = b + (b * s);
                fMin = b - (b * s);
            }

            iSextant = (int)Math.Floor(h / 60f);
            if (300f <= h)
            {
                h -= 360f;
            }
            h /= 60f;
            h -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
            if (0 == iSextant % 2)
            {
                fMid = h * (fMax - fMin) + fMin;
            }
            else
            {
                fMid = fMin - h * (fMax - fMin);
            }

            iMax = Convert.ToInt32(fMax * 255);
            iMid = Convert.ToInt32(fMid * 255);
            iMin = Convert.ToInt32(fMin * 255);

            Color nColor;
            switch (iSextant)
            {
                case 1:
                    nColor = Color.FromArgb(a, iMid, iMax, iMin);
                    break;
                case 2:
                    nColor = Color.FromArgb(a, iMin, iMax, iMid);
                    break;
                case 3:
                    nColor = Color.FromArgb(a, iMin, iMid, iMax);
                    break;
                case 4:
                    nColor = Color.FromArgb(a, iMid, iMin, iMax);
                    break;
                case 5:
                    nColor = Color.FromArgb(a, iMax, iMin, iMid);
                    break;
                default:
                    nColor = Color.FromArgb(a, iMax, iMid, iMin);
                    break;
            }
            return nColor;
        }

        public static Bitmap setOpacity(Bitmap bitmap, int x, int y, int w, int h, int value)
        {
            Bitmap v = new Bitmap(w, h);
            if (value < 0) value = 0;
            if (value > 255) value = 255;
            float opacity = value / 255f;
            Graphics graphics = Graphics.FromImage(v);
            ColorMatrix matrix = new ColorMatrix();
            matrix.Matrix33 = opacity;
            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            graphics.DrawImage(bitmap, new Rectangle(0, 0, w, h), x, y, w, h, GraphicsUnit.Pixel, attributes);
            graphics.Dispose();
            return v;
        }

        public static void addHue(Bitmap bitmap, int hue)
        {
            BitmapWrapper wrapper = new BitmapWrapper(bitmap);
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    wrapper.SetPixel(new Point(i, j), addHue(wrapper.GetPixel(new Point(i, j)), hue));
                }
            }
            wrapper.UnWrapper();
        }

        public static void drawImage(Graphics g, Bitmap bitmap, int x = 0, int y = 0)
        {
            g.DrawImage(bitmap, new Rectangle(x, y, bitmap.Width, bitmap.Height), 
                0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel);
        }
    }
}
