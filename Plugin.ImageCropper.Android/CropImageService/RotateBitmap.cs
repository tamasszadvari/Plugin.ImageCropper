using Android.Graphics;

namespace Plugin.ImageCropper
{
	public class RotateBitmap
	{
		public const string TAG = "RotateBitmap";

		public RotateBitmap (Bitmap bitmap)
		{
			Bitmap = bitmap;
		}

		public RotateBitmap (Bitmap bitmap, int rotation)
		{
			Bitmap = bitmap;
			Rotation = rotation % 360;
		}

		public int Rotation { get; set; }

		public Bitmap Bitmap { get; set; }

		public Matrix GetRotateMatrix ()
		{
			// By default this is an identity MatrixImage.
			var matrix = new Matrix ();

			if (Rotation != 0)
			{
				// We want to do the rotation at origin, but since the bounding
				// rectangle will be changed after rotation, so the delta values
				// are based on old & new width/height respectively.
				var cx = Bitmap.Width / 2;
				var cy = Bitmap.Height / 2;
				matrix.PreTranslate (-cx, -cy);
				matrix.PostRotate (Rotation);
				matrix.PostTranslate (Width / 2, Height / 2);
			}

			return matrix;
		}

		public bool IsOrientationChanged => (Rotation / 90) % 2 != 0;

		public int Height => IsOrientationChanged ? Bitmap.Width : Bitmap.Height;

		public int Width => IsOrientationChanged ? Bitmap.Height : Bitmap.Width;

		// TOOD: Recyle
	}
}
