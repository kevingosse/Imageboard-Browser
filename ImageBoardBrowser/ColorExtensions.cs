using System;
using System.Diagnostics;
using System.Windows.Media;

namespace ImageBoardBrowser
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns a pastel shade of the color
        /// </summary>
        /// <param name="source">Source  color</param>
        /// <returns></returns>
        public static Color GetPastelShade(this Color source, int? seed = null)
        {
            return (GenerateColor(source, true, new Hsb { H = 0, S = 0.2d, B = 255 }, new Hsb { H = 360, S = 0.5d, B = 255 }, seed));
        }

        /// <summary>
        /// Returns a random color
        /// </summary>
        /// <param name="source">Ignored(Use RandomShade to get a shade of given color)</param>
        /// <returns></returns>
        public static Color GetRandom(this Color source, int? seed = null)
        {
            return (GenerateColor(source, false, new Hsb { H = 0, S = 0, B = 0 }, new Hsb { H = 360, S = 1, B = 255 }, seed));
        }
        
        /// <summary>
        /// Returns a random color within a brightness boundry
        /// </summary>
        /// <param name="source">Ignored (Use GetRandomShade to get a random shade of the color)</param>
        /// <param name="minBrightness">A valued from 0.0 to 1.0, 0 is darkest and 1 is lightest</param>
        /// <param name="maxBrightness">A valued from 0.0 to 1.0</param>
        /// <returns></returns>
        public static Color GetRandom(this Color source, double minBrightness, double maxBrightness, int? seed = null)
        {
            if (minBrightness >= 0 && maxBrightness <= 1)
            {
                return (GenerateColor(source, false, new Hsb { H = 0, S = 1 * minBrightness, B = 255 }, new Hsb { H = 360, S = 1 * maxBrightness, B = 255 }, seed));
            }
            
            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Returns a random shade of the color
        /// </summary>
        /// <param name="source">Base color for the returned shade</param>
        /// <returns></returns>
        public static Color GetRandomShade(this Color source, int? seed = null)
        {
            return (GenerateColor(source, true, new Hsb { H = 0, S = 1, B = 0 }, new Hsb { H = 360, S = 1, B = 255 }, seed));
        }

        /// <summary>
        /// Returns a random color within a brightness boundry
        /// </summary>
        /// <param name="source">Base color for the returned shade</param>
        /// <param name="minBrightness">A valued from 0.0 to 1.0, 0 is brightest and 1 is lightest</param>
        /// <param name="maxBrightness">A valued from 0.0 to 1.0</param>
        /// <returns></returns>
        public static Color GetRandomShade(this Color source, double minBrightness, double maxBrightness)
        {
            if (minBrightness >= 0 && maxBrightness <= 1)
            {
                return (GenerateColor(source, true, new Hsb { H = 0, S = 1 * minBrightness, B = 255 }, new Hsb { H = 360, S = 1 * maxBrightness, B = 255 }));
            }
            
            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// Process parameters and returns a color
        /// </summary>
        /// <param name="source">Color source</param>
        /// <param name="isaShadeOfSource">Should source be used to generate the new color</param>
        /// <param name="min">Minimum range for HSB</param>
        /// <param name="max">Maximum range for HSB</param>
        /// <returns></returns>
        private static Color GenerateColor(Color source, bool isaShadeOfSource, Hsb min, Hsb max, int? seed = null)
        {
            var randomizer = seed != null ? new Random(seed.Value) : new Random();

            Hsb hsbValues = ConvertToHsb(new Rgb { R = source.R, G = source.G, B = source.B });
            double hDouble = randomizer.NextDouble();
            double bDouble = randomizer.NextDouble();

            if (max.B - min.B == 0)
            {
                bDouble = 0; //do not change Brightness
            }

            if (isaShadeOfSource)
            {
                min.H = hsbValues.H;
                max.H = hsbValues.H;
                hDouble = 0;
            }
            hsbValues = new Hsb
            {
                H = Convert.ToDouble(randomizer.Next(Convert.ToInt32(min.H), Convert.ToInt32(max.H))) + hDouble,
                S = Convert.ToDouble((randomizer.Next(Convert.ToInt32(min.S * 100), Convert.ToInt32(max.S * 100))) / 100d),
                B = Convert.ToDouble(randomizer.Next(Convert.ToInt32(min.B), Convert.ToInt32(max.B))) + bDouble
            };

            Debug.WriteLine("H:{0} | S:{1} | B:{2} [Min_S:{3} | Max_S{4}]", hsbValues.H, hsbValues.S, hsbValues.B, min.S, max.S);
            Rgb rgbvalues = ConvertToRgb(hsbValues);
            return new Color { A = source.A, R = (byte)rgbvalues.R, G = (byte)rgbvalues.G, B = (byte)rgbvalues.B };
        }

        private static Rgb ConvertToRgb(Hsb hsb)
        {
            // By: <a href="http://blogs.msdn.com/b/codefx/archive/2012/02/09/create-a-color-picker-for-windows-phone.aspx" title="MSDN" target="_blank">Yi-Lun Luo</a>
            double chroma = hsb.S * hsb.B;
            double hue2 = hsb.H / 60;
            double x = chroma * (1 - Math.Abs(hue2 % 2 - 1));
            double r1 = 0d;
            double g1 = 0d;
            double b1 = 0d;
            if (hue2 >= 0 && hue2 < 1)
            {
                r1 = chroma;
                g1 = x;
            }
            else if (hue2 >= 1 && hue2 < 2)
            {
                r1 = x;
                g1 = chroma;
            }
            else if (hue2 >= 2 && hue2 < 3)
            {
                g1 = chroma;
                b1 = x;
            }
            else if (hue2 >= 3 && hue2 < 4)
            {
                g1 = x;
                b1 = chroma;
            }
            else if (hue2 >= 4 && hue2 < 5)
            {
                r1 = x;
                b1 = chroma;
            }
            else if (hue2 >= 5 && hue2 <= 6)
            {
                r1 = chroma;
                b1 = x;
            }
            double m = hsb.B - chroma;
            return new Rgb()
            {
                R = r1 + m,
                G = g1 + m,
                B = b1 + m
            };
        }

        private static Hsb ConvertToHsb(Rgb rgb)
        {
            // By: <a href="http://blogs.msdn.com/b/codefx/archive/2012/02/09/create-a-color-picker-for-windows-phone.aspx" title="MSDN" target="_blank">Yi-Lun Luo</a>
            double r = rgb.R;
            double g = rgb.G;
            double b = rgb.B;

            double max = Max(r, g, b);
            double min = Min(r, g, b);
            double chroma = max - min;
            double hue2 = 0d;

            if (chroma != 0)
            {
                if (max == r)
                {
                    hue2 = (g - b) / chroma;
                }
                else if (max == g)
                {
                    hue2 = (b - r) / chroma + 2;
                }
                else
                {
                    hue2 = (r - g) / chroma + 4;
                }
            }
            double hue = hue2 * 60;

            if (hue < 0)
            {
                hue += 360;
            }
            
            double brightness = max;
            double saturation = 0;
            
            if (chroma != 0)
            {
                saturation = chroma / brightness;
            }
            
            return new Hsb
            {
                H = hue,
                S = saturation,
                B = brightness
            };
        }

        private static double Max(double d1, double d2, double d3)
        {
            return Math.Max(d1 > d2 ? d1 : d2, d3);
        }

        private static double Min(double d1, double d2, double d3)
        {
            return Math.Min(d1 < d2 ? d1 : d2, d3);
        }

        private struct Rgb
        {
            internal double R;
            internal double G;
            internal double B;
        }

        private struct Hsb
        {
            internal double H;
            internal double S;
            internal double B;
        }
    }
}
