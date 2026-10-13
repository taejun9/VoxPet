using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public sealed record ComposedImage(ImagePart Part, string File, AnchorOffset Offset, bool Costume = false);

/// <summary>파일·UI에 의존하지 않는 조합기. 클릭은 즉시 재시작하며 종료 시 idle로 복귀한다.</summary>
public sealed class ImageSetPlayer
{
    private readonly ImageSet set;
    private int? playing;
    private double started;
    public ImageSetPlayer(ImageSet set) { set.Validate(); this.set = set; }
    public bool Play(int index, double seconds)
    {
        if (index < 0 || index >= set.Actions.Length || !double.IsFinite(seconds)) return false;
        playing = index; started = seconds; return true;
    }
    public void Stop() => playing = null;
    public ImageActionFrame? Frame(double seconds)
    {
        if (playing is not { } index || !double.IsFinite(seconds)) return null;
        double elapsed = Math.Max(0, seconds - started) * 1000;
        foreach (var frame in set.Actions[index].Frames) { if (elapsed < frame.DurationMs) return frame; elapsed -= frame.DurationMs; }
        playing = null; return null;
    }
    public IReadOnlyList<ComposedImage> Compose(double seconds, double level, bool blink, ExpressionKind expression, int outfit = -1, int weapon = -1)
    {
        var frame = Frame(seconds); string pose = frame?.Pose ?? "idle";
        int mouth = set.Mouths.Length == 8 ? MouthFrames.Select(level) : !double.IsFinite(level) || level < .2 ? 0 : level < .6 ? 1 : 2;
        var face = set.Expressions.FirstOrDefault(e => e.Kind == expression) ?? set.Expressions.First(e => e.Kind == ExpressionKind.Neutral);
        var images = new List<ComposedImage>();
        AnchorOffset Anchor(ImageAnchor anchor)
        {
            var head = frame?.Anchors?.GetValueOrDefault(ImageAnchor.Head) ?? new();
            var own = anchor == ImageAnchor.Head ? new AnchorOffset() : frame?.Anchors?.GetValueOrDefault(anchor) ?? new();
            var talking = anchor == ImageAnchor.Mouth ? set.MouthOffsets?[mouth] ?? new() : new AnchorOffset();
            return anchor == ImageAnchor.Canvas ? new() : new(head.X + own.X + talking.X, head.Y + own.Y + talking.Y);
        }
        foreach (var part in set.Parts)
        {
            string? file = frame != null && frame.Replace.TryGetValue(part.Part, out var replacement) ? replacement : part.File;
            if (file != null) images.Add(new(part.Part, file, Anchor(part.Part == ImagePart.Head ? ImageAnchor.Head : ImageAnchor.Canvas)));
        }
        // Head가 숨겨진 프레임에서는 얼굴과 장식도 숨겨진다.
        bool headVisible = images.Any(i => i.Part == ImagePart.Head);
        if (headVisible)
        {
            images.Add(new(ImagePart.Eyes, blink ? face.EyesClosed : face.EyesOpen, Anchor(ImageAnchor.Eye)));
            if (face.Brows != null) images.Add(new(ImagePart.Brows, face.Brows, Anchor(ImageAnchor.Face)));
            images.Add(new(ImagePart.Mouth, set.Mouths[mouth], Anchor(ImageAnchor.Mouth)));
        }
        if (outfit >= 0 && outfit < set.Outfits.Length)
            foreach (var layer in set.Outfits[outfit].Layers)
            {
                if (!headVisible && layer.Anchor != ImageAnchor.Canvas || frame != null && ImageSet.Targets(layer.Part).Any(p => frame.Replace.TryGetValue(p, out var file) && file == null)) continue;
                // 소매 포즈는 검증으로 보장한다. 정적 몸통/장식은 idle을 유지한다.
                string image = layer.Poses.GetValueOrDefault(pose) ?? layer.Poses["idle"];
                images.Add(new(layer.Part, image, Anchor(layer.Anchor), true));
            }
        if (weapon >= 0 && weapon < set.Weapons.Length)
        {
            var item = set.Weapons[weapon];
            bool compatible = playing == null || item.Template == set.Actions[playing.Value].Template;
            if (compatible)
                foreach (var layer in item.Layers)
                {
                    string? image = layer.Poses.GetValueOrDefault(pose);
                    // 없는 발사 포즈에 평상 무기를 비스듬히 회전시켜 대체하지 않는다.
                    if (image != null && (layer.Part != ImagePart.Effect || frame != null)) images.Add(new(layer.Part, image, Anchor(layer.Anchor)));
                }
        }
        return images.OrderBy(i => i.Part).ThenBy(i => i.Costume).ToArray();
    }
}
