using System.IO;
using System.Windows.Threading;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using VoxPet.App.Services;

namespace VoxPet.App.ViewModels;

/// <summary>UI 스레드 설정과 시간축. 저장 snapshot은 worker에서 직렬 처리한다.</summary>
public sealed class HandMotionViewModel : ObservableObject
{
    private readonly HandMotionScheduler scheduler = new();
    private readonly HandMotionStore store;
    private readonly bool persistent;
    private readonly Func<double> seconds;
    private readonly CharacterViewModel character;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly DispatcherTimer saveTimer;
    private HandMotionSettings settings = HandMotionSettings.Default();
    private string characterId = CharacterCatalog.DefaultId;
    private int selected, side;
    private bool ready, closing, guides;
    private string status = "기본: 좌클릭 손올리기 · 설정은 자동 저장됩니다.";
    public CharacterViewModel Character => character;
    public string[] Gestures { get; } = ["손올리기", "장난감 총", "손하트", "장난감 칼", "요술봉"];
    public string[] Modes { get; } = ["항상", "초 간격", "입력할 때"];
    public string[] Inputs { get; } = ["좌클릭", "전역 키"];
    public string[] ModifierNames { get; } = ["Ctrl+Alt", "Ctrl", "Alt", "Ctrl+Shift", "Alt+Shift", "Ctrl+Alt+Shift"];
    private static readonly uint[] Modifiers = [3, 2, 1, 6, 5, 7];
    public string[] Keys { get; } = Enumerable.Range(0, 10).Select(i => i.ToString()).Concat(Enumerable.Range('A', 26).Select(i => ((char)i).ToString())).Concat(Enumerable.Range(1, 11).Select(i => "F" + i)).ToArray();
    private static readonly uint[] KeyCodes = Enumerable.Range(0x30, 10).Concat(Enumerable.Range(0x41, 26)).Concat(Enumerable.Range(0x70, 11)).Select(i => (uint)i).ToArray();
    public string[] Sides { get; } = ["화면 왼쪽 손", "화면 오른쪽 손 / 날개"];
    public int SelectedIndex { get => selected; set { if (value is < 0 or > 4 || !Set(ref selected, value)) return; NotifyEditor(); } }
    public HandMotionProfile Selected => settings.Profiles.Single(profile => (int)profile.Gesture == selected);
    public bool Enabled { get => Selected.Enabled; set => Change(Selected with { Enabled = value }); }
    public int ModeIndex { get => (int)Selected.Mode; set { if (value is >= 0 and <= 2) Change(Selected with { Mode = (HandTriggerMode)value }); } }
    public int InputIndex { get => Selected.LeftClick ? 0 : 1; set { if (value is 0 or 1) Change(Selected with { LeftClick = value == 0 }); } }
    public int ModifierIndex { get => Array.IndexOf(Modifiers, Selected.Shortcut.Modifiers); set { if (value >= 0 && value < Modifiers.Length) Change(Selected with { Shortcut = Selected.Shortcut with { Modifiers = Modifiers[value] } }); } }
    public int KeyIndex { get => Array.IndexOf(KeyCodes, Selected.Shortcut.Key); set { if (value >= 0 && value < KeyCodes.Length) Change(Selected with { Shortcut = Selected.Shortcut with { Key = KeyCodes[value] } }); } }
    public double Interval { get => Selected.IntervalSeconds; set => Change(Selected with { IntervalSeconds = value }); }
    public double X { get => Selected.X; set => Change(Selected with { X = value }); }
    public double Y { get => Selected.Y; set => Change(Selected with { Y = value }); }
    public double ScalePercent { get => Selected.Scale * 100; set => Change(Selected with { Scale = value / 100 }); }
    public CharacterRig Rig => settings.Rigs?.GetValueOrDefault(characterId) ?? CharacterRig.Default(characterId);
    private LimbRegion Limb => side == 0 ? Rig.Left : Rig.Right;
    public int SideIndex { get => side; set { if (value is 0 or 1 && Set(ref side, value)) NotifyEditor(); } }
    public bool LimbEnabled { get => Limb.Enabled; set => ChangeLimb(Limb with { Enabled = value }); }
    public double LimbX { get => Limb.X; set => ChangeLimb(Limb with { X = value }); }
    public double LimbY { get => Limb.Y; set => ChangeLimb(Limb with { Y = value }); }
    public double LimbWidth { get => Limb.Width; set => ChangeLimb(Limb with { Width = value }); }
    public double LimbHeight { get => Limb.Height; set => ChangeLimb(Limb with { Height = value }); }
    public double HeadCut { get => Rig.HeadCutY; set => ChangeRig(Rig with { HeadCutY = value }); }
    public double MouthTravel { get => Rig.MouthTravel; set => ChangeRig(Rig with { MouthTravel = value }); }
    public bool HeadMotion { get => Rig.HeadMotion; set => ChangeRig(Rig with { HeadMotion = value }); }
    public bool Guides { get => guides; set { if (Set(ref guides, value)) { character.ShowMotionGuides = value; character.RefreshMotion(); } } }
    public string Status { get => status; set => Set(ref status, value); }
    public bool CanEdit => ready && !closing;
    public RelayCommand PreviewCommand { get; }
    public RelayCommand ClickCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ResetRigCommand { get; }
    public event Action? InputsChanged;
    public HandMotionSettings Snapshot() => settings with { Profiles = [.. settings.Profiles], Rigs = settings.Rigs == null ? null : new(settings.Rigs) };
    public HandMotionViewModel(CharacterViewModel character, bool persistent, string folder, Func<double> seconds)
    {
        this.character = character; this.persistent = persistent; this.seconds = seconds; store = new(Path.Combine(folder, "motion.json"));
        ClickCommand = new(() => Change(Selected with { Enabled = true, Mode = HandTriggerMode.Hotkey, LeftClick = true }), () => CanEdit);
        PreviewCommand = new(() => Trigger((HandGesture)selected, true), () => CanEdit);
        StopCommand = new(() => { scheduler.Cancel(); character.SetMotion(Rig, null, null); }, () => CanEdit);
        ResetRigCommand = new(() => ChangeRig(CharacterRig.Default(characterId)), () => CanEdit);
        saveTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(350) }; saveTimer.Tick += SaveTick;
        scheduler.Configure(settings, seconds());
    }
    public async Task InitializeAsync()
    {
        if (persistent)
        {
            var loaded = await Task.Run(store.Load); if (closing) return;
            settings = loaded.Settings; if (loaded.Recovered) Status = "동작 설정을 읽지 못해 기본값으로 시작합니다. 이전 파일은 보존합니다.";
        }
        scheduler.Configure(settings, seconds()); ready = true; Update(seconds()); NotifyEditor(); Notify(nameof(CanEdit)); PreviewCommand.Refresh(); ClickCommand.Refresh(); StopCommand.Refresh(); ResetRigCommand.Refresh(); InputsChanged?.Invoke();
    }
    public void SetCharacter(string id)
    {
        if (id != characterId) scheduler.Cancel(); characterId = id; Update(seconds()); NotifyEditor();
    }
    public bool Trigger(HandGesture gesture, bool preview = false) => !closing && scheduler.Trigger(gesture, seconds(), preview);
    public void LeftClick()
    {
        var target = settings.Profiles.FirstOrDefault(profile => profile.Enabled && profile.LeftClick && profile.Mode == HandTriggerMode.Hotkey);
        if (target != null) Trigger(target.Gesture);
    }
    public void Update(double now)
    {
        var playback = scheduler.Update(now);
        character.SetMotion(Rig, playback, playback == null ? null : settings.Profiles.Single(profile => profile.Gesture == playback.Gesture));
    }
    private void Change(HandMotionProfile profile)
    {
        if (closing) return;
        var profiles = settings.Profiles.Select(old => old.Gesture == profile.Gesture ? profile :
            profile.Enabled && profile.LeftClick && profile.Mode == HandTriggerMode.Hotkey && old.LeftClick && old.Mode == HandTriggerMode.Hotkey ? old with { Enabled = false } : old).ToArray();
        Apply(settings with { Profiles = profiles });
    }
    private void ChangeLimb(LimbRegion limb) => ChangeRig(side == 0 ? Rig with { Left = limb } : Rig with { Right = limb });
    private void ChangeRig(CharacterRig rig)
    {
        var rigs = settings.Rigs == null ? new Dictionary<string, CharacterRig>() : new(settings.Rigs); rigs[characterId] = rig;
        Apply(settings with { Rigs = rigs });
    }
    private void Apply(HandMotionSettings value)
    {
        if (closing) return;
        try
        {
            value.Validate(); bool inputsChanged = !settings.Profiles.SequenceEqual(value.Profiles); settings = value; scheduler.Configure(settings, seconds());
            Update(seconds()); Status = "미리보기에 반영했습니다. 마지막 설정을 자동 저장합니다.";
            if (persistent && ready) { saveTimer.Stop(); saveTimer.Start(); }
            NotifyEditor(); if (inputsChanged) InputsChanged?.Invoke();
        }
        catch (ArgumentException error) { Status = error.Message; NotifyEditor(); }
    }
    private void NotifyEditor()
    {
        foreach (string name in new[] { nameof(Selected), nameof(Enabled), nameof(ModeIndex), nameof(InputIndex), nameof(ModifierIndex), nameof(KeyIndex), nameof(Interval), nameof(X), nameof(Y), nameof(ScalePercent), nameof(Rig), nameof(HeadCut), nameof(MouthTravel), nameof(HeadMotion), nameof(LimbEnabled), nameof(LimbX), nameof(LimbY), nameof(LimbWidth), nameof(LimbHeight) }) Notify(name);
    }
    private async void SaveTick(object? sender, EventArgs e) { saveTimer.Stop(); if (!closing) await FlushAsync(); }
    public async Task<bool> FlushAsync()
    {
        if (!persistent || !ready) return true;
        var snapshot = Snapshot(); await gate.WaitAsync();
        try { bool saved = await Task.Run(() => store.Save(snapshot)); if (!saved) Status = "동작 설정을 저장하지 못했습니다. 저장 공간/권한을 확인하세요. 이전 파일은 유지합니다."; return saved; }
        finally { gate.Release(); }
    }
    public async Task CloseAsync()
    {
        closing = true; scheduler.Stop(); saveTimer.Stop(); saveTimer.Tick -= SaveTick;
        character.SetMotion(Rig, null, null); character.ShowMotionGuides = false;
        Notify(nameof(CanEdit)); PreviewCommand.Refresh(); ClickCommand.Refresh(); StopCommand.Refresh(); ResetRigCommand.Refresh();
        await FlushAsync();
    }
}
