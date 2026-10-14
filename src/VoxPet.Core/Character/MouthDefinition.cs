namespace VoxPet.Core.Character;

public sealed record MouthDefinition
{
    public string Anchor { get; }
    public string Curve { get; }
    public IReadOnlyList<string> Frames { get; }
    public IReadOnlyList<Transform2D> FrameTransforms { get; }
    public MouthDefinition(IEnumerable<string> frames, string anchor = "mouth", string curve = "smooth", IEnumerable<Transform2D>? frameTransforms = null)
    {
        Anchor = DefinitionValidation.Id(anchor, nameof(anchor));
        if (curve is not ("smooth" or "linear" or "legacy3" or "legacy8")) throw new ArgumentException("입 곡선을 확인하세요.", nameof(curve));
        Curve = curve; Frames = DefinitionValidation.Copy(frames, nameof(frames));
        if (Frames.Count is < 1 or > 16 || curve == "legacy3" && Frames.Count != 3 || curve == "legacy8" && Frames.Count != 8) throw new ArgumentException("입은1~16프레임이어야 합니다.");
        foreach (string asset in Frames) DefinitionValidation.Asset(asset, ".png", nameof(frames));
        FrameTransforms = DefinitionValidation.Copy(frameTransforms ?? Enumerable.Repeat(Transform2D.Identity, Frames.Count), nameof(frameTransforms));
        if (FrameTransforms.Count != Frames.Count) throw new ArgumentException("입 이동은 프레임 수와 같아야 합니다.");
    }
}
