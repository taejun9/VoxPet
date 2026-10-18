using System.Windows.Media;
using VoxPet.Core.Character;
using VoxPet.Core.Rendering;
using VoxPet.Core.Triggers;
using VoxPet.App.Services;
using VoxPet.Core.Motion;

namespace VoxPet.App.ViewModels;

/// <summary>UI가 소유하는 공통 Runtime 경계. tick은 메모리의 정의/이벤트/bitmap만 사용한다.</summary>
public sealed class CharacterRuntimeViewModel
{
    public CharacterRuntime Runtime { get; }
    public IReadOnlyDictionary<string, ImageSource> Images { get; private set; } = null!;
    private RenderFrame? displayFrame;
    private FaceBlendBank? faces;
    private readonly bool wantsFaces, sideView;
    private IReadOnlyDictionary<string, ImageSource> sourceImages = new Dictionary<string, ImageSource>();
    internal IReadOnlyDictionary<string, ImageSource> SourceImages => sourceImages;
    public RenderFrame Frame => displayFrame ?? Runtime.Frame;
    internal Task FacePreparation => faces?.Ready ?? Task.CompletedTask;
    internal bool FacePrepared => faces?.IsPrepared == true;
    internal string? FaceError => faces?.Error;
    internal event Action? FacePreparationChanged;
    public bool UseViewportTransform { get; set; }
    private readonly List<TriggerEvent> events = [];
    internal int PendingInputCount => events.Count;
    private double last;
    public CharacterRuntimeViewModel(CharacterRuntime runtime, IReadOnlyDictionary<string, ImageSource> images, double startedAt = 0, bool interpolateFace = false, bool sideView = false)
    {
        Runtime = runtime; last = startedAt;
        wantsFaces = interpolateFace || runtime.Definition.Motion.Acting != CompanionActing.None;
        this.sideView = sideView || runtime.Definition.Motion.Acting == CompanionActing.Pelly;
        SetImages(images);
    }
    private bool ActivateFaces()
    {
        if (faces != null || !wantsFaces || Runtime.Definition.SpriteSheet is not { } sheet || System.Windows.Application.Current?.Dispatcher.CheckAccess() != true) return false;
        faces = new(sheet, sideView);
        faces.Changed += () =>
        {
            Images = faces.Images; displayFrame = faces.Update(Runtime.Frame, Runtime.State.VoiceLevel, last);
            FacePreparationChanged?.Invoke();
        };
        return true;
    }
    public void SetImages(IReadOnlyDictionary<string, ImageSource> images)
    {
        if (images.Values.Any(image => !image.IsFrozen)) throw new ArgumentException("Runtime 이미지는 frozen이어야 합니다.");
        sourceImages = new System.Collections.ObjectModel.ReadOnlyDictionary<string, ImageSource>(new Dictionary<string, ImageSource>(images, StringComparer.OrdinalIgnoreCase));
        Images = sourceImages; ActivateFaces();
        if (faces != null) { faces.SetImages(sourceImages); Images = faces.Images; }
    }
    public void Input(TriggerEvent input)
    {
        // UI가 지연된 동안 고빈도 입력이 메모리와 다음 frame 작업량을 늘리지 않게 한다.
        if (events.Count < 128) events.Add(input);
    }
    public RenderFrame Update(double now, double voice, bool? blink = null)
    {
        // 캐릭터 전환/종료 과정의 같은 tick 재사용도 허용한다.
        now = Math.Max(Math.Max(last, Runtime.Actions.LastTime), now);
        if (ActivateFaces()) faces!.SetImages(sourceImages);
        var frame = Runtime.Update(new(now, now - last, voice, blink, new InputSnapshot(events)));
        last = now; events.Clear();
        if (faces != null) { displayFrame = faces.Update(frame, voice, now); Images = faces.Images; }
        return Frame;
    }
    public void Stop() { events.Clear(); Runtime.StopActions(); }
    internal void ReleaseFacePreparation() => faces?.Dispose();
}
