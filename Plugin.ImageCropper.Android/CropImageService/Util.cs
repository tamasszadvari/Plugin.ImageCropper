using System;
using Android.Graphics;

namespace Plugin.ImageCropper
{
	public static class Util
	{
		// Rotates the bitmap by the specified degree.
		// If a new bitmap is created, the original bitmap is recycled.
		public static Bitmap RotateImage (Bitmap b, int degrees)
		{
			if (degrees != 0 && b != null)
			{
				var m = new Matrix ();
				m.SetRotate (degrees, (float)b.Width / 2, (float)b.Height / 2);

				try {
					var b2 = Bitmap.CreateBitmap (b, 0, 0, b.Width, b.Height, m, true);
					if (b != b2)
					{
						b.Recycle ();
						b = b2;
					}
				}
				catch (Java.Lang.OutOfMemoryError)
				{
					// We have no memory to rotate. Return the original bitmap.
				}
			}

			return b;
		}

		public static Bitmap Transform (Matrix scaler, Bitmap source, int targetWidth, int targetHeight, bool scaleUp)
		{
			var deltaX = source.Width - targetWidth;
			var deltaY = source.Height - targetHeight;

			if (!scaleUp && (deltaX < 0 || deltaY < 0))
			{
				// In this case the bitmap is smaller, at least in one dimension,
				// than the target.  Transform it by placing as much of the image
				// as possible into the target and leaving the top/bottom or
				// left/right (or both) black.
				var b2 = Bitmap.CreateBitmap (targetWidth, targetHeight, Bitmap.Config.Argb8888);
				var c = new Canvas (b2);

				var deltaXHalf = Math.Max (0, deltaX / 2);
				var deltaYHalf = Math.Max (0, deltaY / 2);

				var src = new Rect (deltaXHalf, deltaYHalf, 
				                    deltaXHalf + Math.Min (targetWidth, source.Width), 
				                    deltaYHalf + Math.Min (targetHeight, source.Height));

				var dstX = (targetWidth - src.Width ()) / 2;
				var dstY = (targetHeight - src.Height ()) / 2;

				var dst = new Rect (dstX, dstY, targetWidth - dstX, targetHeight - dstY);

				c.DrawBitmap (source, src, dst, null);

				return b2;
			}

			float bitmapWidthF = source.Width;
			float bitmapHeightF = source.Height;

			var bitmapAspect = bitmapWidthF / bitmapHeightF;
			var viewAspect = (float)targetWidth / targetHeight;

			if (bitmapAspect > viewAspect)
			{
				var scale = targetHeight / bitmapHeightF;
				if (scale < .9F || scale > 1F)
				{
					scaler.SetScale (scale, scale);
				}
				else
				{
					scaler = null;
				}
			}
			else
			{
				var scale = targetWidth / bitmapWidthF;
				if (scale < .9F || scale > 1F)
				{
					scaler.SetScale (scale, scale);
				}
				else
				{
					scaler = null;
				}
			}

			Bitmap b1 = (scaler != null) 
				? Bitmap.CreateBitmap (source, 0, 0, source.Width, source.Height, scaler, true) // this is used for minithumb and crop, so we want to filter here.
	            : source;

			var dx1 = Math.Max (0, b1.Width - targetWidth);
			var dy1 = Math.Max (0, b1.Height - targetHeight);

			var b3 = Bitmap.CreateBitmap (b1, (dx1 / 2), (dy1 / 2), targetWidth, targetHeight);

			if (b1 != source)
			{
				b1.Recycle ();
			}

			return b3;
		}
	}
}
