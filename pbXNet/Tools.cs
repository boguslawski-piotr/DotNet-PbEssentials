using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace pbXNet
{
    public static class Tools
    {
        public static bool IsDifferent<T>(T a, ref T b)
        {
            if (Equals(a, b))
                return false;
            b = a;
            return true;
        }


    }
}
