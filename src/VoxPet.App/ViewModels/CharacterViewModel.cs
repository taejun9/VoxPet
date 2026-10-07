using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.ViewModels;

/// <summary>두 창이 공유하는 frozen PNG와 표정·모션 상태. UI 스레드에서만 갱신한다.</summary>
public sealed class CharacterViewModel : ObservableObject
{
    private readonly Dictionary<ExpressionKind, ImageSource[,]> defaults = [];
    private ImageSource[,] sprites;
    private CharacterParameters current = new(0, 0, 0, 0, 1, MouthState.Closed);
    private ExpressionProfile profile = ExpressionProfile.Default(0);
    private string name = "Violet Cat · 기본 캐릭터";
    private ImageSource sprite;
    private ImageSource? previousSprite;
    private double bounce, blend = 1, transitionStart, now;
    private double tearLeftY, tearRightY, tearLeftOpacity, tearRightOpacity;

    public CharacterViewModel()
    {
        string[] mouths = ["closed", "half", "open"];
        foreach (var expression in Enum.GetValues<ExpressionKind>())
        {
            var sheet = new ImageSource[3, 2];
            string prefix = expression == ExpressionKind.Neutral ? "" : expression.ToString().ToLowerInvariant() + "-";
            for (int mouth = 0; mouth < 3; mouth++)
                for (int eye = 0; eye < 2; eye++)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri($"pack://application:,,,/Assets/Characters/{prefix}{mouths[mouth]}{(eye == 0 ? "-blink" : "")}.png");
                    bitmap.EndInit(); bitmap.Freeze(); sheet[mouth, eye] = bitmap;
                }
            defaults.Add(expression, sheet);
        }
        sprites = defaults[ExpressionKind.Neutral]; sprite = sprites[0, 1];
    }
    public string Name { get => name; private set => Set(ref name, value); }
    public ImageSource Sprite { get => sprite; private set => Set(ref sprite, value); }
    public ImageSource? PreviousSprite { get => previousSprite; private set => Set(ref previousSprite, value); }
    public double Blend { get => blend; private set { if (Set(ref blend, value)) Notify(nameof(PreviousOpacity)); } }
    public double PreviousOpacity => 1 - Blend;
    public double Bounce { get => bounce; private set => Set(ref bounce, value); }
    public double TearLeftX => profile.TearLeft * 512 - 5;
    public double TearRightX => profile.TearRight * 512 - 5;
    public double TearLeftY { get => tearLeftY; private set => Set(ref tearLeftY, value); }
    public double TearRightY { get => tearRightY; private set => Set(ref tearRightY, value); }
    public double TearLeftOpacity { get => tearLeftOpacity; private set => Set(ref tearLeftOpacity, value); }
    public double TearRightOpacity { get => tearRightOpacity; private set => Set(ref tearRightOpacity, value); }

    /// <summary>진행 중인 전환도 현재 합성 픽셀을 한 장으로 고정해 중첩 참조와 시각적인 점프를 방지한다.</summary>
    public void Apply(ExpressionProfile expression, ImageSource[,]? sheet = null)
    {
        expression.Validate();
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            if (PreviousSprite != null && Blend < 1)
            {
                drawing.PushOpacity(PreviousOpacity); drawing.DrawImage(PreviousSprite, new Rect(0, 0, 512, 512)); drawing.Pop();
            }
            drawing.PushOpacity(Blend); drawing.DrawImage(Sprite, new Rect(0, 0, 512, 512)); drawing.Pop();
            // 떠나는 표정의 눈물도 전환 snapshot에 포함해 갑자기 사라지지 않게 한다.
            DrawTear(drawing, TearLeftX, TearLeftY, TearLeftOpacity);
            DrawTear(drawing, TearRightX, TearRightY, TearRightOpacity);
        }
        var snapshot = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32);
        snapshot.Render(visual); snapshot.Freeze();
        PreviousSprite = snapshot; transitionStart = now; Blend = 0;
        profile = expression; sprites = sheet ?? defaults[expression.Kind];
        Name = sheet == null ? $"Violet Cat · {expression.Name}" : $"{expression.Name} · 사용자 캐릭터";
        Notify(nameof(TearLeftX)); Notify(nameof(TearRightX)); Update(current, now);
    }
    private static void DrawTear(DrawingContext drawing, double x, double y, double opacity)
    {
        if (opacity <= 0) return;
        drawing.PushOpacity(opacity); drawing.PushTransform(new TranslateTransform(x, y));
        drawing.DrawGeometry(Brushes.LightSkyBlue, null, Geometry.Parse("M5,0 C4,5 0,8 0,12 C0,20 10,20 10,12 C10,8 6,5 5,0 Z"));
        drawing.Pop(); drawing.Pop();
    }
    // 기존 일반 PNG 불러오기 경로는 현재 실행의 표정으로 적용한다. 슬롯 저장은 명시적 저장 버튼을 사용한다.
    public void UseSheet(ImageSource[,] sheet, string displayName) => Apply(new(displayName.Length > 40 ? displayName[..40] : displayName), sheet);
    public void RestoreDefault() => Apply(ExpressionProfile.Default(0));
    public void Update(CharacterParameters state) => Update(state, now);
    public void Update(CharacterParameters state, double seconds)
    {
        current = state; now = seconds;
        Sprite = sprites[(int)state.Mouth, profile.Blink && state.EyeOpen < .5 ? 0 : 1];
        Bounce = -state.BodyBounce;
        Blend = PreviousSprite == null ? 1 : ExpressionMotion.Blend(now - transitionStart);
        if (Blend >= 1) PreviousSprite = null;
        var left = ExpressionMotion.Tear(now);
        var right = ExpressionMotion.Tear(now, .45);
        TearLeftY = profile.TearTop * 512 + left.Offset;
        TearRightY = profile.TearTop * 512 + right.Offset;
        TearLeftOpacity = profile.Tears ? left.Opacity * Blend : 0;
        TearRightOpacity = profile.Tears ? right.Opacity * Blend : 0;
    }
    /// <summary>저장용 3×2 RGBA PNG를 작업 스레드에서 인코딩한다. 모든 원본 이미지는 frozen이다.</summary>
    public static byte[] EncodeSheet(ImageSource[,] sheet)
    {
        int cell = (int)sheet[0, 0].Width;
        var pixels = new byte[cell * 3 * cell * 2 * 4];
        for (int mouth = 0; mouth < 3; mouth++)
            for (int row = 0; row < 2; row++)
            {
                var image = new FormatConvertedBitmap((BitmapSource)sheet[mouth, row == 0 ? 1 : 0], PixelFormats.Bgra32, null, 0);
                var source = new byte[cell * cell * 4]; image.CopyPixels(source, cell * 4, 0);
                for (int y = 0; y < cell; y++)
                    Buffer.BlockCopy(source, y * cell * 4, pixels, ((row * cell + y) * cell * 3 + mouth * cell) * 4, cell * 4);
            }
        var bitmap = BitmapSource.Create(cell * 3, cell * 2, 96, 96, PixelFormats.Bgra32, null, pixels, cell * 3 * 4);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream(); encoder.Save(stream); return stream.ToArray();
    }
}
