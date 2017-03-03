using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Views;
using Plugin.CurrentActivity;

namespace Plugin.ImageCropper
{
	public class HighlightView
	{
		// The View displaying the image.
		private readonly View _context;

		public enum ModifyMode
		{
			None,
			Move,
			Grow
		}

		private ModifyMode _mode = ModifyMode.None;

		private RectF _imageRect;  // in image space
		private RectF _cropRect;  // in image space
		public Matrix MatrixImage;

		private bool _maintainAspectRatio;
		private float _initialAspectRatio;

		private Drawable _resizeDrawableWidth;
		private Drawable _resizeDrawableHeight;

		private readonly Paint _focusPaint = new Paint ();
		private readonly Paint _noFocusPaint = new Paint ();
		private readonly Paint _outlinePaint = new Paint ();

		[Flags]
		public enum HitPosition
		{
			None,
			GrowLeftEdge,
			GrowRightEdge,
			GrowTopEdge,
			GrowBottomEdge,
			Move
		}

		#region Constructor
		public HighlightView (View ctx)
		{
			_context = ctx;
		}
		#endregion

		#region Properties
		public bool Focused { get; set; }

		public bool Hidden { get; set; }

		// in screen space
		public Rect DrawRect { get; private set; }

		// Returns the cropping rectangle in image space.
		public Rect CropRect => new Rect ((int)_cropRect.Left, (int)_cropRect.Top, (int)_cropRect.Right, (int)_cropRect.Bottom);

		public ModifyMode Mode
		{
			get { return _mode; }
			set
			{
				if (value != _mode)
				{
					_mode = value;
					_context.Invalidate ();
				}
			}
		}
		#endregion

		#region Public methods
		// Handles motion (dx, dy) in screen space.
		// The "edge" parameter specifies which edges the user is dragging.
		public void HandleMotion (HitPosition edge, float dx, float dy)
		{
			var r = ComputeLayout ();
			switch (edge)
			{
			case HitPosition.None:
				return;
			case HitPosition.Move:
				// Convert to image space before sending to moveBy().
				MoveBy (dx * (_cropRect.Width () / r.Width ()),
				        dy * (_cropRect.Height () / r.Height ()));
				break;
			default:
				if (!edge.HasFlag (HitPosition.GrowLeftEdge) && !edge.HasFlag (HitPosition.GrowRightEdge))
				{
					dx = 0;
				}

				if (!edge.HasFlag (HitPosition.GrowTopEdge) && !edge.HasFlag (HitPosition.GrowBottomEdge))
				{
					dy = 0;
				}

				// Convert to image space before sending to growBy().
				var xDelta = dx * (_cropRect.Width () / r.Width ());
				var yDelta = dy * (_cropRect.Height () / r.Height ());

				GrowBy ((edge.HasFlag (HitPosition.GrowLeftEdge) ? -1 : 1) * xDelta, 
				        (edge.HasFlag (HitPosition.GrowTopEdge) ? -1 : 1) * yDelta);
				break;
			}
		}

		public void Draw (Canvas canvas)
		{
			if (Hidden)
				return;

			canvas.Save ();

			if (!Focused)
			{
				_outlinePaint.Color = Color.White;
				canvas.DrawRect (DrawRect, _outlinePaint);
			}
			else
			{
				var viewDrawingRect = new Rect ();
				_context.GetDrawingRect (viewDrawingRect);

				_outlinePaint.Color = Color.White;// new Color(0XFF, 0xFF, 0x8A, 0x00);
				_focusPaint.Color = new Color (50, 50, 50, 125);

				var path = new Path ();
				path.AddRect (new RectF (DrawRect), Path.Direction.Cw);

				canvas.ClipPath (path, Region.Op.Difference);
				canvas.DrawRect (viewDrawingRect, _focusPaint);

				canvas.Restore ();
				canvas.DrawPath (path, _outlinePaint);

				if (_mode == ModifyMode.Grow)
				{
					var left = DrawRect.Left + 1;
					var right = DrawRect.Right + 1;
					var top = DrawRect.Top + 4;
					var bottom = DrawRect.Bottom + 3;

					var widthWidth = _resizeDrawableWidth.IntrinsicWidth / 2;
					var widthHeight = _resizeDrawableWidth.IntrinsicHeight / 2;
					var heightHeight = _resizeDrawableHeight.IntrinsicHeight / 2;
					var heightWidth = _resizeDrawableHeight.IntrinsicWidth / 2;

					var xMiddle = DrawRect.Left + ((DrawRect.Right - DrawRect.Left) / 2);
					var yMiddle = DrawRect.Top + ((DrawRect.Bottom - DrawRect.Top) / 2);

					_resizeDrawableWidth.SetBounds (left - widthWidth, 
					                                yMiddle - widthHeight, 
					                                left + widthWidth, 
					                                yMiddle + widthHeight);
					_resizeDrawableWidth.Draw (canvas);

					_resizeDrawableWidth.SetBounds (right - widthWidth, 
					                                yMiddle - widthHeight, 
					                                right + widthWidth, 
					                                yMiddle + widthHeight);
					_resizeDrawableWidth.Draw (canvas);

					_resizeDrawableHeight.SetBounds (xMiddle - heightWidth, 
					                                 top - heightHeight, 
					                                 xMiddle + heightWidth, 
					                                 top + heightHeight);
					_resizeDrawableHeight.Draw (canvas);

					_resizeDrawableHeight.SetBounds (xMiddle - heightWidth, 
					                                 bottom - heightHeight, 
					                                 xMiddle + heightWidth, 
					                                 bottom + heightHeight);
					_resizeDrawableHeight.Draw (canvas);
				}
			}
		}

		// Determines which edges are hit by touching at (x, y).
		public HitPosition GetHit (float x, float y)
		{
			var r = ComputeLayout ();
			const float hysteresis = 20F;
			var retval = HitPosition.None;

			// verticalCheck makes sure the position is between the top and
			// the bottom edge (with some tolerance). Similar for horizCheck.
			var verticalCheck = (y >= r.Top - hysteresis) && (y < r.Bottom + hysteresis);
			var horizCheck = (x >= r.Left - hysteresis) && (x < r.Right + hysteresis);

			// Check whether the position is near some edge(s).
			if ((Math.Abs (r.Left - x) < hysteresis) && verticalCheck)
			{
				retval |= HitPosition.GrowLeftEdge;
			}

			if ((Math.Abs (r.Right - x) < hysteresis) && verticalCheck)
			{
				retval |= HitPosition.GrowRightEdge;
			}

			if ((Math.Abs (r.Top - y) < hysteresis) && horizCheck)
			{
				retval |= HitPosition.GrowTopEdge;
			}

			if ((Math.Abs (r.Bottom - y) < hysteresis) && horizCheck)
			{
				retval |= HitPosition.GrowBottomEdge;
			}

			// Not near any edge but inside the rectangle: move.
			if (retval == HitPosition.None && r.Contains ((int)x, (int)y))
			{
				retval = HitPosition.Move;
			}

			return retval;
		}

		public void Invalidate ()
		{
			DrawRect = ComputeLayout ();
		}

		public void Setup (Matrix m, Rect imageRect, RectF cropRect, bool maintainAspectRatio)
		{
			MatrixImage = new Matrix (m);

			_cropRect = cropRect;
			_imageRect = new RectF (imageRect);
			_maintainAspectRatio = maintainAspectRatio;

			_initialAspectRatio = cropRect.Width () / cropRect.Height ();
			DrawRect = ComputeLayout ();

			_focusPaint.SetARGB (125, 50, 50, 50);
			_noFocusPaint.SetARGB (125, 50, 50, 50);
			_outlinePaint.StrokeWidth = 3;
			_outlinePaint.SetStyle (Paint.Style.Stroke);
			_outlinePaint.AntiAlias = true;

			_mode = ModifyMode.None;

			Init ();
		}
		#endregion

		#region Private helpers
		private void Init ()
		{
			_resizeDrawableWidth = ContextCompat.GetDrawable (CrossCurrentActivity.Current.Activity, Resource.Drawable.camera_crop_width);
			_resizeDrawableHeight = ContextCompat.GetDrawable (CrossCurrentActivity.Current.Activity, Resource.Drawable.camera_crop_height);
		}

		// Grows the cropping rectange by (dx, dy) in image space.
		private void MoveBy (float dx, float dy)
		{
			var invalRect = new Rect (DrawRect);

			_cropRect.Offset (dx, dy);

			// Put the cropping rectangle inside image rectangle.
			_cropRect.Offset (Math.Max (0, _imageRect.Left - _cropRect.Left), 
			                  Math.Max (0, _imageRect.Top - _cropRect.Top));

			_cropRect.Offset (Math.Min (0, _imageRect.Right - _cropRect.Right), 
			                  Math.Min (0, _imageRect.Bottom - _cropRect.Bottom));

			DrawRect = ComputeLayout ();
			invalRect.Union (DrawRect);
			invalRect.Inset (-10, -10);
			_context.Invalidate (invalRect);
		}

		// Grows the cropping rectange by (dx, dy) in image space.
		private void GrowBy (float dx, float dy)
		{
			if (_maintainAspectRatio)
			{
				if (Math.Abs (dx) > double.Epsilon)
				{
					dy = dx / _initialAspectRatio;
				}
				else if (Math.Abs (dy) > double.Epsilon)
				{
					dx = dy * _initialAspectRatio;
				}
			}

			// Don't let the cropping rectangle grow too fast.
			// Grow at most half of the difference between the image rectangle and
			// the cropping rectangle.
			var r = new RectF (_cropRect);
			if (dx > 0F && r.Width () + 2 * dx > _imageRect.Width ())
			{
				var adjustment = (_imageRect.Width () - r.Width ()) / 2F;
				dx = adjustment;
				if (_maintainAspectRatio)
				{
					dy = dx / _initialAspectRatio;
				}
			}

			if (dy > 0F && r.Height () + 2 * dy > _imageRect.Height ())
			{
				var adjustment = (_imageRect.Height () - r.Height ()) / 2F;
				dy = adjustment;
				if (_maintainAspectRatio)
				{
					dx = dy * _initialAspectRatio;
				}
			}

			r.Inset (-dx, -dy);

			// Don't let the cropping rectangle shrink too fast.
			var widthCap = 25F;
			if (r.Width () < widthCap)
			{
				r.Inset (-(widthCap - r.Width ()) / 2F, 0F);
			}
			var heightCap = _maintainAspectRatio ? (widthCap / _initialAspectRatio) : widthCap;
			if (r.Height () < heightCap)
			{
				r.Inset (0F, -(heightCap - r.Height ()) / 2F);
			}

			// Put the cropping rectangle inside the image rectangle.
			if (r.Left < _imageRect.Left)
			{
				r.Offset (_imageRect.Left - r.Left, 0F);
			}
			else if (r.Right > _imageRect.Right)
			{
				r.Offset (-(r.Right - _imageRect.Right), 0);
			}
			if (r.Top < _imageRect.Top)
			{
				r.Offset (0F, _imageRect.Top - r.Top);
			}
			else if (r.Bottom > _imageRect.Bottom)
			{
				r.Offset (0F, -(r.Bottom - _imageRect.Bottom));
			}

			_cropRect.Set (r);
			DrawRect = ComputeLayout ();
			_context.Invalidate ();
		}

		// Maps the cropping rectangle from image space to screen space.
		private Rect ComputeLayout ()
		{
			var r = new RectF (_cropRect.Left, _cropRect.Top, _cropRect.Right, _cropRect.Bottom);

			MatrixImage.MapRect (r);

			return new Rect ((int)Math.Round (r.Left), (int)Math.Round (r.Top), (int)Math.Round (r.Right), (int)Math.Round (r.Bottom));
		}
		#endregion
	}
}
