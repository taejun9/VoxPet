using VoxPet.Core.Character;

namespace VoxPet.Core.Rendering;

/// <summary>렌더러가 그대로 표시하는 최종 논리 좌표. UI 타입이나 파일 내용은 보관하지 않는다.</summary>
public sealed record RenderLayer
{
    public string Id { get; }
    public string Asset { get; }
    public Transform2D Transform { get; }
    public double X => Transform.X;
    public double Y => Transform.Y;
    public double ScaleX => Transform.ScaleX;
    public double ScaleY => Transform.ScaleY;
    public double Rotation => Transform.Rotation;
    public double Opacity { get; }
    public int ZIndex { get; }

    public RenderLayer(string id, string asset, Transform2D? transform = null, double opacity = 1, int zIndex = 0)
    {
        Id = DefinitionValidation.Id(id, nameof(id));
        Asset = DefinitionValidation.Asset(asset, ".png", nameof(asset));
        Transform = transform ?? Transform2D.Identity;
        Opacity = DefinitionValidation.Unit(opacity, nameof(opacity));
        ZIndex = zIndex;
    }
}
