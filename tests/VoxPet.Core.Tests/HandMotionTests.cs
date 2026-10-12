using System.Text.Json;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class HandMotionTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "VoxPet-hand-" + Guid.NewGuid());
    private static HandMotionSettings Settings(HandTriggerMode mode, double interval = 5) => HandMotionSettings.Default() with
    { Profiles = HandMotionSettings.Default().Profiles.Select(profile => profile with { Enabled = profile.Gesture == HandGesture.HandRaise, Mode = mode, IntervalSeconds = interval }).ToArray() };
    [Fact]
    public void IntervalDoesNotReplayMissedCyclesAndTriggerDoesNotRestartSameMotion()
    {
        var scheduler = new HandMotionScheduler(); scheduler.Configure(Settings(HandTriggerMode.Interval), 0);
        Assert.Null(scheduler.Update(4.99)); Assert.Equal(0, scheduler.Update(5)!.Progress);
        Assert.Equal(HandGesture.HandRaise, scheduler.Update(5.7)!.Gesture); Assert.Null(scheduler.Update(6.4));
        Assert.Equal(0, scheduler.Update(100)!.Progress); Assert.Null(scheduler.Update(101.5)); Assert.Null(scheduler.Update(104.9));
        scheduler.Configure(Settings(HandTriggerMode.Hotkey), 105);
        Assert.True(scheduler.Trigger(HandGesture.HandRaise, 105)); Assert.False(scheduler.Trigger(HandGesture.HandRaise, 105.7));
        Assert.Equal(.5, scheduler.Update(105.7)!.Progress, 10); Assert.False(scheduler.Trigger(HandGesture.Heart, 105.8));
        Assert.True(scheduler.Trigger(HandGesture.Heart, 105.8, true)); Assert.Equal(HandGesture.Heart, scheduler.Update(105.9)!.Gesture);
        scheduler.Cancel(); Assert.Null(scheduler.Update(106)); scheduler.Stop(); Assert.False(scheduler.Trigger(HandGesture.HandRaise, 107, true));
    }
    [Fact]
    public void AlwaysAndPendingIntervalsShareOnePlaybackFairly()
    {
        var settings = HandMotionSettings.Default() with { Profiles = HandMotionSettings.Default().Profiles.Select(profile => profile with { Enabled = true, Mode = HandTriggerMode.Always }).ToArray() };
        var scheduler = new HandMotionScheduler(); scheduler.Configure(settings, 0);
        for (int i = 0; i < 10; i++) Assert.Equal((HandGesture)(i % 5), scheduler.Update(i * 1.5)!.Gesture);
        scheduler.Configure(Settings(HandTriggerMode.Interval, 2), 20); Assert.Null(scheduler.Update(21)); Assert.NotNull(scheduler.Update(22));
        scheduler.Configure(Settings(HandTriggerMode.Hotkey), 22.2); Assert.Null(scheduler.Update(22.3));
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    public void InvalidTimeIsRejected(double time)
    {
        var scheduler = new HandMotionScheduler(); Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Update(time));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.Trigger(HandGesture.HandRaise, time));
    }
    [Fact]
    public void SettingsPreserveOldWardrobeIdsAndRecoverWithoutOverwriting()
    {
        Assert.Equal(7, (int)CostumeCategory.OnePiece); Assert.Equal(8, (int)CostumeCategory.Cape);
        var legacy = JsonSerializer.Deserialize<CostumeProfile>("{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"hat\",\"Category\":0,\"X\":10,\"Y\":20,\"BaseWidth\":100,\"BaseHeight\":100}")!;
        legacy.Validate(); Assert.Equal(MotionAttachment.Head, legacy.EffectiveAttachment); Assert.Equal(1, legacy.Follow);
        Assert.False(CostumeProfile.Conflicts(CostumeCategory.Cape, CostumeCategory.Outerwear));
        var settings = Settings(HandTriggerMode.Hotkey) with { Rigs = new() { ["pelly"] = CharacterRig.Default("pelly") with { HeadMotion = true, MouthTravel = 22 } } };
        var store = new HandMotionStore(Path.Combine(folder, "motion.json")); Assert.True(store.Save(settings));
        var loaded = store.Load(); Assert.False(loaded.Recovered); Assert.Equal(settings.Profiles, loaded.Settings.Profiles); Assert.Equal(settings.Rigs!["pelly"], loaded.Settings.Rigs!["pelly"]);
        File.WriteAllText(Path.Combine(folder, "motion.json"), "corrupt"); Assert.True(store.Load().Recovered); Assert.Equal("corrupt", File.ReadAllText(Path.Combine(folder, "motion.json")));
        Assert.False(store.Save(settings with { Profiles = [] })); Assert.Equal("corrupt", File.ReadAllText(Path.Combine(folder, "motion.json")));
    }
    [Fact]
    public void DuplicateOrReservedKeysAndNonfiniteGeometryAreRejected()
    {
        var defaults = HandMotionSettings.Default(); defaults.Validate();
        Assert.Throws<ArgumentException>(() => (defaults with { Profiles = defaults.Profiles.Select(profile => profile with { Enabled = true }).ToArray() }).Validate());
        var duplicate = defaults with { Profiles = defaults.Profiles.Select(profile => profile with { Enabled = true, LeftClick = false, Shortcut = new(3, 0x31) }).ToArray() };
        Assert.Throws<ArgumentException>(duplicate.Validate);
        Assert.Throws<ArgumentException>(() => new HandShortcut(6, 0x4d).Validate()); Assert.Throws<ArgumentException>(() => new HandShortcut(6, 0x70).Validate());
        Assert.Throws<ArgumentException>(() => (defaults.Profiles[0] with { Scale = double.NaN }).Validate());
        Assert.Throws<ArgumentException>(() => (CharacterRig.Default("neulbo") with { MouthTravel = double.NaN }).Validate());
        Assert.Throws<ArgumentException>(() => (CharacterRig.Default("neulbo") with { Right = new(62, 290, 96, 148) }).Validate());
        Assert.Throws<ArgumentException>(() => (CharacterRig.Default("neulbo") with { HeadCutY = 320 }).Validate());
    }
    [Fact]
    public void AllFiveCharacterRigsAndGesturesStayFiniteAndSettle()
    {
        var state = new CharacterParameters(1, 5, 3, 0, 1, MouthState.Open);
        foreach (var character in CharacterCatalog.All)
        {
            var rig = CharacterRig.Default(character.Id) with { HeadMotion = true }; rig.Validate();
            foreach (var gesture in Enum.GetValues<HandGesture>())
            {
                var profile = HandMotionProfile.Default(gesture) with { X = 20, Y = -15, Scale = 1.3 };
                var start = CharacterMotion.Evaluate(rig, state, 1, new(gesture, 0), profile);
                Assert.Equal(0, start.LeftAngle); Assert.Equal(0, start.RightAngle); Assert.Equal(1, start.HandScale);
                for (int frame = 0; frame <= 100; frame++)
                {
                    var pose = CharacterMotion.Evaluate(rig, state, 1, new(gesture, frame / 100d), profile);
                    Assert.True(new[] { pose.HeadAngle, pose.MouthY, pose.LeftAngle, pose.RightAngle, pose.HandX, pose.HandY, pose.HandScale }.All(double.IsFinite));
                    Assert.InRange(pose.RightAngle, -40, 40); Assert.Equal(10, pose.MouthY);
                    if (character.SideView) Assert.Equal(0, pose.LeftAngle);
                }
                Assert.Equal(0, CharacterMotion.Evaluate(rig, state, 1, new(gesture, 1), profile).Effect);
            }
        }
    }
    [Fact]
    public void ConnectedMeshHasNoInvertedTrianglesAtDefaultMotionAndFixedRegionEdges()
    {
        foreach (string id in CharacterCatalog.All.Select(character => character.Id).Append("builtin"))
        {
            var rig = CharacterRig.Default(id); var x = ConnectedMotion.Columns(rig); var y = ConnectedMotion.Rows(rig, false);
            foreach (var gesture in Enum.GetValues<HandGesture>()) for (int frame = 0; frame < 20; frame++)
            {
                var pose = CharacterMotion.Evaluate(rig, new(0, 0, 0, 0, 1, MouthState.Closed), 1, new(gesture, frame / 20d), HandMotionProfile.Default(gesture));
                MotionPoint Map(double a, double b) => ConnectedMotion.Map(new(a, b), rig, pose);
                for (int row = 0; row < y.Length - 1; row++) for (int column = 0; column < x.Length - 1; column++)
                {
                    var a = Map(x[column], y[row]); var b = Map(x[column + 1], y[row]); var c = Map(x[column + 1], y[row + 1]); var d = Map(x[column], y[row + 1]);
                    Assert.True(Cross(a, b, c) > 0, $"{id}/{gesture}/{frame}/{column}/{row}: ABC"); Assert.True(Cross(a, c, d) > 0, $"{id}/{gesture}/{frame}/{column}/{row}: ACD");
                }
                Assert.Equal(new MotionPoint(rig.Left.X, rig.Left.Y), Map(rig.Left.X, rig.Left.Y));
                Assert.Equal(new MotionPoint(256, 100), Map(256, 100));
            }
        }
        static double Cross(MotionPoint a, MotionPoint b, MotionPoint c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    }
    [Fact]
    public void HandRaisePreservesLegacyZeroAndActuallyLiftsWristWithoutMovingFace()
    {
        Assert.Equal(0, (int)HandGesture.HandRaise);
        var legacy = JsonSerializer.Deserialize<HandMotionProfile>("{\"Gesture\":0,\"Enabled\":true,\"Mode\":2,\"IntervalSeconds\":5,\"Shortcut\":{\"Modifiers\":3,\"Key\":49}}");
        Assert.Equal(HandGesture.HandRaise, legacy!.Gesture); legacy.Validate();
        foreach (var id in CharacterCatalog.All.Select(c => c.Id).Append("builtin"))
        {
            var rig = CharacterRig.Default(id); var limb = rig.Right;
            var pose = CharacterMotion.Evaluate(rig, new(0, 0, 0, 0, 1, MouthState.Closed), 1, new(HandGesture.HandRaise, .5), HandMotionProfile.Default(HandGesture.HandRaise));
            var wrist = new MotionPoint(limb.PivotX, limb.Y + limb.Height * .9); var moved = ConnectedMotion.Map(wrist, rig, pose);
            Assert.True(moved.Y < wrist.Y - 15); Assert.Equal(new MotionPoint(256, 100), ConnectedMotion.Map(new(256, 100), rig, pose));
        }
    }
    [Fact]
    public void ScopedWardrobeAllowsAllFiveCharactersAndPreservesLegacyGlobalItems()
    {
        var items = CharacterCatalog.All.SelectMany(c => Enumerable.Range(0, 9).Select(i => new CostumeProfile(Guid.NewGuid(), "item", (CostumeCategory)i, 0, 0, 512, 512, CharacterId: c.Id, PresetKey: i.ToString()))).ToArray();
        CostumeStore.Validate(items); Assert.Equal(45, items.Length);
        var global = new CostumeProfile(Guid.NewGuid(), "legacy", CostumeCategory.FaceDecoration, 0, 0, 20, 20);
        CostumeStore.Validate(items.Append(global).ToArray());
        Assert.Throws<ArgumentException>(() => (global with { CharacterId = "../private" }).Validate());
        Assert.Throws<ArgumentException>(() => (global with { PresetKey = "hat" }).Validate());
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
