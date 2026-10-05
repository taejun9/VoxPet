using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Models;

namespace VoxPet.App.ViewModels;

/// <summary>One shared character snapshot for the settings preview and broadcast window.</summary>
public sealed class CharacterViewModel : ObservableObject
{
    private readonly ImageSource[,] sprites = new ImageSource[3, 2];
    private ImageSource sprite;
    private double bounce;
    public CharacterViewModel()
    {
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
    public ImageSource Sprite { get => sprite; private set => Set(ref sprite, value); }
    public double Bounce { get => bounce; private set => Set(ref bounce, value); }
    public void Update(CharacterParameters state)
    {
        Sprite = sprites[(int)state.Mouth, state.EyeOpen < .5 ? 0 : 1];
        Bounce = -state.BodyBounce;
    }
}
