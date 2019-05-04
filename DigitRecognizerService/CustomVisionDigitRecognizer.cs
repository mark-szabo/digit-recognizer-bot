using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ImagePreprocessingService;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace DigitRecognizerService
{
    public class CustomVisionDigitRecognizer : IDigitRecognizer
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _baseUrl;
        private readonly Guid _projectId;
        private readonly string _publishedName;
        private readonly string _apiKey;

        /// <summary>
        /// Recognize digits using Custom Vision.
        /// </summary>
        /// <param name="baseUrl">Custom Vision API base url.</param>
        /// <param name="projectId">Custom Vision project id.</param>
        /// <param name="publishedName">Specifies the name of the model to evaluate against.</param>
        /// <param name="apiKey">Custom Vision API key.</param>
        public CustomVisionDigitRecognizer(string baseUrl, string projectId, string publishedName, string apiKey)
        {
            _baseUrl = baseUrl;
            _projectId = new Guid(projectId);
            _publishedName = publishedName;
            _apiKey = apiKey;
        }

        public async Task<Prediction> PredictAsync(byte[] image)
        {
            var preprocessor = new Preprocessor(Rgba32.Black, Rgba32.White);
            image = preprocessor.Preprocess(image);

            var customVision = new CustomVisionPredictionClient(_httpClient, false)
            {
                ApiKey = _apiKey,
                Endpoint = _baseUrl,
            };

            var stream = new MemoryStream(image);

            var prediction = (await customVision.ClassifyImageWithHttpMessagesAsync(_projectId, _publishedName, stream)).Body;

            var tag = prediction.Predictions.OrderByDescending(p => p.Probability).First();

            return new Prediction
            {
                Tag = Convert.ToInt32(tag.TagName),
                Probability = tag.Probability
            };
        }

        public async Task<Prediction> PredictAsync(Image<Rgba32> image)
        {
            var preprocessor = new Preprocessor(Rgba32.Black, Rgba32.White);
            image = preprocessor.Preprocess(image);

            var customVision = new CustomVisionPredictionClient(_httpClient, false)
            {
                ApiKey = _apiKey,
                Endpoint = _baseUrl,
            };

            var stream = new MemoryStream();
            image.SaveAsPng(stream);

            var prediction = (await customVision.ClassifyImageWithHttpMessagesAsync(_projectId, _publishedName, stream)).Body;

            var tag = prediction.Predictions.OrderByDescending(p => p.Probability).First();

            return new Prediction
            {
                Tag = Convert.ToInt32(tag.TagName),
                Probability = tag.Probability
            };
        }
        
        public async Task<Prediction> PredictWithRestApiAsync(byte[] image)
        {
            var preprocessor = new Preprocessor(Rgba32.Black, Rgba32.White);
            image = preprocessor.Preprocess(image);

            var requestContent = new ByteArrayContent(image);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _apiKey);

            var response = await _httpClient
                .PostAsync(
                    $"{_baseUrl}/customvision/v3.0/Prediction/{_projectId}/classify/iterations/{_publishedName}/image",
                    requestContent);

            response.EnsureSuccessStatusCode();

            var responseContent = response.Content is HttpContent c ? await c.ReadAsStringAsync() : throw new Exception();
            var prediction = JsonConvert.DeserializeObject<CustomVisionResponseObject>(responseContent);

            var tag = prediction.Predictions.OrderByDescending(p => p.Probability).First();

            return new Prediction
            {
                Tag = Convert.ToInt32(tag.TagName),
                Probability = tag.Probability
            };
        }
        
        public async Task<Prediction> PredictWithRestApiAsync(Image<Rgba32> image)
        {
            var preprocessor = new Preprocessor(Rgba32.Black, Rgba32.White);
            image = preprocessor.Preprocess(image);

            var stream = new MemoryStream();
            image.SaveAsPng(stream);

            var requestContent = new ByteArrayContent(stream.ToArray());
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            _httpClient.DefaultRequestHeaders.Add("Prediction-Key", _apiKey);

            var response = await _httpClient
                .PostAsync(
                    $"{_baseUrl}/customvision/v3.0/Prediction/{_projectId}/classify/iterations/{_publishedName}/image",
                    requestContent);

            response.EnsureSuccessStatusCode();

            var responseContent = response.Content is HttpContent c ? await c.ReadAsStringAsync() : throw new Exception();
            var prediction = JsonConvert.DeserializeObject<CustomVisionResponseObject>(responseContent);

            var tag = prediction.Predictions.OrderByDescending(p => p.Probability).First();

            return new Prediction
            {
                Tag = Convert.ToInt32(tag.TagName),
                Probability = tag.Probability
            };
        }

        private class CustomVisionResponseObject
        {
            public CustomVisionPrediction[] Predictions { get; set; }
            public class CustomVisionPrediction
            {
                public double Probability { get; set; }
                public string TagName { get; set; }
            }
        }
    }
}
