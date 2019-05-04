using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Threading.Tasks;

namespace DigitRecognizerService
{
    public interface IDigitRecognizer
    {
        Task<Prediction> PredictAsync(byte[] image);

        Task<Prediction> PredictAsync(Image<Rgba32> image);
    }
}
