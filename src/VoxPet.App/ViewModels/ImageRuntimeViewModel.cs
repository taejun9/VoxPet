using System.IO;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using VoxPet.App.Services;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using VoxPet.Core.Legacy;
using VoxPet.Core.Character;

namespace VoxPet.App.ViewModels;

public sealed record RuntimeLayer(ImageSource Image, AnchorOffset Offset);
public sealed record BindingRow(RuntimeOperation Operation, string Name, string Key);

/// <summary>로컬 이미지 세트를 읽고 공유 캐릭터에 조합 결과를 제공한다. 입력 이벤트에는 파일 작업이 없다.</summary>
public sealed class ImageRuntimeViewModel : ObservableObject
{
    private readonly CharacterViewModel character;
    private readonly bool persistent;
    private readonly string library;
    private readonly RuntimePreferencesStore store;
    private readonly Func<string> currentId;
    private readonly Func<double> seconds;
    private readonly Func<ExpressionKind, Task> expression;
    private readonly SemaphoreSlim saveGate = new(1, 1);
    private readonly DispatcherTimer saveTimer;
    private RuntimePreferences preferences = RuntimePreferences.Default();
    private LoadedImageSet? loaded;
    private CharacterRuntimeViewModel? player;
    private bool closing, importing, selectionBusy, initialized;
    private long activationRevision;
    public bool IsImporting => importing;
    public long SceneRevision => activationRevision;
    private TaskCompletionSource? importDone;
    public Func<Task<bool>>? LegacyRequested { get; set; }
    public void SetSelectionBusy(bool value)
    {
        if (selectionBusy == value) return;
        selectionBusy = value; Notify(nameof(CanUse)); Notify(nameof(CanConfigure));
        ImportCommand.Refresh(); RestoreCommand.Refresh(); LegacyCommand.Refresh(); PlayCommand.Refresh(); ResetCommand.Refresh(); InputsChanged?.Invoke();
    }
    private int action, outfit, weapon;
    private string status = "동일 Canvas의 PNG와 imageset.json을 준비해 등록하세요.";
    private RuntimeOperation? recording;
    public bool IsActive => loaded != null;
    public bool CanConfigure => initialized && !closing && !importing && !selectionBusy;
    public bool CanUse => initialized && IsActive && !closing && !importing && !selectionBusy;
    public bool LeftClick { get => preferences.LeftClick; set { preferences = preferences with { LeftClick = value }; Changed(); } }
    public RuntimeBinding[] Bindings => preferences.Bindings;
    private static readonly string[] OperationNames = ["액션 실행", "다음 액션", "이전 액션", "다음 표정", "이전 표정", "다음 의상", "이전 의상", "기본 상태 복귀"];
    public BindingRow[] BindingRows => Enum.GetValues<RuntimeOperation>().Select(op => new BindingRow(op, OperationNames[(int)op], recording == op ? "키를 누르세요…" : preferences.Bindings.FirstOrDefault(b => b.Operation == op)?.Label ?? "미지정")).ToArray();
    private int ActionCount => loaded?.Definition.Actions.Length ?? 0;
    public string[] ActionNames => ActionCount == 0 ? ["등록 액션 없음"] : loaded!.Definition.Actions.Select(a => a.Name).ToArray();
    public string[] OutfitNames => new[] { "의상 없음" }.Concat(loaded?.Definition.Outfits.Select(o => o.Name) ?? []).ToArray();
    public string[] WeaponNames => new[] { "무기 없음" }.Concat(loaded?.Definition.Weapons.Select(w => w.Name) ?? []).ToArray();
    public ImageSource? WeaponPreview => loaded != null && weapon >= 0 ? loaded.Previews[loaded.Definition.Weapons[weapon].Layers.First(l => l.Part == ImagePart.Weapon).Poses["idle"]] : null;
    public int ActionIndex { get => action; set { if (!CanUse || value < 0 || value >= ActionCount || !Set(ref action, value)) return; player?.Stop(); Changed(); } }
    public int OutfitIndex { get => outfit + 1; set { if (!CanUse || value < 0 || value >= OutfitNames.Length || !Set(ref outfit, value - 1, nameof(OutfitIndex))) return; Changed(); } }
    public int WeaponIndex { get => weapon + 1; set { if (!CanUse || value < 0 || value >= WeaponNames.Length || !Set(ref weapon, value - 1, nameof(WeaponIndex))) return; Notify(nameof(WeaponPreview)); Changed(); } }
    public string Status { get => status; private set => Set(ref status, value); }
    public AsyncCommand ImportCommand { get; }
    public AsyncCommand RestoreCommand { get; }
    public AsyncCommand LegacyCommand { get; }
    public RelayCommand PlayCommand { get; }
    public RelayCommand ResetCommand { get; }
    public event Func<string?>? ChooseManifest;
    public event Action? InputsChanged;
    public event Action? Activated;
    public ImageRuntimeViewModel(CharacterViewModel character, bool persistent, string folder, Func<string> currentId, Func<double> seconds, Func<ExpressionKind, Task> expression)
    {
        this.character = character; this.persistent = persistent; this.currentId = currentId; this.seconds = seconds; this.expression = expression;
        library = Path.Combine(folder, "ImageSets"); store = new(Path.Combine(folder, "image-runtime.json"));
        ImportCommand = new(async () => { string? path = ChooseManifest?.Invoke(); if (path != null) await ImportAsync(path); }, () => CanConfigure);
        RestoreCommand = new(async () => { string id = currentId(); long revision = activationRevision; var pack = await PrepareAsync(id, true); if (!closing && currentId() == id && activationRevision == revision && pack != null) { Activate(pack); Changed(); } }, () => CanConfigure);
        LegacyCommand = new(async () => { if (LegacyRequested != null && await LegacyRequested()) { Changed(); Status = "기존 PNG 시트로 전환했습니다. 등록 세트는 보존합니다."; } }, () => CanUse);
        PlayCommand = new(() => Execute(RuntimeOperation.Play), () => CanUse && ActionCount > 0);
        ResetCommand = new(() => Execute(RuntimeOperation.Reset), () => CanUse);
        saveTimer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(350) }; saveTimer.Tick += SaveTick;
    }
    public async Task InitializeAsync()
    {
        if (initialized || closing) return;
        var saved = persistent ? await Task.Run(store.Load) : RuntimePreferences.Default();
        if (closing) return;
        preferences = saved; initialized = true;
        Notify(nameof(LeftClick)); Notify(nameof(BindingRows)); Notify(nameof(CanConfigure));
        ImportCommand.Refresh(); RestoreCommand.Refresh(); InputsChanged?.Invoke();
    }
    public async Task<LoadedImageSet?> PrepareAsync(string id, bool force = false)
    {
        if (closing || !preferences.Characters.TryGetValue(id, out var selection) || !selection.Enabled && !force) return null;
        try
        {
            var pack = await Task.Run(() => ImageSetLoader.Load(Path.Combine(library, selection.PackId, "imageset.json")));
            if (pack.Definition.CharacterId != id) throw new ArgumentException("캐릭터 세트 ID를 확인하세요.");
            return closing ? null : pack;
        }
        catch (Exception error) when (Expected(error)) { Status = "등록 세트를 읽지 못했습니다. 기존 PNG를 유지합니다. " + error.Message; return null; }
    }
    public void Activate(LoadedImageSet? pack)
    {
        Activated?.Invoke(); activationRevision++; recording = null; Notify(nameof(BindingRows));
        loaded = pack;
        player = pack == null ? null : new(new CharacterRuntime(new LegacyImageSetSource(pack.Definition).Load(), startedAt: seconds()),
            pack.Images.ToDictionary(pair => pair.Key, pair => (ImageSource)pair.Value, StringComparer.OrdinalIgnoreCase), seconds());
        character.SetRuntimeScene(player);
        var saved = preferences.Characters.GetValueOrDefault(currentId());
        action = pack == null ? 0 : Math.Clamp(saved?.Action ?? 0, 0, Math.Max(0, pack.Definition.Actions.Length - 1));
        outfit = pack == null ? -1 : Math.Clamp(saved?.Outfit ?? -1, -1, pack.Definition.Outfits.Length - 1);
        weapon = pack == null ? -1 : Math.Clamp(saved?.Weapon ?? -1, -1, pack.Definition.Weapons.Length - 1);
        character.SetRuntimeLayers(pack == null ? null : [], pack == null ? null : CharacterCatalog.All.Single(c => c.Id == pack.Definition.CharacterId).Name + " · 이미지 세트");
        foreach (string name in new[] { nameof(IsActive), nameof(CanUse), nameof(ActionNames), nameof(OutfitNames), nameof(WeaponNames), nameof(ActionIndex), nameof(OutfitIndex), nameof(WeaponIndex), nameof(WeaponPreview) }) Notify(name);
        ImportCommand.Refresh(); RestoreCommand.Refresh(); LegacyCommand.Refresh(); PlayCommand.Refresh(); ResetCommand.Refresh(); InputsChanged?.Invoke();
        if (pack != null) Status = $"{CharacterCatalog.All.Single(c => c.Id == pack.Definition.CharacterId).Name} · {pack.Definition.Parts.Length}파츠 / {pack.Definition.Actions.Length}액션 · 이미지 조합 실행 중";
    }
    public async Task<bool> ImportAsync(string path)
    {
        if (!CanConfigure) return false;
        string id = currentId(); importing = true; importDone = new(TaskCreationOptions.RunContinuationsAsynchronously); Notify(nameof(IsImporting)); Notify(nameof(CanUse)); Notify(nameof(CanConfigure)); InputsChanged?.Invoke();
        try
        {
            string packId = await Task.Run(() => ImageSetLoader.Import(path, library, id));
            var pack = await Task.Run(() => ImageSetLoader.Load(Path.Combine(library, packId, "imageset.json")));
            if (closing || currentId() != id) return false;
            var selections = new Dictionary<string, RuntimeSelection>(preferences.Characters) { [id] = new(packId) };
            var next = preferences with { Characters = selections };
            if (persistent)
            {
                await saveGate.WaitAsync(); bool saved;
                try { saved = await Task.Run(() => store.Save(next)); } finally { saveGate.Release(); }
                if (!saved) throw new IOException("등록 설정을 저장하지 못했습니다. 기존 세트를 유지합니다.");
            }
            if (closing || currentId() != id) return false;
            preferences = next; Activate(pack); return true;
        }
        catch (Exception error) when (Expected(error)) { if (!closing) Status = "등록 실패 · " + error.Message; return false; }
        finally { importing = false; importDone?.TrySetResult(); Notify(nameof(IsImporting)); Notify(nameof(CanUse)); Notify(nameof(CanConfigure)); PlayCommand.Refresh(); LegacyCommand.Refresh(); ResetCommand.Refresh(); InputsChanged?.Invoke(); }
    }
    private static bool Expected(Exception error) => error is IOException or UnauthorizedAccessException or ArgumentException or JsonException or NotSupportedException or FormatException or OverflowException or System.Runtime.InteropServices.COMException;
    public void Update(double now, double level, bool blink, ExpressionKind kind)
    {
        if (loaded == null || player == null) return;
        string expressionId = kind.ToString().ToLowerInvariant();
        player.Runtime.SetExpression(player.Runtime.Definition.Expressions.Any(e => e.Id == expressionId) ? expressionId : "neutral");
        player.Runtime.Attachments.Restore(new[] { outfit >= 0 ? "outfit-" + outfit : null, weapon >= 0 ? "weapon-" + weapon : null }.OfType<string>());
        var composed = player.Update(now, level, blink);
        character.SetRuntimeLayers(composed.Layers.Select(layer => new RuntimeLayer(loaded.Images[layer.Asset], new(layer.X * 512 / loaded.Definition.CanvasSize, layer.Y * 512 / loaded.Definition.CanvasSize))).ToArray());
    }
    public void Execute(RuntimeOperation operation)
    {
        if (!CanUse) return;
        switch (operation)
        {
            case RuntimeOperation.Play:
                if (weapon >= 0 && ActionCount > 0 && loaded!.Definition.Weapons[weapon].Template != loaded.Definition.Actions[action].Template)
                { Status = "액션과 무기의 템플릿이 다릅니다. 같은 템플릿의 무기를 선택하세요."; return; }
                player!.Runtime.RequestAction("action-" + action); break;
            case RuntimeOperation.NextAction: if (ActionCount > 0) ActionIndex = (action + 1) % ActionCount; break;
            case RuntimeOperation.PreviousAction: if (ActionCount > 0) ActionIndex = (action + ActionCount - 1) % ActionCount; break;
            case RuntimeOperation.NextExpression: ChangeExpression(1); break;
            case RuntimeOperation.PreviousExpression: ChangeExpression(-1); break;
            case RuntimeOperation.NextOutfit: OutfitIndex = (OutfitIndex + 1) % OutfitNames.Length; break;
            case RuntimeOperation.PreviousOutfit: OutfitIndex = (OutfitIndex + OutfitNames.Length - 1) % OutfitNames.Length; break;
            case RuntimeOperation.Reset: player!.Stop(); OutfitIndex = 0; WeaponIndex = 0; _ = expression(ExpressionKind.Neutral); break;
        }
    }
    private void ChangeExpression(int delta)
    {
        var kinds = loaded!.Definition.Expressions.Select(e => e.Kind).ToArray(); int index = Array.IndexOf(kinds, character.ExpressionKind);
        _ = expression(kinds[(Math.Max(0, index) + delta + kinds.Length) % kinds.Length]);
    }
    public void Record(RuntimeOperation operation) { if (!CanConfigure || !Enum.IsDefined(operation)) return; recording = operation; Notify(nameof(BindingRows)); InputsChanged?.Invoke(); }
    public bool Recording => recording != null;
    public bool Capture(Key key, ModifierKeys modifiers)
    {
        if (recording is not { } operation) return false;
        if (key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.System) return true;
        if (key == Key.Escape) { recording = null; Notify(nameof(BindingRows)); InputsChanged?.Invoke(); return true; }
        if (key is Key.Delete or Key.Back)
        { preferences = preferences with { Bindings = preferences.Bindings.Where(b => b.Operation != operation).ToArray() }; recording = null; Changed(); return true; }
        if ((modifiers & ModifierKeys.Windows) != 0) { Status = "Windows 키 조합은 지원하지 않습니다."; return true; }
        uint flags = (uint)(((modifiers & ModifierKeys.Alt) != 0 ? 1 : 0) | ((modifiers & ModifierKeys.Control) != 0 ? 2 : 0) | ((modifiers & ModifierKeys.Shift) != 0 ? 4 : 0));
        var binding = new RuntimeBinding(operation, (uint)KeyInterop.VirtualKeyFromKey(key), flags);
        try
        {
            binding.Validate(); var next = preferences with { Bindings = preferences.Bindings.Where(b => b.Operation != operation).Append(binding).ToArray() }; next.Validate();
            preferences = next; recording = null; Status = "키 설정 저장 · Esc 취소 / Delete 해제"; Changed();
        }
        catch (ArgumentException error) { Status = error.Message; }
        return true;
    }
    public void ReportInputStatus(string message) { if (IsActive && !Recording) Status = message; }
    public void UseSheet() { Activate(null); Changed(); }
    private void Changed()
    {
        var selections = new Dictionary<string, RuntimeSelection>(preferences.Characters);
        if (selections.TryGetValue(currentId(), out var prior)) selections[currentId()] = IsActive ? prior with { Action = action, Outfit = outfit, Weapon = weapon, Enabled = true } : prior with { Enabled = false };
        preferences = preferences with { Characters = selections };
        Notify(nameof(BindingRows)); Notify(nameof(LeftClick)); InputsChanged?.Invoke();
        if (persistent && !closing) { saveTimer.Stop(); saveTimer.Start(); }
    }
    private async void SaveTick(object? sender, EventArgs e) { saveTimer.Stop(); if (!closing) await FlushAsync(); }
    private async Task FlushAsync()
    {
        if (!persistent || !initialized) return;
        await saveGate.WaitAsync();
        try { var snapshot = preferences; if (!await Task.Run(() => store.Save(snapshot))) Status = "세트 설정 저장 실패 · 저장 공간과 권한을 확인하세요."; }
        finally { saveGate.Release(); }
    }
    public async Task CloseAsync()
    {
        closing = true; recording = null; player?.Stop(); saveTimer.Stop(); saveTimer.Tick -= SaveTick; Update(seconds(), 0, false, character.ExpressionKind);
        Notify(nameof(CanUse)); Notify(nameof(CanConfigure)); InputsChanged?.Invoke(); if (importDone != null) await importDone.Task; await FlushAsync();
    }
}
