using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ImagePreprocessingService;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DigitRecognizerService
{
    public class MLServiceDigitRecognizer : IDigitRecognizer
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _apiUrl;

        /// <summary>
        /// Recognize digits using Azure ML Service (Classic Web Service).
        /// </summary>
        /// <param name="apiUrl">API url for the web service.</param>
        public MLServiceDigitRecognizer(string apiUrl)
        {
            _apiUrl = apiUrl;
        }

        public async Task<Prediction> PredictAsync(byte[] image) => await PredictAsync(Image.Load(image));

        public async Task<Prediction> PredictAsync(Image<Rgba32> image)
        {
            var preprocessor = new Preprocessor(Rgba32.White, Rgba32.Black);
            image = preprocessor.Preprocess(image);

            var pixelArray = Preprocessor.ConvertImageToTwoDimensionalArray(image);

            var requestContent = new StringContent("{\"data\": " + JsonConvert.SerializeObject(new[] { pixelArray }) + "}");
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(_apiUrl, requestContent);

            var responseContent = response.Content is HttpContent c ? await c.ReadAsStringAsync() : null;

            //var prediction = JsonConvert.DeserializeObject<>(responseContent);

            //var tag = prediction.Predictions.OrderByDescending(p => p.Probability).First();

            return new Prediction
            {
                Tag = 0,
                Probability = 0
            };
        }
    }
}
