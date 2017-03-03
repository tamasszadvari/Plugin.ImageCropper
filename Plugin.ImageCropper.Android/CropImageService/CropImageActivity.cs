using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Views;
using Android.Widget;
using Plugin.ImageCropper.Abstractions;

namespace Plugin.ImageCropper
{
	/// <summary>
	/// The activity can crop specific region of interest from an image.
	/// </summary>
	[Activity]
	public class CropImageActivity : MonitoredActivity
	{
		internal static event EventHandler<XViewEventArgs> MediaCroped;

		#region Private members
		// These are various options can be specified in the intent.
		private Bitmap.CompressFormat _outputFormat = Bitmap.CompressFormat.Jpeg;
		private global::Android.Net.Uri _saveUri;
		private int _aspectX, _aspectY;
		private readonly Handler _mHandler = new Handler ();

		// These options specifiy the output image size and whether we should
		// scale the output to fit it (or just crop it).
		private int _outputX, _outputY;
		private bool _scale;
		private bool _scaleUp = true;

		private CropImageView _imageView;
		private Bitmap _bitmap;

		private string _imagePath;

		private const int NoStorageError = -1;
		private const int CannotStatError = -2;
		#endregion

		#region Properties
		public HighlightView Crop { set; get; }

		/// <summary>
		/// Whether the "save" button is already clicked.
		/// </summary>
		public bool Saving { get; set; }
		#endregion

		#region Overrides
		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			RequestWindowFeature (WindowFeatures.NoTitle);
			SetContentView (Resource.Layout.CropImage);

			_imageView = FindViewById<CropImageView> (Resource.Id.image);

			ShowStorageToast (this);

			var extras = Intent.Extras;
			if (extras != null)
			{
				_imagePath = extras.GetString ("image-path");

				_saveUri = GetImageUri (_imagePath);
				if (extras.GetString (MediaStore.ExtraOutput) != null)
				{
					_saveUri = GetImageUri (extras.GetString (MediaStore.ExtraOutput));
				}

				_bitmap = GetBitmap (_imagePath);

				_aspectX = extras.GetInt ("aspectX");
				_aspectY = extras.GetInt ("aspectY");
				_outputX = extras.GetInt ("outputX");
				_outputY = extras.GetInt ("outputY");
				_scale = extras.GetBoolean ("scale", true);
				_scaleUp = extras.GetBoolean ("scaleUpIfNeeded", true);

				if (extras.GetString ("outputFormat") != null)
				{
					_outputFormat = Bitmap.CompressFormat.ValueOf (extras.GetString ("outputFormat"));
				}
			}

			if (_bitmap == null)
			{
				Finish ();
				//raise event
				MediaCroped?.Invoke (this, new XViewEventArgs (nameof (MediaCroped), null));
				return;
			}

			Window.AddFlags (WindowManagerFlags.Fullscreen);

			FindViewById<Button> (Resource.Id.discard).Click += (sender, e) => OnDiscardClick ();
			FindViewById<Button> (Resource.Id.save).Click += (sender, e) => OnSaveClicked ();
			FindViewById<Button> (Resource.Id.rotateLeft).Click += (o, e) => OnRotateClicked (-90);
			FindViewById<Button> (Resource.Id.rotateRight).Click += (o, e) => OnRotateClicked (90);

			_imageView.SetImageBitmapResetBase (_bitmap, true);
			AddHighlightView ();
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();

			if (_bitmap != null && !_bitmap.IsRecycled)
			{
				_bitmap.Recycle ();
			}
		}
		#endregion

		#region Private helpers
		private void OnRotateClicked (int degree)
		{
			_bitmap.RotateImage (degree);
			var rotateBitmap = new RotateBitmap (_bitmap);
			_imageView.SetImageRotateBitmapResetBase (rotateBitmap, true);
			AddHighlightView ();
		}

		private void AddHighlightView ()
		{
			Crop = new HighlightView (_imageView);

			var width = _bitmap.Width;
			var height = _bitmap.Height;

			var imageRect = new Rect (0, 0, width, height);

			// make the default size about 4/5 of the width or height
			var cropWidth = Math.Min (width, height) * 4 / 5;
			var cropHeight = cropWidth;

			if (_aspectX != 0 && _aspectY != 0)
			{
				cropHeight = (_aspectX > _aspectY) 
					? cropWidth * _aspectY / _aspectX 
					: cropHeight * _aspectX / _aspectY;
			}

			var x = (width - cropWidth) / 2;
			var y = (height - cropHeight) / 2;

			var cropRect = new RectF (x, y, x + cropWidth, y + cropHeight);
			Crop.Setup (_imageView.ImageMatrix, imageRect, cropRect, _aspectX != 0 && _aspectY != 0);

			_imageView.ClearHighlightViews ();
			Crop.Focused = true;
			_imageView.AddHighlightView (Crop);
		}

		private static Android.Net.Uri GetImageUri (string path)
		{
			return Android.Net.Uri.FromFile (new Java.IO.File (path));
		}

		private Bitmap GetBitmap (string path)
		{
			var uri = GetImageUri (path);

			try {
				const int imageMaxSize = 1024;
				var ins = ContentResolver.OpenInputStream (uri);

				// Decode image size
				var o = new BitmapFactory.Options { InJustDecodeBounds = true };

				var temp = BitmapFactory.DecodeStream (ins, null, o);
				ins.Close ();

				temp.Recycle ();

				var scale = 1;
				if (o.OutHeight > imageMaxSize || o.OutWidth > imageMaxSize)
				{
					scale = (int)Math.Pow (2, (int)Math.Round (Math.Log (imageMaxSize / (double)Math.Max (o.OutHeight, o.OutWidth)) / Math.Log (0.5)));
				}

				var o2 = new BitmapFactory.Options { InSampleSize = scale };
				ins = ContentResolver.OpenInputStream (uri);
				var b = BitmapFactory.DecodeStream (ins, null, o2);
				ins.Close ();

				return b;
			}
			catch (Exception e)
			{
				Log.Error (GetType ().Name, e.Message);
			}

			return null;
		}

		private void OnSaveClicked ()
		{
			// TODO this code needs to change to use the decode/crop/encode single
			// step api so that we don't require that the whole (possibly large)
			// bitmap doesn't have to be read into memory
			if (Saving)
				return;

			Saving = true;

			var width = Crop.CropRect.Width ();
			var height = Crop.CropRect.Height ();

			var croppedImage = Bitmap.CreateBitmap (width, height, Bitmap.Config.Rgb565);
			{
				var canvas = new Canvas (croppedImage);
				var dstRect = new Rect (0, 0, width, height);
				canvas.DrawBitmap (_bitmap, Crop.CropRect, dstRect, null);
			}

			// If the output is required to a specific size then scale or fill
			if (_outputX != 0 && _outputY != 0)
			{
				if (_scale)
				{
					// Scale the image to the required dimensions
					croppedImage.Transform (new Matrix (), _outputX, _outputY, _scaleUp);
				}
				else
				{
					// Don't scale the image crop it to the size requested.
					// Create an new image with the cropped image in the center and
					// the extra space filled.    
					var b = Bitmap.CreateBitmap (_outputX, _outputY, Bitmap.Config.Rgb565);
					var canvas = new Canvas (b);

					var srcRect = Crop.CropRect;
					var dstRect = new Rect (0, 0, _outputX, _outputY);

					var dx = (srcRect.Width () - dstRect.Width ()) / 2;
					var dy = (srcRect.Height () - dstRect.Height ()) / 2;

					// If the srcRect is too big, use the center part of it.
					srcRect.Inset (Math.Max (0, dx), Math.Max (0, dy));

					// If the dstRect is too big, use the center part of it.
					dstRect.Inset (Math.Max (0, -dx), Math.Max (0, -dy));

					// Draw the cropped bitmap in the center
					canvas.DrawBitmap (_bitmap, srcRect, dstRect, null);

					// Set the cropped bitmap as the new bitmap
					croppedImage.Recycle ();
					croppedImage = b;
				}
			}

			// Return the cropped image directly or save it to the specified URI.
			var myExtras = Intent.Extras;
			if (myExtras != null && (myExtras.GetParcelable ("data") != null || myExtras.GetBoolean ("return-data")))
			{
				var extras = new Bundle ();
				extras.PutParcelable ("data", croppedImage);
				SetResult (Result.Ok, (new Intent ()).SetAction ("inline-data").PutExtras (extras));
				Finish ();
			}
			else
			{
				BackgroundJob.StartBackgroundJob (this, null, Resources.GetString(Resource.String.savingImage), () => SaveOutput (croppedImage), _mHandler);
			}

			//raise event
			MediaCroped?.Invoke (this, new XViewEventArgs (nameof (MediaCroped), croppedImage));
		}

		private void OnDiscardClick ()
		{
			SetResult (Result.Canceled);
			Finish ();

			//raise event
			MediaCroped?.Invoke (this, new XViewEventArgs (nameof (MediaCroped), null));
		}

		private void SaveOutput (Bitmap croppedImage)
		{
			if (_saveUri != null)
			{
				try {
					using (var outputStream = ContentResolver.OpenOutputStream (_saveUri))
					{
						if (outputStream != null)
						{
							croppedImage.Compress (_outputFormat, 75, outputStream);
						}
					}
				}
				catch (Exception ex)
				{
					Log.Error (GetType ().Name, ex.Message);
				}

				var extras = new Bundle ();
				SetResult (Result.Ok, new Intent (_saveUri.ToString ()).PutExtras (extras));
			}
			else
			{
				Log.Error (GetType ().Name, "not defined image url");
			}

			croppedImage.Recycle ();
			Finish ();
		}

		private static void ShowStorageToast (Activity activity)
		{
			ShowStorageToastWithRemain (activity, CalculatePicturesRemaining ());
		}

		private static void ShowStorageToastWithRemain (Activity activity, int remaining)
		{
			string noStorageText = null;

			if (remaining == NoStorageError)
			{
				noStorageText = (Android.OS.Environment.ExternalStorageState == Android.OS.Environment.MediaChecking) 
					? activity.Resources.GetString(Resource.String.preparingCard)
					: activity.Resources.GetString (Resource.String.noStorageCard);
			}
			else if (remaining < 1)
			{
				noStorageText = activity.Resources.GetString (Resource.String.notEnoughSpace);
			}

			if (noStorageText != null)
			{
				Toast.MakeText (activity, noStorageText, ToastLength.Long).Show ();
			}
		}

		private static int CalculatePicturesRemaining ()
		{
			try {
				var storageDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory (Android.OS.Environment.DirectoryPictures).ToString ();
				var stat = new StatFs (storageDirectory);
				var remaining = stat.AvailableBlocksLong * (float)stat.BlockSizeLong / 400000F;

				return (int)remaining;
			}
			catch (Exception)
			{
				// if we can't stat the filesystem then we don't know how many
				// pictures are remaining.  it might be zero but just leave it
				// blank since we really don't know.
				return CannotStatError;
			}
		}
		#endregion
	}
}
