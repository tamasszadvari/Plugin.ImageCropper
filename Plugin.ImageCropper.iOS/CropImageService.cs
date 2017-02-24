using System;
using System.Threading.Tasks;
using CoreGraphics;
using UIKit;
using Wapps.TOCrop;
using Plugin.ImageCropper.Abstractions;

namespace Plugin.ImageCropper
{
	public class CropImageService : ICropImageService
	{
		#region cropimage

		public class CropVcDelegate : TOCropViewControllerDelegate
		{
			private readonly TaskCompletionSource<byte[]> _tcs;
			private readonly WeakReference<TOCropViewController> _owner;

			public Task<byte[]> Task => _tcs.Task;

			public CropVcDelegate (TOCropViewController owner)
			{
				_owner = new WeakReference<TOCropViewController> (owner);
				_tcs = new TaskCompletionSource<byte[]> ();
			}

			public override void DidCropImageToRect (TOCropViewController cropViewController, CGRect cropRect, nint angle)
			{
				//dissmiss viewcontroler
				cropViewController.PresentingViewController.DismissViewController (true, null);
				TOCropViewController owner;
				_tcs.SetResult (_owner.TryGetTarget (out owner) ? cropViewController.FinalImage.UIImageToBytes () : null);
			}

			public override void DidFinishCancelled (TOCropViewController cropViewController, bool cancelled)
			{
				//dissmiss viewcontroler
				cropViewController.PresentingViewController.DismissViewController (true, null);
				_tcs.SetResult (null);
			}
		}

		public Task<byte[]> CropImageFromOriginalToBytes (string filePath, CropAspect aspect = CropAspect.Custom)
		{
			var image = UIImage.FromFile (filePath);

			//crop image
			var viewController = new TOCropViewController (TOCropViewCroppingStyle.Default, image);
			var ndelegate = new CropVcDelegate (viewController);

			switch (aspect)
			{
			case CropAspect.Square:
				viewController.AspectRatioLockEnabled = true;
				viewController.AspectRatioPickerButtonHidden = true;
				viewController.AspectRatioPreset = TOCropViewControllerAspectRatioPreset.Square;
				break;
			}

			viewController.Delegate = ndelegate;
			//show
			viewController.PresentUsingRootViewController ();
			var result = ndelegate.Task.ContinueWith (t => t).Unwrap ();
			return result;
		}
		#endregion
	}
}
