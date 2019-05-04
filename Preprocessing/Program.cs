using DigitRecognizerService;
using ImagePreprocessingService;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Threading.Tasks;

namespace Preprocessing
{
    class Program
    {
        static void Main(string[] args)
        {
            Predict("test.jpg").GetAwaiter().GetResult();
            Predict("test2.jpg").GetAwaiter().GetResult();
        }

        private static async Task Predict(string fileName)
        {
            using (Image<Rgba32> image = Image.Load(fileName))
            {
                var preprocessor = new Preprocessor(Rgba32.White, Rgba32.Black);
                var i = preprocessor.Preprocess(image);
                i.Save("result2-7.png", new PngEncoder());

                var pixels = Preprocessor.ConvertImageToArray(i);
                //Console.WriteLine(JsonConvert.SerializeObject(pixels));

                for (int j = 0; j < 784; j++)
                {
                    Console.Write(pixels[j].ToString("D3"));
                    if ((j + 1) % 28 == 0) Console.WriteLine();
                }

                var recognizer = new MLStudioDigitRecognizer("API_URL", "API_KEY");

                var prediction = await recognizer.PredictAsync(i);

                Console.WriteLine($"\n\n\nThis is a(n) {prediction.Tag}! (I'm {prediction.Probability*100}% sure.)\n\n\n");
            }
        }
    }
}
