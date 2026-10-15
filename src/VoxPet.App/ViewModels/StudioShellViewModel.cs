using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using VoxPet.App.Services;
using VoxPet.App.Views;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using CharacterCatalogEntry = VoxPet.Core.Models.CharacterDefinition;

namespace VoxPet.App.ViewModels;

public sealed record CharacterCard(string LibraryId, string Name, string Id, string Version, string Author, string Description, ImageSource? Image, CharacterCatalogEntry? Legacy = null);
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
    public int Page { get => page; set { if (Set(ref page, value)) { Notify(nameof(IsHome)); Notify(nameof(IsLibrary)); Notify(nameof(IsEditor)); Notify(nameof(IsAttachments)); Notify(nameof(IsActions)); if (IsEditor) Editor.StartPreview(); else Editor.PausePreview(); if (value == 4) SettingsRequested?.Invoke(0); } } }
    public bool IsHome => page == 0;
    public bool IsLegacy => !Main.Packages.IsActive;
    public bool IsAttachments => page == 2;
    public bool IsActions => page == 3;
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
    public event Func<string?>? ChoosePackage;
    public event Func<string?>? ChooseExport;
    public event Action<int>? SettingsRequested;
    public StudioShellViewModel(MainViewModel main, bool persistent)
    {
        Main = main; this.persistent = persistent;
        RefreshCommand = new(RefreshAsync, () => !busy && !closing);
        ImportCommand = new(async () => { if (ChoosePackage?.Invoke() is { } path) { await Main.Packages.ImportAsync(path); Status = Main.Packages.Status; await RefreshAsync(); } }, () => !busy && !closing);
        NewCommand = new(async () => { await Editor.NewAsync(); Page = 5; }, () => !busy && !closing);
        SelectCommand = new(async () => { if (Selected == null) return; if (Selected.Legacy is { } legacy) await Main.SelectCharacterAsync(legacy.Id); else await Main.Packages.SelectAsync(Selected.LibraryId); Status = Main.Packages.IsActive ? Main.Packages.Status : Main.CharacterStatus; }, () => Selected != null && !busy && !closing);
        EditCommand = new(async () =>
        {
            if (Selected == null) return;
            if (Selected.Legacy != null) { Status = "기존 PNG 시트와 설정은 방송 / 설정에서 관리하세요. 새 분리 캐릭터는 새 캐릭터로 시작할 수 있습니다."; return; }
            var loaded = await Main.Packages.LoadForEditorAsync(Selected.LibraryId); if (loaded != null && await Editor.OpenPackageAsync(loaded.Package)) Page = 5;
        }, () => Selected?.Legacy == null && Selected != null && !busy && !closing);
        ExportCommand = new(async () => { if (Selected == null || ChooseExport?.Invoke() is not { } path) return; var loaded = await Main.Packages.LoadForEditorAsync(Selected.LibraryId); if (loaded != null) try { await Task.Run(() => new VoxPetPackageExporter().Export(loaded.Package, path)); Status = "캐릭터 내보내기 완료"; } catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { Status = "내보내기 실패 · " + e.Message; } }, () => Selected?.Legacy == null && Selected != null && !busy && !closing);
        DeleteCommand = new(async () => { if (Selected != null) { await Main.Packages.DeleteAsync(Selected.LibraryId); Status = Main.Packages.Status; await RefreshAsync(); } }, () => Selected?.Legacy == null && Selected != null && !busy && !closing);
        QuickPlayCommand = new(() => { if (Main.Packages.Scene is { } scene && scene.Runtime.Definition.Actions.FirstOrDefault() is { } action) Main.Packages.Play(action.Id); else Main.Hands.PreviewCommand.Execute(null); }, () => !closing);
        Main.Packages.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.Packages.Scene)) { Notify(nameof(LiveAttachments)); Notify(nameof(LiveActions)); Notify(nameof(LiveExpressions)); Notify(nameof(RuntimeState)); Notify(nameof(IsLegacy)); } };
        Main.Character.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.Character.RuntimeScene)) Notify(nameof(RuntimeState)); };
        Main.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Main.VoiceLevel)) Notify(nameof(RuntimeState)); };
    }
    private void RefreshCommands() { foreach (var c in new[] { RefreshCommand, ImportCommand, NewCommand, SelectCommand, EditCommand, ExportCommand, DeleteCommand }) c.Refresh(); }
    public async Task RefreshAsync()
    {
        if (closing || busy) return; busy = true; RefreshCommands(); string? id = Selected?.LibraryId;
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
            foreach (var entry in Main.Packages.Entries)
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
