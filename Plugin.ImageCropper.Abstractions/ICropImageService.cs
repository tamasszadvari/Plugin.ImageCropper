using System.Threading.Tasks;

namespace Plugin.ImageCropper.Abstractions
{
	public interface ICropImageService
	{
		Task<byte[]> CropImageFromOriginalToBytes (string filePath, CropAspect aspect = CropAspect.Custom);
	}
}
