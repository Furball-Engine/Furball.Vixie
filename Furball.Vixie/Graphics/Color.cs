using System;

namespace Furball.Vixie.Graphics {
    public struct Color {
        public float Rf;
        public float Gf;
        public float Bf;
        public float Af;

        public byte R {
            get => (byte)(this.Rf * 255);
            set => this.Rf = value / 255f;
        }

        public byte G {
            get => (byte)(this.Gf * 255);
            set => this.Gf = value / 255f;
        }

        public byte B {
            get => (byte)(this.Bf * 255);
            set => this.Bf = value / 255f;
        }

        public byte A {
            get => (byte)(this.Af * 255);
            set => this.Af = value / 255f;
        }

        public Color(byte r, byte g, byte b, byte a = 255) {
            this.Rf = r / 255f;
            this.Gf = g / 255f;
            this.Bf = b / 255f;
            this.Af = a / 255f;
        }

        public Color(int r, int g, int b, int a = 255) {
            this.Rf = r / 255f;
            this.Gf = g / 255f;
            this.Bf = b / 255f;
            this.Af = a / 255f;
        }

        //Color values taken from https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Color.cs
        // ReSharper disable InconsistentNaming
        public static readonly Color Transparent          = new(0, 0, 0, 0);
        public static readonly Color AliceBlue            = new(240, 248, 255);
        public static readonly Color AntiqueWhite         = new(250, 235, 215);
        public static readonly Color Aqua                 = new(0, 255, 255);
        public static readonly Color Aquamarine           = new(127, 255, 212);
        public static readonly Color Azure                = new(240, 255, 255);
        public static readonly Color Beige                = new(245, 245, 220);
        public static readonly Color Bisque               = new(255, 228, 196);
        public static readonly Color Black                = new(0, 0, 0);
        public static readonly Color BlanchedAlmond       = new(255, 235, 205);
        public static readonly Color Blue                 = new(0, 0, 196);
        public static readonly Color BlueViolet           = new(138, 43, 226);
        public static readonly Color Brown                = new(165, 42, 42);
        public static readonly Color BurlyWood            = new(222, 184, 135);
        public static readonly Color CadetBlue            = new(95, 158, 160);
        public static readonly Color Chartreuse           = new(127, 255, 0);
        public static readonly Color Chocolate            = new(210, 105, 30);
        public static readonly Color Coral                = new(255, 127, 80);
        public static readonly Color CornflowerBlue       = new(100, 149, 237);
        public static readonly Color Cornsilk             = new(255, 248, 220);
        public static readonly Color Crimson              = new(220, 20, 60);
        public static readonly Color Cyan                 = new(0, 255, 255);
        public static readonly Color DarkBlue             = new(0, 0, 139);
        public static readonly Color DarkCyan             = new(0, 139, 139);
        public static readonly Color DarkGoldenrod        = new(184, 134, 11);
        public static readonly Color DarkGray             = new(169, 169, 169);
        public static readonly Color DarkGreen            = new(0, 100, 0);
        public static readonly Color DarkKhaki            = new(189, 183, 107);
        public static readonly Color DarkMagenta          = new(139, 0, 139);
        public static readonly Color DarkOliveGreen       = new(85, 107, 47);
        public static readonly Color DarkOrange           = new(255, 140, 0);
        public static readonly Color DarkOrchid           = new(153, 50, 204);
        public static readonly Color DarkRed              = new(139, 0, 0);
        public static readonly Color DarkSalmon           = new(233, 150, 122);
        public static readonly Color DarkSeaGreen         = new(143, 188, 139);
        public static readonly Color DarkSlateBlue        = new(72, 61, 139);
        public static readonly Color DarkSlateGray        = new(47, 79, 79);
        public static readonly Color DarkTurquoise        = new(0, 206, 209);
        public static readonly Color DarkViolet           = new(148, 0, 211);
        public static readonly Color DeepPink             = new(255, 20, 147);
        public static readonly Color DeepSkyBlue          = new(0, 191, 255);
        public static readonly Color DimGray              = new(105, 105, 105);
        public static readonly Color DodgerBlue           = new(30, 144, 255);
        public static readonly Color Firebrick            = new(178, 34, 34);
        public static readonly Color FloralWhite          = new(255, 250, 240);
        public static readonly Color ForestGreen          = new(34, 139, 34);
        public static readonly Color Fuchsia              = new(255, 0, 255);
        public static readonly Color Gainsboro            = new(220, 220, 220);
        public static readonly Color GhostWhite           = new(248, 248, 255);
        public static readonly Color Gold                 = new(255, 215, 0);
        public static readonly Color Goldenrod            = new(218, 165, 32);
        public static readonly Color Gray                 = new(128, 128, 128);
        public static readonly Color Green                = new(0, 128, 0);
        public static readonly Color GreenYellow          = new(173, 255, 47);
        public static readonly Color Honeydew             = new(240, 255, 240);
        public static readonly Color HotPink              = new(255, 105, 180);
        public static readonly Color IndianRed            = new(205, 92, 92);
        public static readonly Color Indigo               = new(75, 0, 130);
        public static readonly Color Ivory                = new(255, 255, 240);
        public static readonly Color Khaki                = new(240, 230, 140);
        public static readonly Color Lavender             = new(230, 230, 250);
        public static readonly Color LavenderBlush        = new(255, 240, 245);
        public static readonly Color LawnGreen            = new(24, 252, 0);
        public static readonly Color LemonChiffon         = new(255, 250, 205);
        public static readonly Color LightBlue            = new(173, 216, 230);
        public static readonly Color LightCoral           = new(240, 128, 128);
        public static readonly Color LightCyan            = new(224, 255, 255);
        public static readonly Color LightGoldenrodYellow = new(250, 250, 210);
        public static readonly Color LightGray            = new(211, 211, 211);
        public static readonly Color LightGreen           = new(144, 238, 144);
        public static readonly Color LightPink            = new(255, 182, 193);
        public static readonly Color LightSalmon          = new(255, 160, 122);
        public static readonly Color LightSeaGreen        = new(32, 178, 170);
        public static readonly Color LightSkyBlue         = new(135, 206, 250);
        public static readonly Color LightSlateGray       = new(119, 136, 153);
        public static readonly Color LightSteelBlue       = new(176, 196, 222);
        public static readonly Color LightYellow          = new(255, 255, 224);
        public static readonly Color Lime                 = new(0, 255, 0);
        public static readonly Color LimeGreen            = new(50, 205, 50);
        public static readonly Color Linen                = new(250, 240, 230);
        public static readonly Color Magenta              = new(255, 0, 255);
        public static readonly Color Maroon               = new(128, 0, 0);
        public static readonly Color MediumAquamarine     = new(102, 205, 170);
        public static readonly Color MediumBlue           = new(0, 0, 205);
        public static readonly Color MediumOrchid         = new(186, 85, 211);
        public static readonly Color MediumPurple         = new(147, 112, 219);
        public static readonly Color MediumSeaGreen       = new(60, 179, 113);
        public static readonly Color MediumSlateBlue      = new(123, 104, 238);
        public static readonly Color MediumSpringGreen    = new(0, 250, 154);
        public static readonly Color MediumTurquoise      = new(72, 209, 204);
        public static readonly Color MediumVioletRed      = new(199, 21, 133);
        public static readonly Color MidnightBlue         = new(25, 25, 112);
        public static readonly Color MintCream            = new(245, 255, 250);
        public static readonly Color MistyRose            = new(255, 228, 225);
        public static readonly Color Moccasin             = new(255, 228, 181);
        public static readonly Color MonoGameOrange       = new(231, 60, 0);
        public static readonly Color NavajoWhite          = new(255, 222, 173);
        public static readonly Color Navy                 = new(0, 0, 128);
        public static readonly Color OldLace              = new(253, 245, 230);
        public static readonly Color White                = new(255, 255, 255);
        public static readonly Color Orange               = new(255, 165, 0);
        public static readonly Color OrangeRed            = new(255, 69, 0);
        public static readonly Color Red                  = new(255, 0, 0);
        // ReSharper restore InconsistentNaming

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public bool Equals(Color color) => color.Rf == this.Rf && color.Gf == this.Gf && color.Bf == this.Bf && color.Af == this.Af;
        // ReSharper restore CompareOfFloatsByEqualityOperator

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public override bool Equals(object obj) => obj is Color color && this.Equals(color);
        // ReSharper disable CompareOfFloatsByEqualityOperator

        public override int GetHashCode() => HashCode.Combine(this.Rf, this.Gf, this.Bf, this.Af);

        public static bool operator ==(Color a, Color b) => a.Equals(b);

        public static bool operator !=(Color a, Color b) => !a.Equals(b);
    }
}
