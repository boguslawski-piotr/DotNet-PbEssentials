using System;
using Xamarin.Forms;

namespace pbXForms
{
	public static class ColorExtensions
	{
		public static string ToHex(this Color v)
		{
			byte a = (byte)(double)(v.A * 255);
			byte r = (byte)(double)(v.R * 255);
			byte g = (byte)(double)(v.G * 255);
			byte b = (byte)(double)(v.B * 255);
			return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
		}
	}
}
