using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Character;
using VoxPet.Core.Rendering;

namespace VoxPet.App.Services;

/// <summary>Bounded frozen face atlas, prepared from local pixels on one cancellable worker.</summary>
internal sealed class FaceBlendBank(SpriteSheetDefinition sheet, bool side) : IDisposable
{
    private sealed record Prepared(SpriteSheetExpression Expression, IReadOnlyDictionary<string, ImageSource> Images);
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly System.Windows.Threading.Dispatcher owner = System.Windows.Application.Current.Dispatcher;
    private IReadOnlyDictionary<string, ImageSource> source = new Dictionary<string, ImageSource>();
    private ImageSource[] signature = [];
    private SpriteSheetExpression? wanted;
    private Prepared? prepared;
    private CancellationTokenSource? cancellation;
    private int revision;
    private bool disposed, eyeInitialized, closed;
    private double eyeFrom = 1, eyeTarget = 1, eyeStarted;
    public Task Ready { get; private set; } = Task.CompletedTask;
    public string? Error { get; private set; }
    public bool IsPrepared => prepared != null;
    public IReadOnlyDictionary<string, ImageSource> Images { get; private set; } = new Dictionary<string, ImageSource>();
    public event Action? Changed;
    public void SetImages(IReadOnlyDictionary<string, ImageSource> images)
    {
        source = images; Images = Merge(images, prepared?.Images);
        Ensure(wanted ?? sheet.Expressions[0]);
    }
    private static IReadOnlyDictionary<string, ImageSource> Merge(IReadOnlyDictionary<string, ImageSource> original, IReadOnlyDictionary<string, ImageSource>? additional)
    {
        var merged = new Dictionary<string, ImageSource>(original, StringComparer.OrdinalIgnoreCase);
        if (additional != null) foreach (var pair in additional) merged[pair.Key] = pair.Value;
        return merged;
    }
    private void Ensure(SpriteSheetExpression expression)
    {
        if (disposed) return;
        var keys = expression.Closed.Concat(expression.Open).ToArray();
        if (keys.Any(key => !source.ContainsKey(key))) return;
        var inputs = keys.Select(key => source[key]).ToArray();
        if (wanted == expression && inputs.SequenceEqual(signature)) return;
        cancellation?.Cancel(); cancellation?.Dispose(); cancellation = new();
        wanted = expression; signature = inputs; prepared = null; Error = null; eyeInitialized = false;
        int version = ++revision; var token = cancellation.Token; Images = source;
        Ready = PrepareAsync(expression, inputs, version, token);
    }
    private async Task PublishAsync(int version, Action change)
    {
        if (disposed || owner.HasShutdownStarted) return;
        try
        {
            await owner.InvokeAsync(() => { if (!disposed && version == revision) change(); }).Task;
        }
        catch (OperationCanceledException) { }
    }
    private async Task PrepareAsync(SpriteSheetExpression expression, ImageSource[] inputs, int version, CancellationToken token)
    {
        bool acquired = false;
        try
        {
            await gate.WaitAsync(token); acquired = true;
            var result = await Task.Run(() => Build(expression, inputs, side, token), token);
            await PublishAsync(version, () => { prepared = result; Images = Merge(source, result.Images); Changed?.Invoke(); });
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or NotSupportedException)
        { await PublishAsync(version, () => { Error = error.GetType().Name; Changed?.Invoke(); }); }
        finally { if (acquired) gate.Release(); }
    }
    private static byte[] Pixels(ImageSource image)
    {
        if (image is not BitmapSource { PixelWidth: 512, PixelHeight: 512 } bitmap || bitmap.Format != PixelFormats.Pbgra32 && bitmap.Format != PixelFormats.Bgra32)
            throw new ArgumentException("얼굴 전환에는512px RGBA 파츠가 필요합니다.");
        var pixels = new byte[512 * 512 * 4]; bitmap.CopyPixels(pixels, 2048, 0);
        if (bitmap.Format == PixelFormats.Bgra32)
            for (int i = 0; i < pixels.Length; i += 4) for (int c = 0; c < 3; c++) pixels[i + c] = (byte)((pixels[i + c] * pixels[i + 3] + 127) / 255);
        return pixels;
    }
    private static BitmapSource Bitmap(byte[] pixels)
    {
        var bitmap = BitmapSource.Create(512, 512, 96, 96, PixelFormats.Pbgra32, null, pixels, 2048); bitmap.Freeze(); return bitmap;
    }
    private static byte[] Register(byte[] pixels, byte[] reference, bool side)
    {
        // Forehead/skull excludes changing eyes and beak. Only integer translation
        // is fitted; original face proportions and the mouth's outline are kept.
        int x0 = side ? 265 : 100, x1 = side ? 405 : 415, y0 = side ? 85 : 45, y1 = side ? 235 : 170;
        long Score(int dx, int dy)
        {
            long score = 0;
            for (int y = y0; y < y1; y += 6) for (int x = x0; x < x1; x += 6)
            {
                int a = (y * 512 + x) * 4, b = ((y - dy) * 512 + x - dx) * 4;
                for (int c = 0; c < 4; c++) score += Math.Abs(reference[a + c] - pixels[b + c]);
            }
            return score;
        }
        long original = Score(0, 0), best = original; int shiftX = 0, shiftY = 0;
        for (int dy = -3; dy <= 3; dy++) for (int dx = -3; dx <= 3; dx++)
        { long score = Score(dx, dy); if (score < best) { best = score; shiftX = dx; shiftY = dy; } }
        if (best >= original * .98 || shiftX == 0 && shiftY == 0) return pixels;
        var aligned = new byte[pixels.Length]; int width = 512 - Math.Abs(shiftX);
        for (int y = Math.Max(0, shiftY); y < Math.Min(512, 512 + shiftY); y++)
            Buffer.BlockCopy(pixels, ((y - shiftY) * 512 + Math.Max(0, -shiftX)) * 4, aligned, (y * 512 + Math.Max(0, shiftX)) * 4, width * 4);
        return aligned;
    }
    private static string Key(SpriteSheetExpression e, int mouth, int eye) => "avatar-face/" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(e.Id)) + $"-{mouth}-{eye}.png";
    private static Prepared Build(SpriteSheetExpression e, ImageSource[] inputs, bool side, CancellationToken token)
    {
        int count = e.Open.Count; var original = new byte[inputs.Length][];
        for (int i = 0; i < original.Length; i++) { token.ThrowIfCancellationRequested(); original[i] = Pixels(inputs[i]); }
        var reference = original[count]; var pixels = new byte[original.Length][];
        for (int i = 0; i < original.Length; i++) { token.ThrowIfCancellationRequested(); pixels[i] = Register(original[i], reference, side); }
        var images = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        for (int eye = 0; eye < 4; eye++) for (int mouth = 0; mouth < count; mouth++)
        {
            token.ThrowIfCancellationRequested();
            var mixed = new byte[512 * 512 * 4];
            for (int i = 0; i < mixed.Length; i++)
            {
                mixed[i] = (byte)((pixels[mouth][i] * (3 - eye) + pixels[count + mouth][i] * eye + 1) / 3);
            }
            string key = eye is 0 or 3 ? (eye == 0 ? e.Closed[mouth] : e.Open[mouth]) : Key(e, mouth, eye);
            images[key] = Bitmap(mixed);
        }
        return new(e, images);
    }
    public RenderFrame Update(RenderFrame frame, double time)
    {
        var face = frame.Layers.FirstOrDefault(l => l.Id == "part-" + sheet.Part);
        if (face == null) return frame;
        var expression = sheet.Expressions.FirstOrDefault(e => e.Open.Contains(face.Asset) || e.Closed.Contains(face.Asset));
        if (expression == null) return frame; // Authored sprite overrides remain authoritative.
        Ensure(expression); bool shutting = expression.Blink && expression.Closed.Contains(face.Asset);
        double duration = eyeTarget == 0 ? .045 : .105, u = Math.Clamp((time - eyeStarted) / duration, 0, 1);
        double eye = eyeFrom + (eyeTarget - eyeFrom) * u * u * (3 - 2 * u);
        if (!eyeInitialized) { eye = eyeFrom = eyeTarget = shutting ? 0 : 1; closed = shutting; eyeInitialized = true; }
        else if (shutting != closed) { closed = shutting; eyeFrom = eye; eyeTarget = shutting ? 0 : 1; eyeStarted = time; }
        if (prepared?.Expression != expression) return frame;
        // Keep the runtime's authored voice curve and selected crisp mouth pose.
        int mouth = Enumerable.Range(0, expression.Open.Count).First(i => expression.Open[i] == face.Asset || expression.Closed[i] == face.Asset);
        int phase = (int)Math.Round(eye * 3);
        string asset = phase is 0 or 3 ? (phase == 0 ? expression.Closed[mouth] : expression.Open[mouth]) : Key(expression, mouth, phase);
        var layers = frame.Layers.Select(l => l.Id != face.Id ? l : new RenderLayer(l.Id, asset, opacity: l.Opacity, zIndex: l.ZIndex, matrix: l.Matrix, width: l.Width, height: l.Height, mesh: l.Mesh));
        return new(layers, frame.CanvasWidth, frame.CanvasHeight, frame.ViewportTransform);
    }
    public void Dispose()
    { if (disposed) return; disposed = true; revision++; cancellation?.Cancel(); cancellation?.Dispose(); prepared = null; }
}
