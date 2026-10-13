using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoxPet.Core.Models;

// 화면 기준 좌우. 슬롯 숫자는 고정된 뒤→앞 렌더 순서다.
public enum ImagePart { CapeBack, ArmL, LegL, LegR, Body, Lower, Upper, Shoes, Head, Eyes, Brows, Mouth, FaceDecoration, ArmR, Weapon, Effect }
public enum ImageAnchor { Canvas, Head, Eye, Face, Mouth }
public enum ActionTemplate { Gesture, OneHand, TwoHand, Firearm, Bow }
public sealed record AnchorOffset(double X = 0, double Y = 0);
public sealed record PartImage(ImagePart Part, string File);
public sealed record FaceImages(ExpressionKind Kind, string EyesOpen, string EyesClosed, string? Brows = null);
public sealed record PoseLayer(ImagePart Part, ImageAnchor Anchor, Dictionary<string, string> Poses);
public sealed record OutfitImages(string Name, PoseLayer[] Layers);
public sealed record WeaponImages(string Name, ActionTemplate Template, PoseLayer[] Layers);
public sealed record ImageActionFrame(int DurationMs, string Pose, Dictionary<ImagePart, string?> Replace,
    Dictionary<ImageAnchor, AnchorOffset>? Anchors = null);
public sealed record ImageAction(string Name, ActionTemplate Template, ImageActionFrame[] Frames);

/// <summary>준비된 투명 PNG만 조합한다. 위치·회전·리깅 편집은 런타임에 포함하지 않는다.</summary>
public sealed record ImageSet(int Version, string CharacterId, int CanvasSize, PartImage[] Parts, string[] Mouths,
    FaceImages[] Expressions, OutfitImages[] Outfits, WeaponImages[] Weapons, ImageAction[] Actions,
    AnchorOffset[]? MouthOffsets = null)
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    public IEnumerable<string> Files => Parts.Select(p => p.File).Concat(Mouths)
        .Concat(Expressions.SelectMany(e => new[] { e.EyesOpen, e.EyesClosed, e.Brows }.OfType<string>()))
        .Concat(Outfits.SelectMany(o => o.Layers).Concat(Weapons.SelectMany(w => w.Layers)).SelectMany(l => l.Poses.Values))
        .Concat(Actions.SelectMany(a => a.Frames).SelectMany(f => f.Replace.Values.OfType<string>())).Distinct(StringComparer.OrdinalIgnoreCase);
    public void Validate()
    {
        if (Version != 1 || !CharacterCatalog.All.Any(c => c.Id == CharacterId) || CanvasSize is not (256 or 512 or 1024)) Fail("버전1 / 늘보군·펠리 / 256·512·1024 Canvas를 사용하세요.");
        if (Parts == null || Mouths == null || Expressions == null || Outfits == null || Weapons == null || Actions == null) Fail("필수 이미지 목록이 없습니다.");
        if (Parts!.Length > 16 || Parts.Any(p => p == null || !Enum.IsDefined(p.Part)) || Parts.Select(p => p.Part).Distinct().Count() != Parts.Length) Fail("기본 파츠 슬롯이 중복되거나 잘못되었습니다.");
        ImagePart[] required = [ImagePart.Head, ImagePart.Body, ImagePart.ArmL, ImagePart.ArmR, ImagePart.LegL, ImagePart.LegR];
        if (required.Except(Parts.Select(p => p.Part)).Any() || Parts.Any(p => p.Part is ImagePart.Eyes or ImagePart.Brows or ImagePart.Mouth or ImagePart.Weapon or ImagePart.Effect)) Fail("Head / Body / ArmL / ArmR / LegL / LegR를 각각 등록하세요. 얼굴·무기는 별도 목록입니다.");
        if (Mouths!.Length is not (3 or 8) || Expressions!.Length is < 1 or > 12 || Expressions.Any(e => e == null || !Enum.IsDefined(e.Kind)) || Expressions.Select(e => e.Kind).Distinct().Count() != Expressions.Length || !Expressions.Any(e => e.Kind == ExpressionKind.Neutral)) Fail("입3·8단계와 중복 없는 기본 표정이 필요합니다.");
        foreach (var face in Expressions) { ValidateFile(face.EyesOpen); ValidateFile(face.EyesClosed); }
        if (Outfits!.Length > 16 || Weapons!.Length > 16 || Actions!.Length > 16) Fail("의상·무기·액션은 각각16개까지 등록합니다.");
        foreach (var action in Actions)
        {
            if (action == null || !Enum.IsDefined(action.Template)) Fail("액션 템플릿이 잘못되었습니다.");
            Name(action!.Name);
            if (action.Frames == null || action.Frames.Length is < 1 or > 64) Fail("액션은1~64프레임이어야 합니다.");
            foreach (var frame in action.Frames!)
            {
                if (frame == null || frame.DurationMs is < 16 or > 2000 || frame.Replace == null) Fail("프레임은16~2000ms이며 교체 목록이 필요합니다.");
                Pose(frame!.Pose);
                if (frame.Replace.Keys.Any(p => p is not (ImagePart.ArmL or ImagePart.ArmR or ImagePart.Body or ImagePart.Head or ImagePart.LegL or ImagePart.LegR))) Fail("액션은 캐릭터 파츠만 교체합니다. 얼굴·무기·의상은 담당 목록에서 조합합니다.");
                if (frame.Anchors != null) foreach (var pair in frame.Anchors) { if (!Enum.IsDefined(pair.Key)) Fail("Anchor를 확인하세요."); Offset(pair.Value); }
            }
            if (action.Frames.Sum(f => f.DurationMs) > 30000) Fail("액션은30초 이내입니다.");
        }
        foreach (var outfit in Outfits) { if (outfit == null) Fail("의상을 확인하세요."); Name(outfit!.Name); Layers(outfit.Layers, false); }
        foreach (var weapon in Weapons)
        {
            if (weapon == null || !Enum.IsDefined(weapon.Template)) Fail("무기를 확인하세요.");
            Name(weapon.Name); Layers(weapon.Layers, true);
            var body = weapon.Layers.Single(l => l.Part == ImagePart.Weapon);
            foreach (var frame in Actions.Where(a => a.Template == weapon.Template).SelectMany(a => a.Frames))
                if (!body.Poses.ContainsKey(frame.Pose)) Fail($"{weapon.Name}에 {frame.Pose} 포즈가 필요합니다.");
        }
        if (MouthOffsets != null) { if (MouthOffsets.Length != Mouths.Length) Fail("입 Anchor는 입 단계와 개수가 같아야 합니다."); foreach (var offset in MouthOffsets) Offset(offset); }
        foreach (string file in Files) ValidateFile(file);
        if (Files.Count() > 128) Fail("한 세트는128개의 PNG까지 사용합니다.");
    }
    private void Layers(PoseLayer[]? layers, bool weapon)
    {
        if (layers == null || layers.Length is < 1 or > 16 || layers.Any(l => l == null) || layers.Select(l => l.Part).Distinct().Count() != layers.Length) Fail("이미지 레이어 슬롯을 확인하세요.");
        foreach (var layer in layers!)
        {
            if (!Enum.IsDefined(layer.Part) || !Enum.IsDefined(layer.Anchor) || layer.Poses == null || layer.Poses.Count is < 1 or > 64 || !layer.Poses.ContainsKey("idle")) Fail("레이어마다 idle 포즈가 필요합니다.");
            if (weapon ? layer.Part is not (ImagePart.Weapon or ImagePart.Effect) : layer.Part is not (ImagePart.CapeBack or ImagePart.ArmL or ImagePart.ArmR or ImagePart.Lower or ImagePart.Upper or ImagePart.Shoes or ImagePart.FaceDecoration)) Fail("의상·무기의 담당 슬롯을 확인하세요.");
            foreach (string pose in layer.Poses!.Keys) Pose(pose);
            // 교체된 팔 위에 평상 소매가 남지 않게 영향을 받는 모든 포즈를 요구한다.
            if (!weapon)
                foreach (var frame in Actions.SelectMany(a => a.Frames).Where(f => Targets(layer.Part).Any(p => f.Replace.ContainsKey(p) && f.Replace[p] != null)))
                    if (!layer.Poses.ContainsKey(frame.Pose)) Fail($"{layer.Part} 소매에 {frame.Pose} 포즈가 필요합니다.");
        }
        if (weapon && !layers.Any(l => l.Part == ImagePart.Weapon)) Fail("무기 본체 레이어가 필요합니다.");
    }
    public static ImagePart[] Targets(ImagePart part) => part switch
    {
        ImagePart.Upper or ImagePart.Lower => [ImagePart.Body],
        ImagePart.Shoes => [ImagePart.LegL, ImagePart.LegR],
        _ => [part]
    };
    public static void ValidateFile(string? file)
    {
        if (string.IsNullOrWhiteSpace(file) || file.Length > 180 || file.StartsWith('/') || file.Contains('\\') || file.Contains(':') || file.Any(char.IsControl) || !file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || file.Split('/').Any(p => p is "" or "." or "..")) Fail("PNG 경로는 세트 안의 상대 경로여야 합니다.");
    }
    private static void Name(string? name) { if (string.IsNullOrWhiteSpace(name) || name.Length > 40 || name.Any(char.IsControl)) Fail("이름은1~40자입니다."); }
    private static void Pose(string? pose) { if (string.IsNullOrWhiteSpace(pose) || pose.Length > 32 || pose.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_')) Fail("포즈 이름은 영문·숫자·-·_를 사용하세요."); }
    private static void Offset(AnchorOffset? value) { if (value == null || !double.IsFinite(value.X) || !double.IsFinite(value.Y) || Math.Abs(value.X) > 128 || Math.Abs(value.Y) > 128) Fail("Anchor 이동은 유한한±128px 이내입니다."); }
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail(string message) => throw new ArgumentException(message);
}
