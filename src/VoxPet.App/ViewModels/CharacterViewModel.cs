using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Models;

namespace VoxPet.App.ViewModels;

/// <summary>설정 미리보기와 방송창이 함께 바인딩하는 단일 캐릭터 상태. UI 스레드에서 갱신한다.</summary>
public sealed class CharacterViewModel : ObservableObject
{
    // 두 배열 모두 [입 0~2, 눈 0=감음/1=뜸] 순서다. defaults를 보관해 외부 시트 적용 후 복원을 지원한다.
    private readonly ImageSource[,] defaults = new ImageSource[3, 2];
    private ImageSource[,] sprites;
    private CharacterParameters current = new(0, 0, 0, 0, 1, MouthState.Closed);
    private string name = "Violet Cat · 기본 캐릭터";
    private ImageSource sprite;
    private double bounce;
    /// <summary>
    /// 내장 PNG를 pack URI로 읽어 Freeze한다. 두 창에서 같은 immutable 이미지 자원을 공유한다.
    /// </summary>
    public CharacterViewModel()
    {
        sprites = defaults;
        string[] names = ["closed", "half", "open"];
        for (int mouth = 0; mouth < 3; mouth++)
            for (int eye = 0; eye < 2; eye++)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri($"pack://application:,,,/Assets/Characters/{names[mouth]}{(eye == 0 ? "-blink" : "")}.png");
                bitmap.EndInit(); bitmap.Freeze(); sprites[mouth, eye] = bitmap;
            }
        sprite = sprites[0, 1];
    }
    public string Name { get => name; private set => Set(ref name, value); }
    /// <summary>
    /// 로더가 완성한 시트를 한 번에 교체하고 마지막 애니메이션 상태를 새 이미지에 즉시 적용한다.
    /// </summary>
    public void UseSheet(ImageSource[,] sheet, string displayName)
    {
        sprites = sheet; Name = $"{displayName} · 사용자 캐릭터"; Update(current);
    }
    /// <summary>
    /// 현재 입·눈 상태는 유지하면서 내장 캐릭터 배열로 복원한다.
    /// </summary>
    public void RestoreDefault()
    {
        sprites = defaults; Name = "Violet Cat · 기본 캐릭터"; Update(current);
    }
    public ImageSource Sprite { get => sprite; private set => Set(ref sprite, value); }
    public double Bounce { get => bounce; private set => Set(ref bounce, value); }
    /// <summary>
    /// 입/눈 상태로 PNG를 고르고 WPF의 아래쪽 양수 좌표를 반전해 몸이 위로 움직이게 한다.
    /// </summary>
    public void Update(CharacterParameters state)
    {
        current = state;
        Sprite = sprites[(int)state.Mouth, state.EyeOpen < .5 ? 0 : 1];
        Bounce = -state.BodyBounce;
    }
}
