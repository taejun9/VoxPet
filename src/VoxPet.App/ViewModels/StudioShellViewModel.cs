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
    public string[] Navigation { get; } = ["홈", "캐릭터", "표정", "의상 / 부착물", "동작", "방송 / 설정", "캐릭터 편집기"];
    private readonly bool persistent;
    private bool closing, busy;
    private int page;
    private string search = "", status = "";
    private CharacterCard? selected;
    public ObservableCollection<CharacterCard> Cards { get; } = [];
    public IEnumerable<CharacterCard> Filtered => Cards.Where(c => c.Name.Contains(Search, StringComparison.CurrentCultureIgnoreCase) || c.Id.Contains(Search, StringComparison.OrdinalIgnoreCase));
    public string Search { get => search; set { if (Set(ref search, value)) Notify(nameof(Filtered)); } }
    public int Page { get => page; set { if (Set(ref page, value)) { Notify(nameof(NavigationIndex)); Notify(nameof(IsExpressions)); Notify(nameof(IsHome)); Notify(nameof(IsLibrary)); Notify(nameof(IsEditor)); Notify(nameof(IsAttachments)); Notify(nameof(IsActions)); Notify(nameof(IsSettings)); if (IsEditor) Editor.StartPreview(); else Editor.PausePreview(); if (value == 4) SettingsRequested?.Invoke(Main.SettingsTab); } } }
    private static readonly int[] navigationPages = [0, 1, 6, 2, 3, 4, 5];
    public int NavigationIndex { get => Array.IndexOf(navigationPages, Page); set { if (value >= 0 && value < navigationPages.Length) Page = navigationPages[value]; } }
    public bool IsExpressions => page == 6;
    public bool IsHome => page == 0;
    public bool IsLegacy => !Main.Packages.IsActive;
    public bool IsSheet => !Main.Packages.IsActive && !Main.Runtime.IsActive;
    public bool HasLegacyQuickActions => IsSheet && !Main.Character.HasCompanionSamples;
    public bool IsImageSet => !Main.Packages.IsActive && Main.Runtime.IsActive;
    public bool IsAttachments => page == 2;
    public bool IsActions => page == 3;
    public bool IsSettings => page == 4;
    public string[] SettingsSections => Main.Packages.IsActive ? ["마이크", "음성 반응", "방송 / 캐릭터"] : ["마이크", "음성 반응", "방송 / 캐릭터", "표정 슬롯", "눈 깜빡임 / 눈물", "의상 배치", "손동작 / 입력"];
    public string CurrentCharacter => Main.Character.Name;
    public string PackageStatus => Main.Packages.Status;
    public CharacterRuntimeViewModel? DisplayScene => Main.Packages.Scene ?? (IsSheet ? Main.Character.CompanionScene : null);
    public IReadOnlyList<VoxPet.Core.Attachments.AttachmentDefinition> LiveAttachments => DisplayScene?.Runtime.Definition.Attachments ?? [];
    public IReadOnlyList<VoxPet.Core.Actions.ActionDefinition> LiveActions => DisplayScene?.Runtime.Definition.Actions ?? [];
    public IReadOnlyList<VoxPet.Core.Character.ExpressionDefinition> LiveExpressions => Main.Packages.Scene?.Runtime.Definition.Expressions ?? [];
    public bool IsLibrary => page == 1;
    public bool IsEditor => page == 5;
    public CharacterCard? Selected { get => selected; set { if (Set(ref selected, value)) RefreshCommands(); } }
    public string Status { get => status; private set => Set(ref status, value); }
    private string LegacyActionName => Main.Character.CompanionScene.Runtime.State.ActionId is { } id && LiveActions.FirstOrDefault(a => a.Id == id) is { } action ? action.Name : Main.Character.Motion.Gesture is { } gesture ? Main.Hands.Gestures[(int)gesture] : "대기";
    public string RuntimeState => Main.Packages.Scene is { } s ? $"표정 {s.Runtime.State.ExpressionId} · 입 {s.Runtime.MouthFrame + 1} · 동작 {s.Runtime.State.ActionId ?? "idle"} · 우선순위 {s.Runtime.Actions.Current.Priority}" : $"표정 {Main.Character.ExpressionKind} · 입 {Main.Character.MouthFrameCount}단계 · 동작 {LegacyActionName}";
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand ImportCommand { get; }
    public AsyncCommand NewCommand { get; }
    public AsyncCommand SelectCommand { get; }
    public AsyncCommand EditCommand { get; }
    public AsyncCommand ExportCommand { get; }
    public AsyncCommand DeleteCommand { get; }
    public RelayCommand QuickPlayCommand { get; }
    public AsyncCommand SampleCommand { get; }
    public AsyncCommand EditCompanionCommand { get; }
    public RelayCommand StopActionsCommand { get; }
    public RelayCommand RemoveAttachmentsCommand { get; }
    public ObservableCollection<StudioAttachment> AttachmentCards { get; } = [];
    public IEnumerable<StudioAttachment> HairCards => AttachmentCards.Where(c => c.Id.StartsWith("hair-", StringComparison.Ordinal));
    public IEnumerable<StudioAttachment> ClothingCards => AttachmentCards.Where(c => !c.Id.StartsWith("hair-", StringComparison.Ordinal));
    public KeyValuePair<string, string>[] ClothingCategories { get; } = [new("all", "전체"), new("top", "상의"), new("bottom", "하의"), new("onePiece", "한벌옷"), new("outerwear", "겉옷"), new("shoes", "신발"), new("hat", "모자"), new("glasses", "안경"), new("faceDecoration", "얼굴 장식"), new("earrings", "귀걸이"), new("headAccessory", "머리 장식"), new("handAccessory", "손 장식"), new("cape", "망토 / 날개"), new("backAccessory", "등 장식"), new("effect", "이펙트")];
    private string clothingCategory = "all";
    public string ClothingCategory { get => clothingCategory; set { if (value != null && ClothingCategories.Any(c => c.Key == value) && Set(ref clothingCategory, value)) { Notify(nameof(FilteredClothingCards)); Notify(nameof(ShowHair)); Notify(nameof(ClothingCount)); } } }
    public IEnumerable<StudioAttachment> FilteredClothingCards => ClothingCards.Where(c => ClothingCategory == "all" || c.Category == ClothingCategory);
    public bool ShowHair => HairCards.Any() && ClothingCategory is "all" or "headAccessory";
    public string ClothingCount => $"{FilteredClothingCards.Count() + (ShowHair ? HairCards.Count() : 0)}개 / 전체 {AttachmentCards.Count}개";
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
        string id = Main.SelectedCharacter.Id;
        if ((Main.Packages.IsActive || Main.Runtime.IsActive) && !await Main.SelectCharacterAsync(id, true))
        { Status = Main.CharacterStatus; return; }
        if (closing || Main.SelectedCharacter.Id != id) return;
        if (!Main.Character.HasCompanionSamples)
        {
            var prepared = await CompanionSampleLibrary.LoadAsync(id);
            if (closing || Main.SelectedCharacter.Id != id || Main.Packages.IsActive || Main.Runtime.IsActive) return;
            Main.Character.SetCompanionSamples(prepared, id);
        }
        if (closing) return;
        Status = Main.Character.HasCompanionSamples ? Main.SelectedCharacter.Name + " · 의상17종과 동작9종이 준비되어 있습니다." : "의상 파일을 읽지 못했습니다. Assets/Samples 폴더를 확인하세요.";
        Page = destination; UpdateAttachmentCards();
    }
    public void PlayAction(string id)
    { if (Main.Packages.IsActive) Main.Packages.Play(id); else if (IsSheet) { Main.Equipment.SelectForAction(id); Main.Character.PlayCompanionAction(id); } }
    private void UpdateAttachmentCards()
    {
        AttachmentCards.Clear();
        Notify(nameof(HairCards)); Notify(nameof(ClothingCards)); Notify(nameof(FilteredClothingCards)); Notify(nameof(ShowHair)); Notify(nameof(ClothingCount));
        if (DisplayScene is not { } scene) return;
        foreach (var item in scene.Runtime.Definition.Attachments)
        {
            var layer = item.Layers.FirstOrDefault();
            var thumbnail = scene.Images.GetValueOrDefault("thumbnails/" + item.Id + ".png") ?? (layer != null ? scene.Images.GetValueOrDefault(layer.Asset) : null);
            AttachmentCards.Add(new StudioAttachment(item.Id, item.Name, item.Category, thumbnail,
                () => scene.Runtime.Attachments.EnabledIds.Contains(item.Id), () =>
                {
                    if (closing || !ReferenceEquals(scene, DisplayScene) || Main.Packages.IsBusy) return;
                    scene.Runtime.Attachments.SetEnabled(item.Id, !scene.Runtime.Attachments.EnabledIds.Contains(item.Id));
                    if (!Main.Packages.IsActive) Main.Character.RefreshMotion();
                    foreach (var card in AttachmentCards) card.Refresh();
                }));
        }
        Notify(nameof(HairCards)); Notify(nameof(ClothingCards)); Notify(nameof(FilteredClothingCards)); Notify(nameof(ShowHair)); Notify(nameof(ClothingCount));
    }
    private void RefreshSamples()
    { Notify(nameof(HasLegacyQuickActions)); Notify(nameof(LiveAttachments)); Notify(nameof(LiveActions)); Notify(nameof(DisplayScene)); UpdateAttachmentCards(); EditCompanionCommand.Refresh(); }

    public event Func<string?>? ChoosePackage;
    public event Func<string?>? ChooseExport;
    public event Action<int>? SettingsRequested;
    public StudioShellViewModel(MainViewModel main, bool persistent)
    {
        Main = main; this.persistent = persistent;
        SampleCommand = new(UseSamplesAsync, () => !busy && !closing && !Main.Packages.IsBusy);
        EditCompanionCommand = new(async () =>
        {
            try
            {
                string id = Main.Character.CompanionCharacterId;
                if (IsSheet && await Main.Character.EditableCompanionAsync() is { } package && !closing && IsSheet && Main.Character.CompanionCharacterId == id && await Editor.OpenPackageAsync(package)) Page = 5;
            }
            catch (Exception error) when (CharacterPackageLibrary.Expected(error) || error is InvalidOperationException)
            { if (!closing) Status = "편집 자료를 준비하지 못했습니다. 현재 캐릭터와 원본은 유지합니다."; }
        }, () => !busy && !closing && IsSheet && Main.Character.HasCompanionSamples);
        StopActionsCommand = new(() => { Main.Packages.Scene?.Stop(); Main.Character.StopCompanionActions(); Main.Hands.StopCommand.Execute(null); }, () => !closing);
        RemoveAttachmentsCommand = new(() => { if (DisplayScene is { } scene) foreach (string id in scene.Runtime.Attachments.EnabledIds.ToArray()) scene.Runtime.Attachments.SetEnabled(id, false); foreach (var card in AttachmentCards) card.Refresh(); if (!Main.Packages.IsActive) Main.Wardrobe.RemoveAllCommand.Execute(null); Main.Character.RefreshMotion(); }, () => !closing);
        RefreshCommand = new(RefreshAsync, () => !busy && !closing);
        ImportCommand = new(async () => { if (ChoosePackage?.Invoke() is { } path) { await Main.Packages.ImportAsync(path); Status = Main.Packages.Status; await RefreshAsync(); } }, () => !busy && !closing);
        NewCommand = new(async () => { await Editor.NewAsync(); Page = 5; }, () => !busy && !closing);
        SelectCommand = new(async () => { if (Selected == null) return; if (Selected.Legacy is { } legacy) await Main.SelectCharacterAsync(legacy.Id); else await Main.Packages.SelectAsync(Selected.LibraryId); Status = Main.Packages.IsActive ? Main.Packages.Status : Main.CharacterStatus; }, () => Selected != null && !busy && !closing);
        EditCommand = new(async () =>
        {
            if (Selected == null) return;
            if (Selected.Legacy != null) { Status = "기존 PNG 시트와 설정은 방송 / 설정에서 관리하세요. 새 분리 캐릭터는 새 캐릭터로 시작할 수 있습니다."; return; }
            var loaded = await ReadCardAsync(Selected); if (loaded != null && await Editor.OpenPackageAsync(loaded.Package)) Page = 5;
        }, () => Selected?.Legacy == null && Selected != null && !busy && !closing);
        ExportCommand = new(async () => { if (Selected == null || ChooseExport?.Invoke() is not { } path) return; var loaded = await ReadCardAsync(Selected); if (loaded != null) try { await Task.Run(() => new VoxPetPackageExporter().Export(loaded.Package, path)); Status = "캐릭터 내보내기 완료"; } catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { Status = "내보내기 실패 · " + e.Message; } }, () => Selected?.Legacy == null && Selected != null && !busy && !closing);
        DeleteCommand = new(async () => { if (Selected != null) { await Main.Packages.DeleteAsync(Selected.LibraryId); Status = Main.Packages.Status; await RefreshAsync(); } }, () => Selected?.Legacy == null && Selected != null && !Selected.Sample && !busy && !closing);
        QuickPlayCommand = new(() => { if (DisplayScene is { } scene && scene.Runtime.Definition.Actions.FirstOrDefault() is { } action) PlayAction(action.Id); else Main.Hands.PreviewCommand.Execute(null); }, () => !closing);
        Main.Packages.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.Packages.Scene)) { Notify(nameof(LiveAttachments)); Notify(nameof(LiveActions)); Notify(nameof(LiveExpressions)); Notify(nameof(RuntimeState)); Notify(nameof(IsLegacy)); Notify(nameof(IsSheet)); Notify(nameof(IsImageSet)); Notify(nameof(SettingsSections)); RefreshSamples(); if (Main.Packages.IsActive && Main.SettingsTab > 2) Main.SettingsTab = 0; } if (e.PropertyName == nameof(Main.Packages.Status)) Notify(nameof(PackageStatus)); SampleCommand.Refresh(); };
        Main.Character.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(Main.Character.RuntimeScene) or nameof(Main.Character.Motion)) Notify(nameof(RuntimeState)); if (e.PropertyName == nameof(Main.Character.Name)) Notify(nameof(CurrentCharacter)); if (e.PropertyName is nameof(Main.Character.HasCompanionSamples) or nameof(Main.Character.CompanionCharacterId) or nameof(Main.Character.CompanionScene)) RefreshSamples(); };
        Main.Runtime.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.Runtime.IsActive)) { Notify(nameof(IsSheet)); Notify(nameof(IsImageSet)); RefreshSamples(); } };
        Main.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.VoiceLevel)) Notify(nameof(RuntimeState)); };
    }
    private void RefreshCommands() { foreach (var c in new[] { RefreshCommand, ImportCommand, NewCommand, SelectCommand, EditCommand, ExportCommand, DeleteCommand, SampleCommand, EditCompanionCommand }) c.Refresh(); }
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
                cards.Add(new("legacy-" + legacy.Id, legacy.Name, legacy.Id, "v1", "", "의상17종 · 동작9종 포함 · 기존 PNG와 표정 유지", image, legacy));
            }
            foreach (var entry in Main.Packages.Entries.Where(e => e.CharacterId != SampleId))
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
