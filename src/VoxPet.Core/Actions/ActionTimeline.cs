using VoxPet.Core.Character;

namespace VoxPet.Core.Actions;

internal static class ActionTimeline
{
    private readonly record struct Key(double Time, Transform2D Value, int Index = -1);
    public static ActionState Evaluate(ActionDefinition action, IReadOnlyDictionary<string, PoseDefinition> poses, double milliseconds, bool playing)
    {
        var transforms = new Dictionary<string, Transform2D>(StringComparer.Ordinal); var sprites = new Dictionary<string, string?>(StringComparer.Ordinal);
        var visibility = new Dictionary<string, bool>(StringComparer.Ordinal); var attachments = new Dictionary<string, bool>(StringComparer.Ordinal);
        var transformTimes = new Dictionary<string, double>(StringComparer.Ordinal);
        var transformIndices = new Dictionary<string, int>(StringComparer.Ordinal);
        var effects = new List<ActionEffect>(); string? poseId = null, poseName = null, expression = null;
        for (int i = 0; i < action.Timeline.Count; i++)
        {
            var entry = action.Timeline[i]; if (entry.TimeMs > milliseconds) break;
            switch (entry.Kind)
            {
                case ActionEventKind.Pose:
                    poseId = entry.Value; poseName = entry.Value; transforms.Clear(); transformTimes.Clear(); transformIndices.Clear(); sprites.Clear(); visibility.Clear();
                    if (entry.Value != "idle")
                    {
                        var pose = poses[entry.Value!]; poseName = pose.Name;
                        foreach (var pair in pose.Transforms) { transforms[pair.Key] = pair.Value; transformTimes[pair.Key] = entry.TimeMs; transformIndices[pair.Key] = i; }
                        foreach (var pair in pose.Sprites) sprites[pair.Key] = pair.Value;
                    }
                    break;
                case ActionEventKind.Transform: transforms[entry.Target!] = entry.Transform!; transformTimes[entry.Target!] = entry.TimeMs; transformIndices[entry.Target!] = i; break;
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
                // A pose resets tracks. Equal-time keys resolve to the last authored value.
                var next = Neighbor(target, transformIndices[target], 1, milliseconds);
                if (next is not { } end || !transformTimes.TryGetValue(target, out double startTime)) continue;
                var start = new Key(startTime, transforms[target]);
                var previous = Neighbor(target, transformIndices[target], -1, startTime);
                var following = Neighbor(target, end.Index, 1, end.Time);
                transforms[target] = ActionTransformCurve.Interpolate(start.Time, start.Value, end.Time, end.Value, milliseconds,
                    previous?.Time, previous?.Value, following?.Time, following?.Value, action.TransformInterpolation);
            }
        Key? Neighbor(string target, int index, int direction, double boundary)
        {
            for (int j = index + direction; j >= 0 && j < action.Timeline.Count; j += direction)
            {
                var key = action.Timeline[j];
                if (direction > 0 && key.Kind == ActionEventKind.Pose) return null;
                if (direction < 0 && key.Kind == ActionEventKind.Pose)
                    return key.TimeMs < boundary && key.Value != "idle" && poses[key.Value!].Transforms.TryGetValue(target, out var poseTransform)
                        ? new(key.TimeMs, poseTransform, j) : null;
                if (key.Kind != ActionEventKind.Transform || key.Target != target || (direction > 0 ? key.TimeMs <= boundary : key.TimeMs >= boundary)) continue;
                // Match normal timeline evaluation when duplicate keys share a timestamp.
                while (direction > 0 && j + 1 < action.Timeline.Count && action.Timeline[j + 1].TimeMs == key.TimeMs)
                {
                    j++; var duplicate = action.Timeline[j];
                    if (duplicate.Kind == ActionEventKind.Pose) return null;
                    if (duplicate.Kind == ActionEventKind.Transform && duplicate.Target == target) key = duplicate;
                }
                return new(key.TimeMs, key.Transform!, j);
            }
            return null;
        }
        return new(action.Id, playing, action.Priority, Math.Clamp(milliseconds / action.DurationMs, 0, 1), poseId, poseName,
            transforms, sprites, visibility, effects, expression, attachments);
    }
}
