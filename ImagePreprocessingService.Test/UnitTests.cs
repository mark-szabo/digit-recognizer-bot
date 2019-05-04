using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DigitRecognizerService.Test
{
    public class UnitTests
    {
        private const string _customVisionBaseUrl = "";
        private const string _customVisionProjectId = "";
        private const string _customVisionPublishedName = "";
        private const string _customVisionApiKey = "";

        private const string _mlStudioApiUrl = "";
        private const string _mlStudioApiKey = "";

        private const string _mlServiceApiUrl = "";

        [Fact]
        public async Task PredictWithCustomVisionAsync_ReturnsPredictionAsync()
        {
            var fileStream = new FileStream("test2.jpg", FileMode.Open, FileAccess.Read);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);

            var byteArray = memoryStream.ToArray();

            var recognizer = new CustomVisionDigitRecognizer(
                _customVisionBaseUrl,
                _customVisionProjectId,
                _customVisionPublishedName,
                _customVisionApiKey);

            var prediction = await recognizer.PredictAsync(byteArray);

            Assert.Equal(5, prediction.Tag);
        }

        [Fact]
        public async Task PredictWithRestCustomVisionAsync_ReturnsPredictionAsync()
        {
            var fileStream = new FileStream("test2.jpg", FileMode.Open, FileAccess.Read);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);

            var byteArray = memoryStream.ToArray();

            var recognizer = new CustomVisionDigitRecognizer(
                _customVisionBaseUrl,
                _customVisionProjectId,
                _customVisionPublishedName,
                _customVisionApiKey);

            var prediction = await recognizer.PredictWithRestApiAsync(byteArray);

            Assert.Equal(5, prediction.Tag);
        }

        [Fact]
        public async Task PredictWithMLStudioAsync_ReturnsPredictionAsync()
        {
            var fileStream = new FileStream("test2.jpg", FileMode.Open, FileAccess.Read);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);

            var byteArray = memoryStream.ToArray();

            var recognizer = new MLStudioDigitRecognizer(
                _mlStudioApiUrl,
                _mlStudioApiKey);

            var prediction = await recognizer.PredictAsync(byteArray);

            Assert.Equal(5, prediction.Tag);
        }

        [Fact]
        public async Task PredictWithMLServiceAsync_ReturnsPredictionAsync()
        {
            var fileStream = new FileStream("test2.jpg", FileMode.Open, FileAccess.Read);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);

            var byteArray = memoryStream.ToArray();

            var recognizer = new MLServiceDigitRecognizer(_mlServiceApiUrl);

            var prediction = await recognizer.PredictAsync(byteArray);

            Assert.Equal(5, prediction.Tag);
        }
    }
}
