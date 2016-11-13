using System;
using System.Collections.Generic;
using System.Text;

namespace MyBand.Entities
{
    class ColorTheme
    {
        public static byte MIN_BRIGHTNESS = 0x00;
        public static byte MID_BRIGTHNESS = 0x03;
        public static byte MAX_BRIGTHNESS = 0x06;

        public static ColorTheme Black   = new ColorTheme(0, 0, 0);
        public static ColorTheme Blue    = new ColorTheme(0, 0, 6);
        public static ColorTheme Green   = new ColorTheme(0, 6, 0);
        public static ColorTheme Aqua    = new ColorTheme(0, 6, 6);
        public static ColorTheme Red     = new ColorTheme(6, 0, 0);
        public static ColorTheme Fuchsia = new ColorTheme(6, 0, 6);
        public static ColorTheme Yellow  = new ColorTheme(6, 6, 0);
        public static ColorTheme Gray    = new ColorTheme(3, 3, 3);
        public static ColorTheme White   = new ColorTheme(6, 6, 6);
        public static ColorTheme Orange  = new ColorTheme(6, 3, 0);

        private byte red;
        private byte green;
        private byte blue;

        public byte R   { get { return this.red;   } }
        public byte G   { get { return this.green; } }
        public byte B   { get { return this.blue;  } }

        public ColorTheme(byte R, byte G, byte B)
        {
            this.red   = R;
            this.green = G;
            this.blue  = B;
        }

        public static ColorTheme FromInt32(int Integer)
        {
            return new ColorTheme((byte)((Integer >> 16) & 0xFF), (byte)((Integer >> 8) & 0xFF), (byte)(Integer & 0xFF));
        }

        public Int32 ToInt32()
        {
            int ret = (red << 16) | (green << 8) | blue;
            return ret;
        }

        public override string ToString()
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            if (this.ToInt32() == 0x000606) { return loader.GetString("ColorBlue"); }
            if (this.ToInt32() == 0x040500) { return loader.GetString("ColorGreen"); }
            if (this.ToInt32() == 0x060102) { return loader.GetString("ColorRed"); }
            if (this.ToInt32() == 0x060200) { return loader.GetString("ColorOrange"); }
            return "R:"+red+"G:"+green+"B:"+blue;
        }
    }
}
