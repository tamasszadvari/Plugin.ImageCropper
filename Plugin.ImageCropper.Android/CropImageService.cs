using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Plugin.CurrentActivity;
using Plugin.ImageCropper.Abstractions;

namespace Plugin.ImageCropper
{
	[Android.Runtime.Preserve (AllMembers = true)]
	public class CropImageService : ICropImageService
	{
		private int GetRequestId ()
		{
			var id = _requestId;
			if (_requestId == int.MaxValue)
				_requestId = 0;
			else
				_requestId++;

			return id;
		}

		private int _requestId;
		private TaskCompletionSource<byte[]> _completionSource;

		public Task<byte[]> CropImageFromOriginalToBytes (string filePath, CropAspect aspect = CropAspect.Custom)
		{
			var id = GetRequestId ();

			var ntcs = new TaskCompletionSource<byte[]> (id);
			if (Interlocked.CompareExchange (ref _completionSource, ntcs, null) != null)
			{
#if DEBUG
				throw new InvalidOperationException ("Only one operation can be active at a time");
#else
                return null;
#endif
			}

			var intent = new Intent (CrossCurrentActivity.Current.Activity, typeof (CropImageActivity));
			intent.PutExtra ("image-path", filePath);
			intent.PutExtra ("scale", true);

			switch (aspect)
			{
			case CropAspect.Square:
				intent.PutExtra ("aspectX", 1);
				intent.PutExtra ("aspectY", 1);
				break;
			}

			//event
			EventHandler<XViewEventArgs> handler = null;
			handler = (s, e) => {
				var tcs = Interlocked.Exchange (ref _completionSource, null);

				CropImageActivity.MediaCroped -= handler;
				tcs.SetResult ((e.CastObject as Bitmap)?.BitmapToBytes ());
			};

			CropImageActivity.MediaCroped += handler;
			CrossCurrentActivity.Current.Activity.StartActivity (intent);

			return _completionSource.Task;
		}
	}
}
