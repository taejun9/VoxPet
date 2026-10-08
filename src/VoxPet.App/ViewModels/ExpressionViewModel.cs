using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using VoxPet.App.Services;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.ViewModels;

/// <summary>슬롯의 편집 초안과 마지막 저장값을 분리한다. 단축키는 마지막 저장값을 사용한다.</summary>
public sealed class ExpressionSlotViewModel : ObservableObject
{
    private ExpressionProfile draft;
    public ExpressionProfile Saved { get; private set; }
    public ImageSource[,]? DraftSheet { get; private set; }
    public int Index { get; }
    public string Label => $"F{Index + 1} · {Saved.Name}";
    public ExpressionSlotViewModel(int index, ExpressionProfile profile) { Index = index; draft = Saved = profile; }
    public ExpressionProfile Draft => draft;
    public string Name { get => draft.Name; set { draft = draft with { Name = value }; Notify(); } }
    public ExpressionKind Kind { get => draft.Kind; set { draft = draft with { Kind = value }; Notify(); } }
    public bool Blink { get => draft.Blink; set { draft = draft with { Blink = value }; Notify(); } }
    public bool Tears { get => draft.Tears; set { draft = draft with { Tears = value }; Notify(); } }
    public double TearLeft { get => draft.TearLeft; set { draft = draft with { TearLeft = value }; Notify(); } }
    public double TearRight { get => draft.TearRight; set { draft = draft with { TearRight = value }; Notify(); } }
    public double TearTop { get => draft.TearTop; set { draft = draft with { TearTop = value }; Notify(); } }
    public string SheetHint => DraftSheet != null || draft.SheetId != null ? "사용자 PNG · 기본 표정으로 바꾸려면 아래 버튼을 누르세요." : "내장 표정 · PNG 시트를 슬롯별로 저장할 수도 있습니다.";
    public RelayCommand SwitchCommand { get; set; } = null!;
    public void UseSheet(ImageSource[,] sheet) { DraftSheet = sheet; draft = draft with { SheetId = null }; Notify(nameof(SheetHint)); }
    public void UseBuiltin() { DraftSheet = null; draft = draft with { SheetId = null }; Notify(nameof(SheetHint)); }
    public void MarkSaved(ExpressionProfile snapshot)
    {
        // 저장 중 편집을 막는 UI와 함께 사용한다. 저장된 이미지는 다음 호출부터 관리 복사본으로 읽는다.
        Saved = draft = snapshot; DraftSheet = null;
        foreach (string property in new[] { nameof(Label), nameof(SheetHint), nameof(Name), nameof(Kind), nameof(Blink), nameof(Tears), nameof(TearLeft), nameof(TearRight), nameof(TearTop) }) Notify(property);
    }
}

/// <summary>로컬 슬롯 IO를 작업 스레드에 두고 최신 전환 요청만 UI에 적용한다.</summary>
public sealed class ExpressionViewModel : ObservableObject
{
    private readonly CharacterViewModel character;
    private readonly ExpressionSlotStore store;
    private readonly bool persistent;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<int, ImageSource[,]> memorySheets = [];
    private long revision;
    private bool closing, busy;
    private ExpressionSlotViewModel selected;
    private string status = "슬롯을 편집한 뒤 저장하세요. Ctrl+Shift+F1~F11은 다른 앱에서도 전환합니다.";
    private string hotkeyStatus = "단축키 준비 중…";
    public CharacterViewModel Character => character;
    public ExpressionSlotViewModel[] Slots { get; }
    public ExpressionSlotViewModel Selected { get => selected; set => Set(ref selected, value); }
    public bool CanEdit => !busy && !closing;
    public string Status { get => status; private set => Set(ref status, value); }
    public string HotkeyStatus { get => hotkeyStatus; set => Set(ref hotkeyStatus, value); }
    public KeyValuePair<ExpressionKind, string>[] Kinds { get; } = [
        new(ExpressionKind.Neutral, "평상"), new(ExpressionKind.Happy, "기쁨"), new(ExpressionKind.Sad, "슬픔"),
        new(ExpressionKind.Angry, "화남"), new(ExpressionKind.Surprised, "놀람"), new(ExpressionKind.Sleepy, "졸림")];
    public AsyncCommand SaveCommand { get; }
    public AsyncCommand PreviewCommand { get; }
    public AsyncCommand ImportCommand { get; }
    public RelayCommand BuiltinCommand { get; }
    public event Func<string?>? ChooseSheet;

    public ExpressionViewModel(CharacterViewModel character, bool persistent, string? folder = null)
    {
        this.character = character; this.persistent = persistent;
        store = new(folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoxPet", "Expressions"));
        Slots = Enumerable.Range(0, 12).Select(i => new ExpressionSlotViewModel(i, ExpressionProfile.Default(i))).ToArray();
        selected = Slots[0];
        foreach (var slot in Slots) slot.SwitchCommand = new(() => _ = ActivateAsync(slot.Index), () => !closing);
        SaveCommand = new(SaveAsync, () => CanEdit);
        PreviewCommand = new(() => PreviewAsync(), () => CanEdit);
        ImportCommand = new(ImportAsync, () => CanEdit);
        BuiltinCommand = new(() => { Selected.UseBuiltin(); Status = "내장 표정을 선택했습니다. 미리보기 후 저장하세요."; }, () => CanEdit);
    }
    public async Task InitializeAsync()
    {
        if (!persistent) return;
        var profiles = await Task.Run(() => Enumerable.Range(0, 12).Select(store.Load).ToArray());
        if (closing) return;
        for (int i = 0; i < 12; i++) Slots[i].MarkSaved(profiles[i]);
        await ActivateAsync(0);
    }
    private void SetBusy(bool value)
    {
        busy = value; Notify(nameof(CanEdit));
        SaveCommand.Refresh(); PreviewCommand.Refresh(); ImportCommand.Refresh(); BuiltinCommand.Refresh();
    }
    private static bool Expected(Exception ex) => ex is IOException or UnauthorizedAccessException or ArgumentException or
        NotSupportedException or FormatException or OverflowException or COMException;
    private async Task<ImageSource[,]?> LoadSheetAsync(ExpressionProfile profile)
    {
        return profile.SheetId == null ? null : await Task.Run(() => CharacterSheetLoader.Load(store.ReadSheet(profile)));
    }
    /// <summary>연속 입력의 오래된 IO 결과를 버린다. 실패하면 표시 중인 표정을 유지한다.</summary>
    public async Task ActivateAsync(int index)
    {
        if (closing || index is < 0 or >= 12) return;
        long request = ++revision;
        await gate.WaitAsync();
        try
        {
            if (closing || request != revision) return;
            var slot = Slots[index]; var profile = slot.Saved;
            var sheet = !persistent && profile.SheetId != null && memorySheets.TryGetValue(index, out var cached) ? cached : await LoadSheetAsync(profile);
            if (closing || request != revision) return;
            character.Apply(profile, sheet); Selected = slot;
            Status = $"{slot.Label} · 표정 전환 완료";
        }
        catch (Exception ex) when (Expected(ex))
        {
            if (!closing && request == revision) Status = "저장된 시트를 읽지 못했습니다. 현재 표정을 유지합니다. 슬롯에 PNG를 다시 저장하거나 내장 표정을 저장하세요.";
        }
        finally { gate.Release(); }
    }
    public async Task PreviewAsync()
    {
        var slot = Selected; long request = ++revision; SetBusy(true);
        await gate.WaitAsync();
        try
        {
            slot.Draft.Validate();
            var sheet = slot.DraftSheet ?? (!persistent && slot.Draft.SheetId != null && memorySheets.TryGetValue(slot.Index, out var cached) ? cached : await LoadSheetAsync(slot.Draft));
            if (closing || request != revision) return;
            character.Apply(slot.Draft, sheet); Status = "편집한 표정 미리보기 · 단축키에 반영하려면 저장하세요.";
        }
        catch (Exception ex) when (Expected(ex)) { if (!closing) Status = "미리보기에 실패했습니다. 이름과 PNG 시트를 확인하세요. 현재 표정을 유지합니다."; }
        finally { gate.Release(); SetBusy(false); }
    }
    private async Task ImportAsync()
    {
        string? path = ChooseSheet?.Invoke();
        if (path != null) await ImportAsync(path);
    }
    public async Task<bool> ImportAsync(string path)
    {
        if (!CanEdit) return false;
        var slot = Selected; SetBusy(true);
        try
        {
            var sheet = await Task.Run(() => CharacterSheetLoader.Load(path));
            if (closing) return false;
            slot.UseSheet(sheet); Status = "시트 적용 준비 완료 · 눈물 위치를 조정하고 미리보기·저장을 누르세요.";
            return true;
        }
        catch (Exception ex) when (Expected(ex)) { if (!closing) Status = "PNG를 읽지 못했습니다. 16MB 이하 3열 또는8열×2행 RGBA 시트를 확인하세요. 기존 슬롯을 유지합니다."; return false; }
        finally { SetBusy(false); }
    }
    public async Task SaveAsync()
    {
        if (!CanEdit) return;
        var slot = Selected; var profile = slot.Draft; var sheet = slot.DraftSheet; long request = ++revision; SetBusy(true);
        await gate.WaitAsync();
        try
        {
            profile.Validate();
            // 편집 실패뿐 아니라 손상된 기존 시트도 저장 전 확인한다.
            var rendered = sheet ?? (!persistent && profile.SheetId != null && memorySheets.TryGetValue(slot.Index, out var cached) ? cached : await LoadSheetAsync(profile));
            if (closing) return;
            var saved = persistent ? await Task.Run(() => store.Save(slot.Index, profile, sheet == null ? null : CharacterViewModel.EncodeSheet(sheet))) : profile;
            if (!persistent)
            {
                if (rendered == null) memorySheets.Remove(slot.Index);
                else { memorySheets[slot.Index] = rendered; saved = saved with { SheetId = Guid.NewGuid().ToString("N") }; }
            }
            slot.MarkSaved(saved);
            if (closing || request != revision) return;
            character.Apply(saved, rendered); Status = $"{slot.Label} · {(persistent ? "로컬 저장 완료 · 다음 실행에도 유지됩니다." : "QA 메모리 저장 완료")}";
        }
        catch (Exception ex) when (Expected(ex)) { if (!closing) Status = "슬롯을 저장하지 못했습니다. 이름·파일·저장 공간을 확인하세요. 이전 저장값을 유지합니다."; }
        finally { gate.Release(); SetBusy(false); }
    }
    public async Task CloseAsync()
    {
        closing = true; ++revision; SetBusy(true);
        await gate.WaitAsync(); gate.Release();
    }
}
