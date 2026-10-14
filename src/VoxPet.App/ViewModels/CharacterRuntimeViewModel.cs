using System.Windows.Media;
using VoxPet.Core.Character;
using VoxPet.Core.Rendering;
using VoxPet.Core.Triggers;

namespace VoxPet.App.ViewModels;

/// <summary>UI가 소유하는 공통 Runtime 경계. tick은 메모리의 정의/이벤트/bitmap만 사용한다.</summary>
public sealed class CharacterRuntimeViewModel
{
    public CharacterRuntime Runtime { get; }
    public IReadOnlyDictionary<string, ImageSource> Images { get; private set; } = null!;
    public RenderFrame Frame => Runtime.Frame;
    public bool UseViewportTransform { get; set; }
    private readonly List<TriggerEvent> events = [];
    private double last;
    public CharacterRuntimeViewModel(CharacterRuntime runtime, IReadOnlyDictionary<string, ImageSource> images, double startedAt = 0)
    {
        Runtime = runtime; last = startedAt;
        SetImages(images);
    }
    public void SetImages(IReadOnlyDictionary<string, ImageSource> images)
    {
        if (images.Values.Any(image => !image.IsFrozen)) throw new ArgumentException("Runtime 이미지는 frozen이어야 합니다.");
        Images = new System.Collections.ObjectModel.ReadOnlyDictionary<string, ImageSource>(new Dictionary<string, ImageSource>(images, StringComparer.OrdinalIgnoreCase));
    }
    public void Input(TriggerEvent input) => events.Add(input);
    public RenderFrame Update(double now, double voice, bool? blink = null)
    {
        // 캐릭터 전환/종료 과정의 같은 tick 재사용도 허용한다.
        now = Math.Max(Math.Max(last, Runtime.Actions.LastTime), now);
        var frame = Runtime.Update(new(now, now - last, voice, blink, new InputSnapshot(events)));
        last = now; events.Clear(); return frame;
    }
    public void Stop() { events.Clear(); Runtime.StopActions(); }
}
