using System.Collections.ObjectModel;
using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

/// <summary>부모 순서대로 합성한다. static은 생성 시 계산하며 tick에는 IO가 없다.</summary>
public sealed class RigMotionSolver
{
    private readonly RigDefinition definition;
    public IReadOnlyDictionary<string, AffineTransform2D> Static { get; }
    public RigMotionSolver(RigDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition); this.definition = definition; Static = Compute(new Dictionary<string, Transform2D>(), null, null);
        // 역변환할 수 없는 static rig는 사용 시작 전에 거부한다.
        foreach (var matrix in Static.Values) matrix.Inverse();
    }
    internal IReadOnlyDictionary<string, AffineTransform2D> Compute(IReadOnlyDictionary<string, Transform2D> deltas, string? mouthAnchor, Transform2D? mouthMotion)
    {
        var world = new Dictionary<string, AffineTransform2D>(StringComparer.Ordinal);
        foreach (var anchor in definition.Ordered)
        {
            var local = AffineTransform2D.FromTransform(anchor.Transform);
            if (deltas.TryGetValue(anchor.Id, out var delta)) local = local.Compose(AffineTransform2D.FromTransform(delta));
            if (anchor.Id == mouthAnchor && mouthMotion is not null) local = local.Compose(AffineTransform2D.FromTransform(mouthMotion));
            world[anchor.Id] = (anchor.Parent is null ? AffineTransform2D.Identity : world[anchor.Parent]).Compose(local);
        }
        return new ReadOnlyDictionary<string, AffineTransform2D>(world);
    }
    public RigPose Resolve(IReadOnlyDictionary<string, Transform2D>? deltas = null, string? mouthAnchor = null, Transform2D? mouthMotion = null)
    {
        var copy = new CharacterPose(transforms: deltas).Transforms;
        if (copy.Keys.Any(id => !Static.ContainsKey(id)) || mouthAnchor is not null && !Static.ContainsKey(mouthAnchor)) throw new ArgumentException("rig 모션 Anchor가 없습니다.");
        return new(this, copy, mouthAnchor, mouthMotion ?? Transform2D.Identity);
    }
    internal static Transform2D Weight(Transform2D value, double follow)
    {
        if (follow == 0) return Transform2D.Identity;
        if (follow == 1) return value;
        // 반전은 중간 영향도에서 방향을 전환하며0배율을 만들지 않는다.
        static double Scale(double scale, double amount) => ((1 - amount) + Math.Abs(scale) * amount) * (scale < 0 && amount >= .5 ? -1 : 1);
        return new(value.X * follow, value.Y * follow, Scale(value.ScaleX, follow), Scale(value.ScaleY, follow), value.Rotation * follow);
    }
}

public sealed class RigPose
{
    private readonly RigMotionSolver solver;
    private readonly IReadOnlyDictionary<string, Transform2D> deltas;
    private readonly string? mouthAnchor;
    private readonly Transform2D mouthMotion;
    private readonly Dictionary<double, IReadOnlyDictionary<string, AffineTransform2D>> weighted = [];
    private readonly Dictionary<double, IReadOnlyDictionary<string, AffineTransform2D>> bones = [];
    public IReadOnlyDictionary<string, AffineTransform2D> Static => solver.Static;
    public IReadOnlyDictionary<string, AffineTransform2D> Animated { get; }
    public IReadOnlyDictionary<string, AffineTransform2D> WithMouth => Mouth(1);
    internal RigPose(RigMotionSolver solver, IReadOnlyDictionary<string, Transform2D> deltas, string? mouthAnchor, Transform2D mouthMotion)
    {
        this.solver = solver; this.deltas = deltas; this.mouthAnchor = mouthAnchor; this.mouthMotion = mouthMotion;
        Animated = solver.Compute(deltas, null, null); bones[1] = Animated; bones[0] = solver.Static;
    }
    public IReadOnlyDictionary<string, AffineTransform2D> Bone(double follow)
    {
        DefinitionValidation.Unit(follow, nameof(follow));
        if (!bones.TryGetValue(follow, out var result))
            bones[follow] = result = solver.Compute(deltas.ToDictionary(pair => pair.Key, pair => RigMotionSolver.Weight(pair.Value, follow)), null, null);
        return result;
    }
    public IReadOnlyDictionary<string, AffineTransform2D> Mouth(double follow)
    {
        DefinitionValidation.Unit(follow, nameof(follow));
        if (mouthAnchor is null || mouthMotion == Transform2D.Identity) return Bone(follow);
        if (!weighted.TryGetValue(follow, out var result)) weighted[follow] = result = solver.Compute(
            deltas.ToDictionary(pair => pair.Key, pair => RigMotionSolver.Weight(pair.Value, follow)), mouthAnchor, RigMotionSolver.Weight(mouthMotion, follow));
        return result;
    }
}
