# Workshop lab guide

## PreprocessingService

```console
dotnet add package Newtonsoft.Json
dotnet add package SixLabors.ImageSharp
```

```csharp
public class Preprocessor
{
    private readonly Rgba32 _backgroundColor = Rgba32.White;
    private readonly Rgba32 _foregroundColor = Rgba32.Black;

    public Preprocessor() { }

    public Preprocessor(Rgba32 backgroundColor, Rgba32 foregroundColor)
    {
        _backgroundColor = backgroundColor;
        _foregroundColor = foregroundColor;
    }

    /// <summary>
    /// Preprocess camera images for MNIST-based neural networks.
    /// </summary>
    /// <param name="image">Source image in a byte array.</param>
    /// <returns>Preprocessed image in a byte array.</returns>
    public byte[] Preprocess(byte[] input)
    {
        Image<Rgba32> image = Image.Load(input);

        image = Preprocess(image);

        var stream = new MemoryStream();
        image.SaveAsPng(stream);

        return stream.ToArray();
    }

    /// <summary>
    /// Preprocess camera images for MNIST-based neural networks.
    /// </summary>
    /// <param name="image">Source image in a file format agnostic structure in memory as a series of Rgba32 pixels.</param>
    /// <returns>Preprocessed image in a file format agnostic structure in memory as a series of Rgba32 pixels.</returns>
    public Image<Rgba32> Preprocess(Image<Rgba32> image)
    {
        // Step 1: Apply a grayscale filter
        image.Mutate(i => i.Grayscale());

        // Step 2: Apply a white vignette on the corners to remove shadow marks
        image.Mutate(i => i.Vignette(Rgba32.White));

        // Step 3: Separate foreground and background with a threshold and set the correct colors
        image.Mutate(i => i.BinaryThreshold(0.6f, _backgroundColor, _foregroundColor));

        // Step 4: Crop to bounding box
        var boundingBox = FindBoundingBox(image);
        image.Mutate(i => i.Crop(boundingBox));

        // Step 5: Make the image a square
        var maxWidthHeight = Math.Max(image.Width, image.Height);
        image.Mutate(i => i.Pad(maxWidthHeight, maxWidthHeight).BackgroundColor(_backgroundColor));

        // Step 6: Downscale to 20x20
        image.Mutate(i => i.Resize(20, 20));

        // Step 7: Add 4 pixel margin
        image.Mutate(i => i.Pad(28, 28).BackgroundColor(_backgroundColor));

        return image;
    }

    private Rectangle FindBoundingBox(Image<Rgba32> image)
    {
        // ➡
        var topLeftX = F(0, 0, x => x < image.Width, y => y < image.Height, true, 1);

        // ⬇
        var topLeftY = F(0, 0, y => y < image.Height, x => x < image.Width, false, 1);

        // ⬅
        var bottomRightX = F(image.Width - 1, image.Height - 1, x => x >= 0, y => y >= 0, true, -1);

        // ⬆
        var bottomRightY = F(image.Height - 1, image.Width - 1, y => y >= 0, x => x >= 0, false, -1);

        return new Rectangle(topLeftX, topLeftY, bottomRightX - topLeftX, bottomRightY - topLeftY);

        int F(int coordinateI, int coordinateJ, Func<int, bool> comparerI, Func<int, bool> comparerJ, bool horizontal, int increment)
        {
            var limit = 0;
            for (int i = coordinateI; comparerI(i); i += increment)
            {
                bool foundForegroundPixel = false;
                for (int j = coordinateJ; comparerJ(j); j += increment)
                {
                    var pixel = horizontal ? image[i, j] : image[j, i];
                    if (pixel != _backgroundColor)
                    {
                        foundForegroundPixel = true;
                        break;
                    }
                }

                if (foundForegroundPixel) break;
                limit = i;
            }

            return limit;
        }
    }
}
```

## DigitRecognizerService

```console
dotnet add package Newtonsoft.Json
dotnet add package SixLabors.ImageSharp
dotnet add package Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction
dotnet add reference ..\PreprocessingService\PreprocessingService.csproj
```

```csharp
public class Prediction
{
    public int Tag { get; set; }
    public double Probability { get; set; }
    public override string ToString() => JsonConvert.SerializeObject(this);
}
```

```csharp
public class CustomVisionDigitRecognizer
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
}
```

## Bot

```console
dotnet add reference ..\DigitRecognizerService\DigitRecognizerService.csproj
```

```csharp
public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
{
    var activity = turnContext.Activity;

    // Create a dialog context
    if (activity.Type == ActivityTypes.Message)
    {
        if (activity.Attachments != null && activity.Attachments.Any())
        {
            // We know the user is sending an attachment as there is at least one item
            // in the Attachments list.
            await HandleIncomingAttachmentAsync(turnContext, activity);
            return;
        }
    }
}
```

```csharp
/// <summary>
/// Handle attachments uploaded by users. The bot receives an <see cref="Attachment"/> in an <see cref="Activity"/>.
/// The activity has a <see cref="IList{T}"/> of attachments.
/// </summary>
/// <remarks>
/// Not all channels allow users to upload files. Some channels have restrictions
/// on file type, size, and other attributes. Consult the documentation for the channel for
/// more information. For example Skype's limits are here
/// <see ref="https://support.skype.com/en/faq/FA34644/skype-file-sharing-file-types-size-and-time-limits"/>.
/// </remarks>
private async Task HandleIncomingAttachmentAsync(DialogContext dc, IMessageActivity activity)
{
    foreach (var file in activity.Attachments)
    {
        if (file.ContentType != "image/png" && file.ContentType != "image/jpeg")
        {
            await dc.Context.SendActivityAsync("Sorry, I cannot process images other than png/jpeg.");
        }

        // Download the actual attachment
        using (var client = new HttpClient())
        {
            var stream = await client.GetStreamAsync(file.ContentUrl);
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var byteArray = memoryStream.ToArray();

            var recognizer = new CustomVisionDigitRecognizer(
                "<CustomVisionBaseUrl>",
                "<CustomVisionProjectId>",
                "<CustomVisionPublishedName>",
                "<CustomVisionApiKey>");

            var prediction = await recognizer.PredictAsync(byteArray);

            await SendPredictionAnswer(dc, prediction.Tag, prediction.Probability);
        }
    }
}

private static async Task SendPredictionAnswer(DialogContext dc, int digit, double probability)
{
    var digitWithArticle = string.Empty;
    switch (digit)
    {
        case 0:
            digitWithArticle = "a zero";
            break;
        case 1:
        case 2:
        case 3:
        case 4:
        case 5:
        case 6:
        case 7:
        case 9:
            digitWithArticle = $"a {digit}";
            break;
        case 8:
            digitWithArticle = $"an {digit}";
            break;
        default:
            break;
    }

    if (probability < 0.5)
    {
        await dc.Context.SendActivityAsync($"I'm not 100% sure, but I think this is {digitWithArticle}.");
        return;
    }

    var random = new Random();
    switch (random.Next(0, 4))
    {
        case 0:
            await dc.Context.SendActivityAsync($"I know! I know! It's {digitWithArticle}!");
            break;
        case 1:
            await dc.Context.SendActivityAsync($"OK, this should be {digitWithArticle}!");
            break;
        case 2:
            await dc.Context.SendActivityAsync($"This is {digitWithArticle}!");
            break;
        case 3:
            await dc.Context.SendActivityAsync($"Easy-peasy! This is {digitWithArticle}.");
            break;
    }
}
```
