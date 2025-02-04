using System;
using System.Linq;

namespace FDK.ExtensionMethods
{
    public static class StringExtensions
    {
        public static int[] CommaSeparatedStringToInt32Array(this string str)
        {
            return str.Split(',').Select(int.Parse).ToArray();
        }
        public static double ToDouble(this string str, double min, double max, double def)
        {
            // 1 と違って範囲外の場合ちゃんと丸めて返します。
            if (double.TryParse(str, out double num))
                return Math.Max(Math.Min(num, max), min);

            return def;
        }
        public static int ToInt32(this string str, int min, int max, int def)
        {
            // 1 と違って範囲外の場合ちゃんと丸めて返します。
            if (int.TryParse(str, out int num))
                return Math.Max(Math.Min(num, max), min);
                
            return def;
        }
    }
}