using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using VoxPet.App.Services;
using VoxPet.App.Views;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using CharacterCatalogEntry = VoxPet.Core.Models.CharacterDefinition;

namespace VoxPet.App.ViewModels;

public sealed record CharacterCard(string LibraryId, string Name, string Id, string Version, string Author, string Description, ImageSource? Image, CharacterCatalogEntry? Legacy = null, bool Sample = false)
{
    // UI text search/automation must never stringify a worker-owned image decoder.
    public override string ToString() => Name;
}
/// <summary>Dashboard/library/편집 navigation. 기존 settings 화면은 Main의 고수준 navigation으로 연결한다.</summary>
public sealed class StudioShellViewModel : ObservableObject
{
    public MainViewModel Main { get; }
    public CharacterEditorViewModel Editor { get; } = new();
    public string[] Navigation { get; } = ["홈", "캐릭터", "의상 / 부착물", "동작", "방송 / 설정", "캐릭터 편집기"];
    private readonly bool persistent;
    private bool closing, busy;
    private int page;
    private string search = "", status = "";
    private CharacterCard? selected;
    public ObservableCollection<CharacterCard> Cards { get; } = [];
    public IEnumerable<CharacterCard> Filtered => Cards.Where(c => c.Name.Contains(Search, StringComparison.CurrentCultureIgnoreCase) || c.Id.Contains(Search, StringComparison.OrdinalIgnoreCase));
    public string Search { get => search; set { if (Set(ref search, value)) Notify(nameof(Filtered)); } }
    public int Page { get => page; set { if (Set(ref page, value)) { Notify(nameof(IsHome)); Notify(nameof(IsLibrary)); Notify(nameof(IsEditor)); Notify(nameof(IsAttachments)); Notify(nameof(IsActions)); Notify(nameof(IsSettings)); if (IsEditor) Editor.StartPreview(); else Editor.PausePreview(); if (value == 4) SettingsRequested?.Invoke(Main.SettingsTab); } } }
    public bool IsHome => page == 0;
    public bool IsLegacy => !Main.Packages.IsActive;
    public bool IsSheet => !Main.Packages.IsActive && !Main.Runtime.IsActive;
    public bool IsImageSet => !Main.Packages.IsActive && Main.Runtime.IsActive;
    public bool IsAttachments => page == 2;
    public bool IsActions => page == 3;
    public bool IsSettings => page == 4;
    public string[] SettingsSections => Main.Packages.IsActive ? ["마이크", "음성 반응", "방송 / 캐릭터"] : ["마이크", "음성 반응", "방송 / 캐릭터", "표정 슬롯", "눈 깜빡임 / 눈물", "의상 배치", "손동작 / 입력"];
    public string CurrentCharacter => Main.Character.Name;
    public string PackageStatus => Main.Packages.Status;
    public IReadOnlyList<VoxPet.Core.Attachments.AttachmentDefinition> LiveAttachments => Main.Packages.Scene?.Runtime.Definition.Attachments ?? [];
    public IReadOnlyList<VoxPet.Core.Actions.ActionDefinition> LiveActions => Main.Packages.Scene?.Runtime.Definition.Actions ?? [];
    public IReadOnlyList<VoxPet.Core.Character.ExpressionDefinition> LiveExpressions => Main.Packages.Scene?.Runtime.Definition.Expressions ?? [];
    public bool IsLibrary => page == 1;
    public bool IsEditor => page == 5;
    public CharacterCard? Selected { get => selected; set { if (Set(ref selected, value)) RefreshCommands(); } }
    public string Status { get => status; private set => Set(ref status, value); }
    public string RuntimeState => Main.Packages.Scene is { } s ? $"표정 {s.Runtime.State.ExpressionId} · 입 {s.Runtime.MouthFrame + 1} · 동작 {s.Runtime.State.ActionId ?? "idle"} · 우선순위 {s.Runtime.Actions.Current.Priority}" : $"표정 {Main.Character.ExpressionKind} · 입 {Main.Character.MouthFrameCount}단계 · 동작 {Main.Character.Motion.Gesture?.ToString() ?? "idle"}";
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand ImportCommand { get; }
    public AsyncCommand NewCommand { get; }
    public AsyncCommand SelectCommand { get; }
    public AsyncCommand EditCommand { get; }
    public AsyncCommand ExportCommand { get; }
    public AsyncCommand DeleteCommand { get; }
    public RelayCommand QuickPlayCommand { get; }
    public AsyncCommand SampleCommand { get; }
    public RelayCommand StopActionsCommand { get; }
    public RelayCommand RemoveAttachmentsCommand { get; }
    public ObservableCollection<StudioAttachment> AttachmentCards { get; } = [];
    public static string SamplePath => Path.Combine(AppContext.BaseDirectory, "Assets", "Samples", "studio-samples.voxpet");
    public const string SampleId = "voxpet.samples.studio";
    public const string SampleVersion = "1.1.0";
    private async Task<LoadedCharacterPackage?> ReadCardAsync(CharacterCard card)
    {
        try { return card.Sample ? await Task.Run(() => CharacterPackageLibrary.Decode(new VoxPetPackageLoader().Load(SamplePath))) : await Main.Packages.LoadForEditorAsync(card.LibraryId); }
        catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { Status = "샘플 파일을 읽지 못했습니다. Assets/Samples 폴더를 확인하세요."; return null; }
    }
    public async Task UseSamplesAsync()
    {
        if (closing) return;
        int destination = Page == 3 ? 3 : 2;
        await Main.Packages.RefreshAsync(); if (closing) return;
        var existing = Main.Packages.Entries.FirstOrDefault(e => e.CharacterId == SampleId && e.Version == SampleVersion);
        bool ok = existing != null && await Main.Packages.SelectAsync(existing.LibraryId);
        if (!ok && !closing) ok = await Main.Packages.ImportAsync(SamplePath);
        Status = Main.Packages.Status;
        if (ok && !closing) { Page = destination; UpdateAttachmentCards(); await RefreshAsync(); if (!closing) Selected = Cards.FirstOrDefault(c => c.Sample); }
    }
    private void UpdateAttachmentCards()
    {
        AttachmentCards.Clear();
        if (Main.Packages.Loaded is not { } loaded || Main.Packages.Scene is not { } scene) return;
        foreach (var item in scene.Runtime.Definition.Attachments)
        {
            var layer = item.Layers.FirstOrDefault();
            AttachmentCards.Add(new StudioAttachment(item.Id, item.Name, item.Category, layer != null ? loaded.Images[layer.Asset] : null,
                () => scene.Runtime.Attachments.EnabledIds.Contains(item.Id), () => { if (!Main.Packages.CanReceiveInput) return; scene.Runtime.Attachments.SetEnabled(item.Id, !scene.Runtime.Attachments.EnabledIds.Contains(item.Id)); foreach (var card in AttachmentCards) card.Refresh(); }));
        }
    }
    public event Func<string?>? ChoosePackage;
    public event Func<string?>? ChooseExport;
    public event Action<int>? SettingsRequested;
    public StudioShellViewModel(MainViewModel main, bool persistent)
    {
        Main = main; this.persistent = persistent;
        SampleCommand = new(UseSamplesAsync, () => !busy && !closing && !Main.Packages.IsBusy);
        StopActionsCommand = new(() => { Main.Packages.Scene?.Stop(); Main.Hands.StopCommand.Execute(null); }, () => !closing);
        RemoveAttachmentsCommand = new(() => { if (Main.Packages.Scene is { } scene) foreach (string id in scene.Runtime.Attachments.EnabledIds.ToArray()) scene.Runtime.Attachments.SetEnabled(id, false); foreach (var card in AttachmentCards) card.Refresh(); if (!Main.Packages.IsActive) Main.Wardrobe.RemoveAllCommand.Execute(null); }, () => !closing);
        RefreshCommand = new(RefreshAsync, () => !busy && !closing);
        ImportCommand = new(async () => { if (ChoosePackage?.Invoke() is { } path) { await Main.Packages.ImportAsync(path); Status = Main.Packages.Status; await RefreshAsync(); } }, () => !busy && !closing);
        NewCommand = new(async () => { await Editor.NewAsync(); Page = 5; }, () => !busy && !closing);
        SelectCommand = new(async () => { if (Selected == null) return; if (Selected.Sample) { await UseSamplesAsync(); return; } if (Selected.Legacy is { } legacy) await Main.SelectCharacterAsync(legacy.Id); else await Main.Packages.SelectAsync(Selected.LibraryId); Status = Main.Packages.IsActive ? Main.Packages.Status : Main.CharacterStatus; }, () => Selected != null && !busy && !closing);
        EditCommand = new(async () =>
        {
            if (Selected == null) return;
            if (Selected.Legacy != null) { Status = "기존 PNG 시트와 설정은 방송 / 설정에서 관리하세요. 새 분리 캐릭터는 새 캐릭터로 시작할 수 있습니다."; return; }
            var loaded = await ReadCardAsync(Selected); if (loaded != null && await Editor.OpenPackageAsync(loaded.Package)) Page = 5;
        }, () => Selected?.Legacy == null && Selected != null && !busy && !closing);
        ExportCommand = new(async () => { if (Selected == null || ChooseExport?.Invoke() is not { } path) return; var loaded = await ReadCardAsync(Selected); if (loaded != null) try { await Task.Run(() => new VoxPetPackageExporter().Export(loaded.Package, path)); Status = "캐릭터 내보내기 완료"; } catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { Status = "내보내기 실패 · " + e.Message; } }, () => Selected?.Legacy == null && Selected != null && !busy && !closing);
        DeleteCommand = new(async () => { if (Selected != null) { await Main.Packages.DeleteAsync(Selected.LibraryId); Status = Main.Packages.Status; await RefreshAsync(); } }, () => Selected?.Legacy == null && Selected != null && !Selected.Sample && !busy && !closing);
        QuickPlayCommand = new(() => { if (Main.Packages.Scene is { } scene && scene.Runtime.Definition.Actions.FirstOrDefault() is { } action) Main.Packages.Play(action.Id); else Main.Hands.PreviewCommand.Execute(null); }, () => !closing);
        Main.Packages.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.Packages.Scene)) { Notify(nameof(LiveAttachments)); Notify(nameof(LiveActions)); Notify(nameof(LiveExpressions)); Notify(nameof(RuntimeState)); Notify(nameof(IsLegacy)); Notify(nameof(IsSheet)); Notify(nameof(IsImageSet)); Notify(nameof(SettingsSections)); UpdateAttachmentCards(); if (Main.Packages.IsActive && Main.SettingsTab > 2) Main.SettingsTab = 0; } if (e.PropertyName == nameof(Main.Packages.Status)) Notify(nameof(PackageStatus)); SampleCommand.Refresh(); };
        Main.Character.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.Character.RuntimeScene)) Notify(nameof(RuntimeState)); if (e.PropertyName == nameof(Main.Character.Name)) Notify(nameof(CurrentCharacter)); };
        Main.Runtime.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.Runtime.IsActive)) { Notify(nameof(IsSheet)); Notify(nameof(IsImageSet)); } };
        Main.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.VoiceLevel)) Notify(nameof(RuntimeState)); };
    }
    private void RefreshCommands() { foreach (var c in new[] { RefreshCommand, ImportCommand, NewCommand, SelectCommand, EditCommand, ExportCommand, DeleteCommand, SampleCommand }) c.Refresh(); }
    public async Task RefreshAsync()
    {
        if (closing || busy) return; busy = true; RefreshCommands(); string? id = Selected?.LibraryId ?? (Main.Packages.Loaded?.Package.Manifest is { Id: SampleId, Version: SampleVersion } ? "builtin-sample" : Main.Packages.Loaded?.LibraryId ?? "legacy-" + Main.SelectedCharacter.Id);
        try
        {
            await Main.Packages.RefreshAsync(); var cards = new List<CharacterCard>();
            foreach (var legacy in Main.Characters)
            {
                ImageSource? image = null;
                string path = Path.Combine(Main.CharacterFolder, legacy.FileName);
                try { if (File.Exists(path)) image = await Task.Run(() => LegacyCharacterImporter.Load(path)[0, 1]); }
                catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { /* Original remains available to the legacy recovery flow. */ }
                cards.Add(new("legacy-" + legacy.Id, legacy.Name, legacy.Id, "v1", "", "기존 PNG 시트 · 기존 설정 유지", image, legacy));
            }
            if (File.Exists(SamplePath))
            {
                try
                {
                    var sample = await Task.Run(() => CharacterPackageLibrary.Decode(new VoxPetPackageLoader().Load(SamplePath)));
                    cards.Add(new("builtin-sample", sample.Package.Manifest.Name, SampleId, sample.Package.Manifest.Version, "VoxPet · CC0", "14분류·의상17종(헤어3종)/동작9종 · 움직이는 모자와 날개", sample.Images[sample.Package.Manifest.Thumbnail!], Sample: true));
                }
                catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { Status = "샘플을 읽지 못했습니다. Assets/Samples 폴더를 확인하세요. 기존 캐릭터는 유지합니다."; }
            }
            foreach (var entry in Main.Packages.Entries.Where(e => e.CharacterId != SampleId || e.Version != SampleVersion))
            {
                try
                {
                    var loaded = await Main.Packages.LoadForEditorAsync(entry.LibraryId); if (loaded == null) continue;
                    var m = loaded.Package.Manifest; ImageSource? image = m.Thumbnail is { } t ? loaded.Images[t] : await Task.Run(() =>
                    {
                        var runtime = loaded.Package.CreateRuntime(new Random(1)); var frame = runtime.Update(new(0, 0, 0, false));
                        var group = new DrawingGroup(); using (var drawing = group.Open()) RenderFrameDrawing.Draw(drawing, frame, loaded.Images);
                        group.Freeze(); var thumbnail = new DrawingImage(group); thumbnail.Freeze(); return thumbnail;
                    });
                    cards.Add(new(entry.LibraryId, m.Name, m.Id, m.Version, m.Author, m.Description, image));
                }
                catch (Exception e) when (CharacterPackageLibrary.Expected(e) || e is InvalidOperationException)
                { Status = "일부 캐릭터의 미리보기를 만들지 못했습니다. 기존 원본은 유지합니다."; }
            }
            if (closing) return; Cards.Clear(); foreach (var card in cards) Cards.Add(card); Selected = Cards.FirstOrDefault(c => c.LibraryId == id) ?? Cards.FirstOrDefault(); Notify(nameof(Filtered));
        }
        finally { busy = false; RefreshCommands(); }
    }
    public async Task CloseAsync() { closing = true; await Editor.CloseAsync(); RefreshCommands(); }
}

public sealed class StudioAttachment(string id, string name, string category, ImageSource? image, Func<bool> worn, Action toggle) : ObservableObject
{
    public string Id => id;
    public string Name => name;
    public string Category => category;
    public ImageSource? Image => image;
    public bool IsWorn => worn();
    public string WearLabel => IsWorn ? "해제" : "착용";
    public RelayCommand ToggleCommand { get; } = new(toggle);
    public void Refresh() { Notify(nameof(IsWorn)); Notify(nameof(WearLabel)); }
}
