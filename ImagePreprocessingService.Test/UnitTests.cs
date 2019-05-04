using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ImagePreprocessingService;

namespace ImagePreprocessingService.Test
{
    public class UnitTests
    {
        [Fact]
        public async Task PreprocessAndPredict_ReturnsPredictionAsync()
        {
            var fileStream = new FileStream("test2.jpg", FileMode.Open, FileAccess.Read);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);

            var byteArray = memoryStream.ToArray();

            var prediction = await Preprocessor.PreprocessAndPredict(byteArray);

            Assert.Equal(5, prediction.Tag);
        }
    }
}
