using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.IO;

namespace Preprocessing
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Image<Rgba32> image = Image.Load("test.jpg"))
            {
                var i = Preprocess(image);
                i.Save("testresult.jpg");

                var stream = new MemoryStream();
                i.Save(stream, JpegFormat.Instance);
            }
            using (Image<Rgba32> image = Image.Load("test2.jpg"))
            {
                var i = Preprocess(image);
                i.Save("testresult2.jpg");
            }
        }

        static Image<Rgba32> Preprocess(Image<Rgba32> image)
        {
            // image is now in a file format agnostic structure in memory as a series of Rgba32 pixels

            image.Mutate(x => x.Grayscale());

            image.Mutate(x => x.Vignette(new Rgba32(255, 255, 255)));

            image.Mutate(x => x.BinaryThreshold(0.6f, new Rgba32(255, 255, 255), new Rgba32(0, 0, 0)));

            var topLeftX = 0;
            for (int i = 0; i < image.Width; i++)
            {
                bool whiteRow = true;
                for (int j = 0; j < image.Height; j++)
                {
                    if (image[i, j] != Rgba32.White)
                    {
                        whiteRow = false;
                        break;
                    }
                }

                if (!whiteRow) break;
                topLeftX = i;
            }

            var topLeftY = 0;
            for (int j = 0; j < image.Height; j++)
            {
                bool whiteColumn = true;
                for (int i = 0; i < image.Width; i++)
                {
                    if (image[i, j] != Rgba32.White)
                    {
                        whiteColumn = false;
                        break;
                    }
                }

                if (!whiteColumn) break;
                topLeftY = j;
            }

            var bottomRightX = 0;
            for (int i = image.Width - 1; i >= 0; i--)
            {
                bool whiteRow = true;
                for (int j = image.Height - 1; j >= 0; j--)
                {
                    if (image[i, j] != Rgba32.White)
                    {
                        whiteRow = false;
                        break;
                    }
                }

                if (!whiteRow) break;
                bottomRightX = i;
            }

            var bottomRightY = 0;
            for (int j = image.Height - 1; j >= 0; j--)
            {
                bool whiteColumn = true;
                for (int i = image.Width - 1; i >= 0; i--)
                {
                    if (image[i, j] != Rgba32.White)
                    {
                        whiteColumn = false;
                        break;
                    }
                }

                if (!whiteColumn) break;
                bottomRightY = j;
            }

            image.Mutate(x => x.Crop(new Rectangle(topLeftX, topLeftY, bottomRightX - topLeftX, bottomRightY - topLeftY)));

            var maxWidthHeight = Math.Max(image.Width, image.Height);
            image.Mutate(x => x.Pad(maxWidthHeight, maxWidthHeight).BackgroundColor(new Rgba32(255, 255, 255)));

            image.Mutate(x => x.Resize(20, 20));

            image.Mutate(x => x.Pad(28, 28).BackgroundColor(new Rgba32(255, 255, 255)));

            return image;
        }
    }
}
