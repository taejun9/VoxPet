using VoxPet.Core.Actions;
using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

/// <summary>Opt-in companion acting. Retargets retain position/velocity; feet remain planted.</summary>
public sealed class AvatarPerformance
{
    private readonly RigMotionSolver solver;
    private Dictionary<string, Transform2D> previous = [];
    private Dictionary<string, double[]> velocity = [];
    private Dictionary<string, double[]> offsets = [];
    private Dictionary<string, double[]> outgoingVelocity = [];
    private string? actionId;
    private double lastTime, lastProgress, fadeStart = double.NegativeInfinity;
    private bool initialized;
    private double voice, voiceVelocity;
    public const double TransitionSeconds = .22;
    public double Voice => voice;
    public AvatarPerformance(RigDefinition rig) => solver = new(rig);
    private static double[] Values(Transform2D t) => [t.X, t.Y, t.ScaleX, t.ScaleY, t.Rotation];
    public IReadOnlyDictionary<string, Transform2D> Update(RuntimeInput input, ActionState action, IReadOnlyDictionary<string, Transform2D> target, bool side)
    {
        double dt = initialized ? input.Time - lastTime : 0;
        if (dt < 0) throw new ArgumentOutOfRangeException(nameof(input));
        var result = target.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        bool changed = initialized && (action.ActionId != actionId || action.ActionId != null && action.Progress < lastProgress);
        if (changed)
        {
            fadeStart = input.Time; offsets.Clear(); outgoingVelocity.Clear();
            foreach (string key in previous.Keys.Union(target.Keys))
            {
                var from = Values(previous.GetValueOrDefault(key) ?? Transform2D.Identity);
                var to = Values(target.GetValueOrDefault(key) ?? Transform2D.Identity);
                offsets[key] = from.Zip(to, (a, b) => a - b).ToArray();
                outgoingVelocity[key] = velocity.GetValueOrDefault(key) ?? new double[5];
            }
        }
        double u = Math.Clamp((input.Time - fadeStart) / TransitionSeconds, 0, 1);
        if (u < 1)
            foreach (var pair in offsets)
            {
                var values = Values(result.GetValueOrDefault(pair.Key) ?? Transform2D.Identity);
                double positionWeight = (2 * u - 3) * u * u + 1, speedWeight = ((u - 2) * u + 1) * u;
                for (int i = 0; i < values.Length; i++) values[i] += positionWeight * pair.Value[i] + speedWeight * TransitionSeconds * outgoingVelocity[pair.Key][i];
                // The companion tracks never mirror. Keep a rapid interrupted scale invertible.
                result[pair.Key] = new(values[0], values[1], Math.Clamp(values[2], .1, 10), Math.Clamp(values[3], .1, 10), values[4]);
            }
        else { offsets.Clear(); outgoingVelocity.Clear(); }
        if (dt > 0)
        {
            var speeds = new Dictionary<string, double[]>();
            foreach (string key in result.Keys.Union(previous.Keys))
            {
                var now = Values(result.GetValueOrDefault(key) ?? Transform2D.Identity);
                var old = Values(previous.GetValueOrDefault(key) ?? Transform2D.Identity);
                speeds[key] = now.Zip(old, (a, b) => (a - b) / dt).ToArray();
            }
            velocity = speeds;
            // Exact critically damped response for a held level; stable at irregular frame rates.
            double omega = side ? 15 : 11, displacement = voice - input.VoiceLevel;
            double impulse = voiceVelocity + omega * displacement, decay = Math.Exp(-omega * dt);
            if (omega * dt > 64) { voice = input.VoiceLevel; voiceVelocity = 0; }
            else
            {
                voice = input.VoiceLevel + (displacement + impulse * dt) * decay;
                voiceVelocity = (voiceVelocity - omega * impulse * dt) * decay;
            }
        }
        else if (!initialized) voice = input.VoiceLevel;
        voice = Math.Clamp(voice, 0, 1);
        previous = new(result, StringComparer.Ordinal); lastTime = input.Time; actionId = action.ActionId; lastProgress = action.Progress; initialized = true;
        AddSecondary(result, input.Time, voice, side);
        return result;
    }
    private void AddSecondary(Dictionary<string, Transform2D> values, double time, double level, bool side)
    {
        if (!solver.Static.ContainsKey("body") || !solver.Static.ContainsKey("head")) return;
        double period = side ? 4.2 : 4.8;
        double breath = (side ? .003 : .004) * Math.Sin((time % period) * Math.PI * 2 / period);
        var body = values.GetValueOrDefault("body") ?? Transform2D.Identity;
        values["body"] = new(body.X, body.Y - (side ? 112 : 115) * breath - level * .9,
            body.ScaleX * (1 - breath * .35), body.ScaleY * (1 + breath), body.Rotation);
        var head = values.GetValueOrDefault("head") ?? Transform2D.Identity;
        double angle = (side ? .55 : .7) * Math.Sin((time % 31) * Math.PI * 2 / 7.75) + level * (side ? -.65 : .8);
        double a = angle * Math.PI / 180, nx = side ? -12 : 0, ny = side ? 85 : 45;
        double x = nx - nx * Math.Cos(a) + ny * Math.Sin(a), y = ny - nx * Math.Sin(a) - ny * Math.Cos(a) - level * .7;
        double r = head.Rotation * Math.PI / 180;
        values["head"] = new(head.X + head.ScaleX * Math.Cos(r) * x - head.ScaleY * Math.Sin(r) * y,
            head.Y + head.ScaleX * Math.Sin(r) * x + head.ScaleY * Math.Cos(r) * y, head.ScaleX, head.ScaleY, head.Rotation + angle);
        var animated = solver.Resolve(values);
        foreach (string id in new[] { "leftFoot", "rightFoot" })
        {
            if (!solver.Static.ContainsKey(id)) continue;
            var wanted = solver.Static[id].Map(0, 0);
            var local = solver.Static["body"].Inverse().Compose(solver.Static[id]);
            var parent = animated.Animated["body"].Inverse().Map(wanted.X, wanted.Y);
            var delta = local.Inverse().Map(parent.X, parent.Y);
            var old = values.GetValueOrDefault(id) ?? Transform2D.Identity;
            values[id] = new(delta.X, delta.Y, old.ScaleX, old.ScaleY, old.Rotation);
        }
    }
}
