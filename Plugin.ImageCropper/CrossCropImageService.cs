using System;
using Plugin.ImageCropper.Abstractions;

namespace Plugin.ImageCropper
{
	public static class CrossCropImageService
	{
		static Lazy<ICropImageService> TTS = new Lazy<ICropImageService> (GetService, System.Threading.LazyThreadSafetyMode.PublicationOnly);

		public static ICropImageService Current
		{
			get
			{
				var ret = TTS.Value;
				if (ret == null)
				{
					throw NotImplementedInReferenceAssembly ();
				}
				return ret;
			}
		}

		static ICropImageService GetService ()
		{
#if PORTABLE
        return null;
#else
			return new CropImageService ();
#endif
		}

		internal static Exception NotImplementedInReferenceAssembly ()
		{
			return new NotImplementedException ("This functionality is not implemented in the portable version of this assembly.  You should reference the Xam.Plugins.ImageCropper NuGet package from your main application project in order to reference the platform-specific implementation.");
		}
	}
}
