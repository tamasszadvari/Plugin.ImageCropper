using System;
using Foundation;
using UIKit;

namespace Plugin.ImageCropper
{
	public static class Extensions
	{
		#region image <=> byte[]
		public static byte[] UIImageToBytes (this UIImage image)
		{
			if (image == null)
				return null;
			
			NSData data = null;

			try {
				data = image.AsPNG ();
				return data.ToArray ();
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				image.Dispose ();
				data?.Dispose ();
			}
		}

		public static UIImage BytesToUIImage (this byte[] data)
		{
			if (data == null)
				return null;
			
			UIImage image;
			try {
				image = new UIImage (NSData.FromArray (data));
			}
			catch (Exception)
			{
				return null;
			}

			return image;
		}

		#endregion

		public static void PresentUsingRootViewController (this UIViewController controller)
		{
			if (controller == null)
				throw new ArgumentNullException (nameof (controller));

			var visibleViewController = GetVisibleViewController (null);
			visibleViewController?.PresentViewController (controller, true, () => { });
		}

		private static UIViewController GetVisibleViewController (UIViewController controller)
		{
			if (controller == null)
			{
				controller = UIApplication.SharedApplication.KeyWindow.RootViewController;
			}

			if (controller?.NavigationController?.VisibleViewController != null)
			{
				return controller.NavigationController.VisibleViewController;
			}

			if (controller != null && (controller.IsViewLoaded && controller.View?.Window != null))
			{
				return controller;
			}

			if (controller != null)
			{
				foreach (var childViewController in controller.ChildViewControllers)
				{
					var foundVisibleViewController = GetVisibleViewController (childViewController);
					if (foundVisibleViewController == null)
						continue;

					return foundVisibleViewController;
				}
			}
			return controller;
		}
	}
}
