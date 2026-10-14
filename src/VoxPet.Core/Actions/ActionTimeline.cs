using VoxPet.Core.Character;

namespace VoxPet.Core.Actions;

internal static class ActionTimeline
{
    public static ActionState Evaluate(ActionDefinition action, IReadOnlyDictionary<string, PoseDefinition> poses, double milliseconds, bool playing)
    {
        var transforms = new Dictionary<string, Transform2D>(StringComparer.Ordinal); var sprites = new Dictionary<string, string?>(StringComparer.Ordinal);
        var visibility = new Dictionary<string, bool>(StringComparer.Ordinal); var attachments = new Dictionary<string, bool>(StringComparer.Ordinal);
        var effects = new List<ActionEffect>(); string? poseId = null, poseName = null, expression = null;
        for (int i = 0; i < action.Timeline.Count; i++)
        {
            var entry = action.Timeline[i]; if (entry.TimeMs > milliseconds) break;
            switch (entry.Kind)
            {
                case ActionEventKind.Pose:
                    poseId = entry.Value; poseName = entry.Value; transforms.Clear(); sprites.Clear(); visibility.Clear();
                    if (entry.Value != "idle")
                    {
                        var pose = poses[entry.Value!]; poseName = pose.Name;
                        foreach (var pair in pose.Transforms) transforms[pair.Key] = pair.Value;
                        foreach (var pair in pose.Sprites) sprites[pair.Key] = pair.Value;
                    }
                    break;
                case ActionEventKind.Transform: transforms[entry.Target!] = entry.Transform!; break;
                case ActionEventKind.Sprite: sprites[entry.Target!] = entry.Value; break;
                case ActionEventKind.Visibility: visibility[entry.Target!] = entry.Enabled; break;
                case ActionEventKind.Expression: expression = entry.Value; break;
                case ActionEventKind.Attachment: attachments[entry.Value!] = entry.Enabled; break;
                case ActionEventKind.Effect:
                    double age = milliseconds - entry.TimeMs;
                    if (age < entry.DurationMs) effects.Add(new($"{action.Id}-effect-{i}", entry.Value!, entry.Target!, entry.Transform ?? Transform2D.Identity, age / entry.DurationMs));
                    break;
                case ActionEventKind.Sound: break; // 규격 예약. Core는 파일/오디오를 재생하지 않는다.
            }
        }
        return new(action.Id, playing, action.Priority, Math.Clamp(milliseconds / action.DurationMs, 0, 1), poseId, poseName,
            transforms, sprites, visibility, effects, expression, attachments);
    }
}
