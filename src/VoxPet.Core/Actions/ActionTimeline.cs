using VoxPet.Core.Character;

namespace VoxPet.Core.Actions;

internal static class ActionTimeline
{
    public static ActionState Evaluate(ActionDefinition action, IReadOnlyDictionary<string, PoseDefinition> poses, double milliseconds, bool playing)
    {
        var transforms = new Dictionary<string, Transform2D>(StringComparer.Ordinal); var sprites = new Dictionary<string, string?>(StringComparer.Ordinal);
        var visibility = new Dictionary<string, bool>(StringComparer.Ordinal); var attachments = new Dictionary<string, bool>(StringComparer.Ordinal);
        var transformTimes = new Dictionary<string, double>(StringComparer.Ordinal);
        var effects = new List<ActionEffect>(); string? poseId = null, poseName = null, expression = null;
        for (int i = 0; i < action.Timeline.Count; i++)
        {
            var entry = action.Timeline[i]; if (entry.TimeMs > milliseconds) break;
            switch (entry.Kind)
            {
                case ActionEventKind.Pose:
                    poseId = entry.Value; poseName = entry.Value; transforms.Clear(); transformTimes.Clear(); sprites.Clear(); visibility.Clear();
                    if (entry.Value != "idle")
                    {
                        var pose = poses[entry.Value!]; poseName = pose.Name;
                        foreach (var pair in pose.Transforms) { transforms[pair.Key] = pair.Value; transformTimes[pair.Key] = entry.TimeMs; }
                        foreach (var pair in pose.Sprites) sprites[pair.Key] = pair.Value;
                    }
                    break;
                case ActionEventKind.Transform: transforms[entry.Target!] = entry.Transform!; transformTimes[entry.Target!] = entry.TimeMs; break;
                case ActionEventKind.Sprite: sprites[entry.Target!] = entry.Value; break;
                case ActionEventKind.Visibility: visibility[entry.Target!] = entry.Enabled; break;
                case ActionEventKind.Expression: expression = entry.Value; break;
                case ActionEventKind.Attachment: attachments[entry.Value!] = entry.Enabled; break;
                case ActionEventKind.Effect:
                    double age = milliseconds - entry.TimeMs;
                    if (age < entry.DurationMs) effects.Add(new($"{action.Id}-effect-{i}", entry.Value!, entry.Target!, entry.Transform ?? Transform2D.Identity, age / entry.DurationMs, entry.EndAnchor, entry.EndTransform, entry.ZIndex));
                    break;
                case ActionEventKind.Sound: break; // 규격 예약. Core는 파일/오디오를 재생하지 않는다.
            }
        }
        if (action.InterpolateTransforms)
            foreach (string target in transforms.Keys.ToArray())
            {
                // A pose event is a track reset; never interpolate across it.
                var next = action.Timeline.SkipWhile(e => e.TimeMs <= milliseconds).TakeWhile(e => e.Kind != ActionEventKind.Pose)
                    .FirstOrDefault(e => e.Kind == ActionEventKind.Transform && e.Target == target);
                if (next?.Transform is not { } end || !transformTimes.TryGetValue(target, out double startTime)) continue;
                double t = Math.Clamp((milliseconds - startTime) / (next.TimeMs - startTime), 0, 1); t = t * t * (3 - 2 * t);
                var start = transforms[target];
                double Blend(double a, double b) => a * (1 - t) + b * t;
                // Mirroring is discrete: crossing a zero scale would make the rig singular.
                double Scale(double a, double b) => Math.Sign(a) == Math.Sign(b) ? Blend(a, b) : a;
                transforms[target] = new(Blend(start.X, end.X), Blend(start.Y, end.Y), Scale(start.ScaleX, end.ScaleX), Scale(start.ScaleY, end.ScaleY), Blend(start.Rotation, end.Rotation));
            }
        return new(action.Id, playing, action.Priority, Math.Clamp(milliseconds / action.DurationMs, 0, 1), poseId, poseName,
            transforms, sprites, visibility, effects, expression, attachments);
    }
}
