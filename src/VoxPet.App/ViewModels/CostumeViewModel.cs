using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using VoxPet.App.Services;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.ViewModels;

public sealed class CostumeItemViewModel : ObservableObject
{
    private CostumeProfile profile;
    private ImageSource? image;
    private readonly Action<CostumeItemViewModel, string> changed;
    public CostumeProfile Profile => profile;
    public Guid Id => profile.Id;
    public CostumeCategory Category => profile.Category;
    public string CategoryName => CostumeViewModel.CategoryNames[(int)Category];
    public string Label => CategoryName + " · " + Name;
    public string Name { get => profile.Name; set { if (value == profile.Name) return; profile = profile with { Name = value }; Notify(); Notify(nameof(Label)); changed(this, nameof(Name)); } }
    public double X { get => profile.X; set { profile = profile with { X = double.IsFinite(value) ? Math.Clamp(value, -512, 1024) : profile.X }; Notify(); changed(this, nameof(X)); } }
    public double Y { get => profile.Y; set { profile = profile with { Y = double.IsFinite(value) ? Math.Clamp(value, -512, 1024) : profile.Y }; Notify(); changed(this, nameof(Y)); } }
    public double SizePercent { get => profile.Scale * 100; set { profile = profile with { Scale = double.IsFinite(value) ? Math.Clamp(value / 100, .05, 5) : profile.Scale }; Notify(); Notify(nameof(DisplayWidth)); Notify(nameof(DisplayHeight)); changed(this, nameof(SizePercent)); } }
    public double DisplayWidth => profile.BaseWidth * profile.Scale;
    public double DisplayHeight => profile.BaseHeight * profile.Scale;
    public bool Enabled { get => profile.Enabled; set { if (value == profile.Enabled) return; SetEnabled(value); changed(this, nameof(Enabled)); } }
    public bool BehindCharacter { get => profile.BehindCharacter; set { if (value == profile.BehindCharacter) return; profile = profile with { BehindCharacter = value }; Notify(); Notify(nameof(ShowInFront)); Notify(nameof(ShowBehind)); changed(this, nameof(BehindCharacter)); } }
    public int AttachmentIndex { get => (int)profile.Attachment; set { if (!Enum.IsDefined((MotionAttachment)value)) return; profile = profile with { Attachment = (MotionAttachment)value }; Notify(); changed(this, nameof(AttachmentIndex)); } }
    public double FollowPercent { get => profile.Follow * 100; set { if (!double.IsFinite(value)) return; profile = profile with { Follow = Math.Clamp(value / 100, 0, 1) }; Notify(); changed(this, nameof(FollowPercent)); } }
    public bool ShowInFront => Enabled && !BehindCharacter;
    public bool ShowBehind => Enabled && BehindCharacter;
    public ImageSource? Image { get => image; internal set => Set(ref image, value); }
    public AsyncCommand WearCommand { get; internal set; } = null!;
    internal void SetEnabled(bool value) { profile = profile with { Enabled = value }; Notify(nameof(Enabled)); Notify(nameof(ShowInFront)); Notify(nameof(ShowBehind)); }
    internal CostumeItemViewModel(CostumeProfile profile, Action<CostumeItemViewModel, string> changed) { this.profile = profile; this.changed = changed; }
}

/// <summary>의상 편집·착용은 UI 소유, 파일/디코딩은 worker. 저장한 원본 복사본을 별도 관리한다.</summary>
public sealed class CostumeViewModel : ObservableObject
{
    public string[] AttachmentNames { get; } = ["분류에 맞춰 자동 연결", "몸통", "머리", "입 (머리도 함께)", "화면 왼쪽 손", "화면 오른쪽 손 / 날개", "몸통 + 양쪽 소매"];
    public static string[] CategoryNames { get; } = ["모자", "안경", "얼굴장식", "귀걸이", "상의", "하의", "겉옷", "한벌옷", "망토"];
    private readonly CostumeStore store;
    private readonly bool persistent;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<Guid, byte[]> memory = [];
    private bool busy, closing;
    private bool dirty;
    private long placementRevision;
    private readonly System.Windows.Threading.DispatcherTimer saveTimer;
    private CostumeItemViewModel? selected;
    private CostumeCategory importCategory;
    private int filterIndex;
    private string status = "이미지를 추가하고 클릭해 착용하세요. 배치는 자동 저장됩니다.";
    public ObservableCollection<CostumeItemViewModel> Items { get; } = [];
    public IEnumerable<CostumeItemViewModel> Filtered => Items.Where(item => FilterIndex == 0 || (int)item.Category == FilterIndex - 1);
    public string[] Filters { get; } = ["전체", .. CategoryNames];
    public KeyValuePair<CostumeCategory, string>[] Categories { get; } = Enum.GetValues<CostumeCategory>().Select(category => new KeyValuePair<CostumeCategory, string>(category, CategoryNames[(int)category])).ToArray();
    public int FilterIndex { get => filterIndex; set { if (Set(ref filterIndex, Math.Clamp(value, 0, CategoryNames.Length))) { Notify(nameof(Filtered)); selected = Filtered.FirstOrDefault(); Notify(nameof(Selected)); Refresh(); } } }
    public CostumeCategory ImportCategory { get => importCategory; set => Set(ref importCategory, value); }
    public CostumeItemViewModel? Selected { get => selected; set { if (value == null || !Items.Contains(value)) return; if (Set(ref selected, value)) Refresh(); } }
    public bool CanEdit => !busy && !closing;
    public bool CanAdjust => CanEdit && Selected != null;
    public string Status { get => status; private set => Set(ref status, value); }
    public AsyncCommand ImportCommand { get; }
    public AsyncCommand SaveCommand { get; }
    public AsyncCommand DeleteCommand { get; }
    public RelayCommand RemoveAllCommand { get; }
    public RelayCommand UpLayerCommand { get; }
    public RelayCommand DownLayerCommand { get; }
    public RelayCommand LeftCommand { get; }
    public RelayCommand RightCommand { get; }
    public RelayCommand UpCommand { get; }
    public RelayCommand DownCommand { get; }
    public event Func<string?>? ChooseImage;
    public CostumeViewModel(bool persistent, string? folder = null)
    {
        this.persistent = persistent; store = new(folder ?? Path.Combine(AppPaths.DataFolder, "Wardrobe"));
        saveTimer = new(System.Windows.Threading.DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(350) };
        saveTimer.Tick += AutoSaveTick;
        ImportCommand = new(async () => { if (ChooseImage?.Invoke() is { } path) await ImportAsync(path); }, () => CanEdit);
        SaveCommand = new(SaveAsync, () => CanEdit);
        DeleteCommand = new(DeleteAsync, () => CanAdjust);
        RemoveAllCommand = new(() => { foreach (var item in Items) item.SetEnabled(false); ReleaseUnused(); ScheduleSave(); Status = "모두 해제했습니다. 자동 저장됩니다."; }, () => CanEdit);
        UpLayerCommand = new(() => MoveLayer(1), () => CanAdjust && Items.IndexOf(Selected!) < Items.Count - 1);
        DownLayerCommand = new(() => MoveLayer(-1), () => CanAdjust && Items.IndexOf(Selected!) > 0);
        LeftCommand = new(() => Selected!.X -= 1, () => CanAdjust); RightCommand = new(() => Selected!.X += 1, () => CanAdjust);
        UpCommand = new(() => Selected!.Y -= 1, () => CanAdjust); DownCommand = new(() => Selected!.Y += 1, () => CanAdjust);
    }
    private void Refresh()
    {
        Notify(nameof(CanEdit)); Notify(nameof(CanAdjust));
        foreach (var command in new[] { ImportCommand, SaveCommand, DeleteCommand }) command.Refresh();
        foreach (var command in new[] { RemoveAllCommand, UpLayerCommand, DownLayerCommand, LeftCommand, RightCommand, UpCommand, DownCommand }) command.Refresh();
        foreach (var item in Items) item.WearCommand.Refresh();
    }
    private void SetBusy(bool value) { busy = value; Refresh(); }
    private CostumeItemViewModel Create(CostumeProfile profile)
    {
        var item = new CostumeItemViewModel(profile, (entry, property) =>
        {
            if (property == nameof(entry.Enabled)) _ = WearAsync(entry, entry.Enabled);
            else { Status = "미리보기에 반영했습니다. 마지막 배치를 자동 저장합니다."; ScheduleSave(); }
        });
        item.WearCommand = new(() => WearAsync(item, true), () => CanEdit); return item;
    }
    private static bool Expected(Exception error) => error is IOException or UnauthorizedAccessException or ArgumentException or FormatException or NotSupportedException or InvalidOperationException or COMException;
    private async Task<ImageSource> ReadAsync(Guid id) => await Task.Run(() => CostumeImageLoader.Load(persistent ? store.ReadImage(id) : memory[id]).Image);
    private void ReleaseUnused() { foreach (var item in Items.Where(item => !item.Enabled && !ReferenceEquals(item, Selected))) item.Image = null; }
    private void ApplyConflicts(CostumeItemViewModel item)
    {
        foreach (var other in Items.Where(other => !ReferenceEquals(other, item) && CostumeProfile.Conflicts(item.Category, other.Category))) other.SetEnabled(false);
    }
    public async Task InitializeAsync()
    {
        if (!persistent) return;
        SetBusy(true); await gate.WaitAsync();
        try
        {
            var loaded = await Task.Run(store.Load); if (closing) return;
            foreach (var profile in loaded.Items) Items.Add(Create(profile));
            foreach (var item in Items.Where(item => item.Enabled).ToArray())
            {
                if (!item.Enabled) continue;
                try { item.Image = await ReadAsync(item.Id); ApplyConflicts(item); }
                catch (Exception error) when (Expected(error)) { item.SetEnabled(false); Status = "일부 보관 이미지를 읽지 못했습니다. 해당 원본을 다시 추가하세요. 기존 파일은 보존합니다."; }
            }
            selected = Items.FirstOrDefault(); Notify(nameof(Selected)); Notify(nameof(Filtered));
            if (loaded.Recovered) Status = "보관함 설정을 읽지 못했습니다. 관리 이미지 파일은 보존합니다.";
        }
        finally { gate.Release(); SetBusy(false); }
    }
    public async Task<bool> ImportAsync(string path)
    {
        if (!CanEdit || Items.Count >= 100) { Status = "처리 중이거나 보관함100개 제한입니다."; return false; }
        var category = ImportCategory; SetBusy(true); await gate.WaitAsync();
        try
        {
            var loaded = await Task.Run(() =>
            {
                using var file = File.OpenRead(path); if (file.Length is < 16 or > 16 * 1024 * 1024) throw new ArgumentException("이미지 크기");
                var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes); return (Bytes: bytes, Decoded: CostumeImageLoader.Load(bytes));
            });
            if (closing) return false;
            double width = category switch { CostumeCategory.Hat or CostumeCategory.Glasses => 200, CostumeCategory.FaceDecoration => 140, CostumeCategory.Earrings => 300, CostumeCategory.Cape => 320, _ => 230 };
            double height = width * loaded.Decoded.Height / loaded.Decoded.Width;
            if (height > 512) { width *= 512 / height; height = 512; }
            var profile = new CostumeProfile(Guid.NewGuid(), Path.GetFileNameWithoutExtension(path)[..Math.Min(60, Path.GetFileNameWithoutExtension(path).Length)], category,
                (512 - width) / 2, category switch { CostumeCategory.Hat => 20, CostumeCategory.Glasses or CostumeCategory.Earrings => 140, CostumeCategory.FaceDecoration => 210, CostumeCategory.Bottom => 370, CostumeCategory.Cape => 180, _ => 280 }, width, height, BehindCharacter: category == CostumeCategory.Cape);
            var snapshot = Items.Select(item => item.Profile with { Enabled = item.Enabled && !CostumeProfile.Conflicts(category, item.Category) }).Append(profile).ToArray();
            CostumeStore.Validate(snapshot);
            if (persistent) await Task.Run(() => store.Import(snapshot, profile.Id, loaded.Bytes)); else memory.Add(profile.Id, loaded.Bytes);
            if (closing) return false;
            var added = Create(profile); added.Image = loaded.Decoded.Image; Items.Add(added); ApplyConflicts(added); selected = added;
            if (FilterIndex != 0 && FilterIndex != (int)category + 1) { filterIndex = 0; Notify(nameof(FilterIndex)); }
            Notify(nameof(Selected)); Notify(nameof(Filtered)); ReleaseUnused(); Status = "보관함에 저장하고 착용했습니다. 크기/위치/움직임 연결은 자동 저장됩니다."; return true;
        }
        catch (Exception error) when (Expected(error)) { Status = "이미지를 추가하지 못했습니다. PNG/JPG/BMP/GIF,16MB 이하·4096px/800만 화소 이하인지 확인하세요. 기존 의상은 유지합니다."; return false; }
        finally { gate.Release(); SetBusy(false); }
    }
    public async Task WearAsync(CostumeItemViewModel item, bool enabled)
    {
        if (closing || !Items.Contains(item)) return;
        selected = item; Notify(nameof(Selected)); SetBusy(true); await gate.WaitAsync();
        try
        {
            if (closing) return;
            if (enabled)
            {
                if (Items.Count(other => other.Enabled && !ReferenceEquals(other, item) && !CostumeProfile.Conflicts(item.Category, other.Category)) >= 24) throw new ArgumentException("착용24개 제한");
                item.Image ??= await ReadAsync(item.Id); if (closing) return; ApplyConflicts(item);
            }
            item.SetEnabled(enabled); ReleaseUnused(); Status = enabled ? "착용했습니다. 위치/크기 조정은 미리보기에 즉시 반영됩니다. 자동 저장됩니다." : "해제했습니다. 자동 저장됩니다.";
        }
        catch (Exception error) when (Expected(error)) { item.SetEnabled(false); Status = "이미지를 읽지 못했거나 착용24개 제한입니다. 기존 보관 파일은 유지합니다."; }
        finally { gate.Release(); SetBusy(false); if (!closing) ScheduleSave(); }
    }
    private void MoveLayer(int delta)
    {
        if (!CanAdjust) return; int current = Items.IndexOf(Selected!), next = current + delta;
        if (next < 0 || next >= Items.Count) return; Items.Move(current, next); ScheduleSave(); Notify(nameof(Filtered)); Notify(nameof(Selected)); Refresh(); Status = "겹치는 순서를 바꿨습니다. 목록의 아래 항목이 앞에 표시됩니다. 자동 저장됩니다.";
    }
    public async Task SaveAsync()
    {
        if (!CanEdit) return; var snapshot = Items.Select(item => item.Profile).ToArray(); SetBusy(true); await gate.WaitAsync();
        try { CostumeStore.Validate(snapshot); if (persistent) await Task.Run(() => store.Save(snapshot)); Status = "의상/착용/배치/순서를 저장했습니다. 다음 실행에도 유지됩니다."; }
        catch (Exception error) when (Expected(error)) { Status = "저장하지 못했습니다. 이름/저장 공간을 확인하세요. 이전 저장값은 유지합니다."; }
        finally { gate.Release(); SetBusy(false); }
    }
    private async Task DeleteAsync()
    {
        if (!CanAdjust) return; var item = Selected!; var remaining = Items.Where(other => !ReferenceEquals(item, other)).Select(other => other.Profile).ToArray(); SetBusy(true); await gate.WaitAsync();
        try
        {
            if (persistent) await Task.Run(() => store.Delete(remaining, item.Id)); else memory.Remove(item.Id);
            Items.Remove(item); selected = Filtered.FirstOrDefault(); Notify(nameof(Selected)); Notify(nameof(Filtered)); Status = "보관 복사본을 삭제했습니다. 사용자의 원본 이미지는 유지합니다.";
        }
        catch (Exception error) when (Expected(error)) { Status = "삭제하지 못했습니다. 기존 보관함을 유지합니다."; }
        finally { gate.Release(); SetBusy(false); }
    }
    private void ScheduleSave() { dirty = true; placementRevision++; if (persistent && !closing) { saveTimer.Stop(); saveTimer.Start(); } }
    private async void AutoSaveTick(object? sender, EventArgs e) { saveTimer.Stop(); if (!closing && dirty) await FlushPlacementAsync(); }
    private async Task FlushPlacementAsync()
    {
        await gate.WaitAsync();
        try { long revision = placementRevision; var snapshot = Items.Select(item => item.Profile).ToArray(); CostumeStore.Validate(snapshot); if (persistent) await Task.Run(() => store.Save(snapshot)); if (revision == placementRevision) dirty = false; }
        catch (Exception error) when (Expected(error)) { Status = "배치를 저장하지 못했습니다. 이름/저장 공간을 확인하세요. 이전 저장값은 유지합니다."; }
        finally { gate.Release(); }
    }
    public async Task CloseAsync() { closing = true; saveTimer.Stop(); saveTimer.Tick -= AutoSaveTick; Refresh(); await gate.WaitAsync(); gate.Release(); if (dirty) await FlushPlacementAsync(); }
}
