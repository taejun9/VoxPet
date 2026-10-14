using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Motion;
using VoxPet.Core.Rendering;
using VoxPet.Core.Triggers;

namespace VoxPet.Core.Character;

/// <summary>한 update thread가 소유하는 중앙 runtime. 원본 파일 형식·UI·오디오 장치를 알지 않는다.</summary>
public sealed class CharacterRuntime
{
    public CharacterDefinition Definition { get; }
    public CharacterState State { get; private set; } = new();
    public CharacterPose Pose { get; private set; } = new();
    public ActionEngine Actions { get; }
    public TriggerEngine Triggers { get; }
    public AttachmentSystem Attachments { get; }
    public RigPose CurrentRig { get; private set; }
    public int MouthFrame { get; private set; }
    public int PendingAutomaticCount => automatic.Count;
    public RenderFrame Frame { get; private set; }
    private readonly MotionEngine motion;
    private readonly RigMotionSolver rig;
    private readonly RenderComposer composer;
    private readonly IRuntimeFrameProcessor? processor;
    private readonly Dictionary<string, ActionDefinition> actionById;
    private readonly LinkedList<string> automatic = new();
    private readonly Dictionary<string, LinkedListNode<string>> pending = new(StringComparer.Ordinal);
    private readonly List<TriggerEvent> manual = [];
    private double last;
    private string expression = "neutral";
    public CharacterRuntime(CharacterDefinition definition, Random? random = null, MotionEngine? motion = null,
        IReadOnlyDictionary<string, AssetDimensions>? dimensions = null, IRenderGeometryProvider? geometry = null, double startedAt = 0,
        IEnumerable<TriggerDefinition>? triggers = null, IRuntimeFrameProcessor? processor = null, ActionEngine? actions = null)
    {
        ArgumentNullException.ThrowIfNull(definition); Definition = definition; last = DefinitionValidation.Finite(startedAt, nameof(startedAt));
        if (startedAt < 0) throw new ArgumentOutOfRangeException(nameof(startedAt));
        this.processor = processor; this.motion = motion ?? definition.Motion.CreateEngine(random); Actions = actions ?? new(definition.Poses); Attachments = new(definition.Attachments);
        rig = new(definition.Rig); CurrentRig = rig.Resolve(); composer = new(definition, dimensions, geometry);
        actionById = definition.Actions.ToDictionary(a => a.Id, StringComparer.Ordinal);
        expression = definition.Expressions.FirstOrDefault(e => e.Id == "neutral")?.Id ?? definition.Expressions.FirstOrDefault()?.Id ?? "neutral";
        var bindings = DefinitionValidation.Copy(triggers ?? definition.Actions.SelectMany(a => a.Triggers), nameof(triggers)).ToList();
        if (bindings.Any(t => !actionById.ContainsKey(t.ActionId))) throw new ArgumentException("트리거 동작 참조가 없습니다.");
        var ids = bindings.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var action in definition.Actions)
        {
            if (bindings.Any(t => t.Kind == TriggerKind.Manual && t.Key == action.Id && t.Enabled)) continue;
            string id = "runtime-manual-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(action.Id)); while (!ids.Add(id)) id += "-";
            bindings.Add(new(id, action.Id, TriggerKind.Manual, action.Id));
        }
        Triggers = new(bindings, startedAt); Frame = new([], definition.CanvasWidth, definition.CanvasHeight);
    }
    public void SetExpression(string id)
    {
        if (!Definition.Expressions.Any(e => e.Id == id)) throw new ArgumentException("표정이 없습니다.", nameof(id));
        expression = id;
    }
    public void RequestAction(string id)
    {
        if (!actionById.ContainsKey(id)) throw new ArgumentException("동작이 없습니다.", nameof(id));
        // 동일 tick 중복 입력은 하나로 모은다.
        if (!manual.Any(e => e.Key == id)) manual.Add(new(TriggerKind.Manual, id));
    }
    public void StopActions() { Actions.Stop(); automatic.Clear(); pending.Clear(); manual.Clear(); }
    public RenderFrame Update(RuntimeInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Time < last) throw new ArgumentOutOfRangeException(nameof(input), "단조 증가 시간을 사용하세요.");
        last = input.Time; Actions.Update(input.Time);
        Triggers.Update(input.Time, input.VoiceLevel, input.Input.Events.Concat(manual)); manual.Clear();
        foreach (var request in Triggers.Requests)
        {
            if (request.Kind is TriggerKind.Interval or TriggerKind.VoiceThreshold)
            { if (!pending.ContainsKey(request.ActionId)) pending.Add(request.ActionId, automatic.AddLast(request.ActionId)); }
            else if (Actions.TryPlay(actionById[request.ActionId], input.Time) && pending.Remove(request.ActionId, out var queued)) automatic.Remove(queued);
        }
        if (!Actions.Current.IsPlaying)
            while (automatic.First is { } node)
            {
                automatic.RemoveFirst(); pending.Remove(node.Value);
                if (Actions.TryPlay(actionById[node.Value], input.Time)) break;
            }
        Pose = motion.Update(input.VoiceLevel, input.Time, input.DeltaTime); var selector = Definition.Mouth ?? Definition.SpriteSheet?.Selector;
        MouthFrame = selector is null ? 0 : MouthController.Select(selector, Pose.MouthOpen);
        var transforms = new Dictionary<string, Transform2D>(StringComparer.Ordinal); Transform2D viewport = Transform2D.Identity;
        foreach (var pair in Pose.Transforms)
        {
            string target = pair.Key == "body" ? Definition.Motion.BodyAnchor : pair.Key == "head" ? Definition.Motion.HeadAnchor : pair.Key;
            if (pair.Key == "body" && Definition.Motion.ViewportMotion) viewport = pair.Value;
            else if (rig.Static.ContainsKey(target)) transforms[target] = pair.Value;
        }
        int priority = input.VoiceLevel > 0 ? ActionPriority.VoiceMotion : ActionPriority.Idle;
        foreach (var pair in Actions.Current.Transforms)
            if (rig.Static.ContainsKey(pair.Key) && (Actions.Current.Priority >= priority || !transforms.ContainsKey(pair.Key))) transforms[pair.Key] = pair.Value;
        Transform2D mouthMotion = Transform2D.Identity;
        if (Definition.Mouth is { } mouth)
        {
            var frame = mouth.FrameTransforms[MouthFrame]; mouthMotion = new(frame.X, frame.Y + Pose.MouthOpen * Definition.Motion.MouthTravel, frame.ScaleX, frame.ScaleY, frame.Rotation);
        }
        CurrentRig = rig.Resolve(transforms, Definition.Mouth?.Anchor, mouthMotion);
        string chosenExpression = Actions.Current.ExpressionId ?? expression;
        var selected = Attachments.ForFrame(Actions.Current.Attachments);
        var context = new RuntimeRenderContext(input, Pose, Actions.Current, CurrentRig);
        Frame = composer.Compose(context, chosenExpression, MouthFrame, Attachments, selected, viewport);
        if (processor != null) Frame = processor.Process(context, Frame);
        State = new(input.VoiceLevel, chosenExpression, Actions.Current.ActionId, selected); return Frame;
    }
}
