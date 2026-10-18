using System.Collections.ObjectModel;
using System.IO;
using VoxPet.App.Services;
using VoxPet.Core.Character;
using VoxPet.Core.Triggers;

namespace VoxPet.App.ViewModels;

/// <summary>native 보관함/선택 수명. 실패/오래된 worker 결과는 현재 scene을 덮지 않는다.</summary>
public sealed class NativeCharacterViewModel : ObservableObject
{
    private readonly CharacterViewModel character;
    private readonly CharacterPackageLibrary library;
    private readonly bool persistent;
    private readonly Func<double> seconds;
    private readonly SemaphoreSlim io = new(1, 1);
    private CharacterRuntimeViewModel? scene;
    private bool busy, closing, mouseEnabled, faulted;
    private long revision;
    private string status = ".voxpet 캐릭터를 등록할 수 있습니다.";
    public LoadedCharacterPackage? Loaded { get; private set; }
    public ObservableCollection<CharacterPackageEntry> Entries { get; } = [];
    public int UnavailableEntries { get; private set; }
    public bool IsActive => scene != null;
    public bool IsBusy => busy;
    public bool CanReceiveInput => IsActive && !busy && !closing && !faulted;
    public bool MouseEnabled { get => mouseEnabled; set { if (Set(ref mouseEnabled, value)) InputsChanged?.Invoke(); } }
    public long SceneRevision => revision;
    public string Status { get => status; private set => Set(ref status, value); }
    public CharacterRuntimeViewModel? Scene => scene;
    public event Action? InputsChanged;
    public NativeCharacterViewModel(CharacterViewModel character, bool persistent, string folder, Func<double> seconds)
    { this.character = character; this.persistent = persistent; this.seconds = seconds; library = new(Path.Combine(folder, "CharactersV2")); }
    public async Task InitializeAsync()
    {
        if (!persistent || closing) return;
        await RefreshAsync(); string? id = await Task.Run(library.Selected); if (id != null && !closing) await SelectAsync(id);
    }
    public async Task RefreshAsync()
    {
        if (!persistent || closing) return;
        await io.WaitAsync();
        try { var entries = await Task.Run(library.List); if (!closing) { UnavailableEntries = library.SkippedCount; Entries.Clear(); foreach (var entry in entries) Entries.Add(entry); } }
        catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { if (!closing) Status = "캐릭터 보관함 읽기 실패 · 기존 파일을 유지합니다. " + e.Message; }
        finally { io.Release(); }
    }
    public async Task<bool> ImportAsync(string path)
        => await ReadAsync(() => persistent ? library.Import(path) : CharacterPackageLibrary.Decode(new VoxPet.Core.Packages.VoxPetPackageLoader().Load(path)));
    public async Task<bool> SelectAsync(string id) => await ReadAsync(() => library.Load(id));
    private async Task<bool> ReadAsync(Func<LoadedCharacterPackage> load)
    {
        if (closing || busy) return false;
        long request = ++revision; busy = true; Notify(nameof(IsBusy)); InputsChanged?.Invoke(); await io.WaitAsync();
        try
        {
            double startedAt = seconds();
            var result = await Task.Run(() =>
            {
                var prepared = load();
                var next = new CharacterRuntimeViewModel(prepared.Package.CreateRuntime(startedAt: startedAt), prepared.Images, startedAt) { UseViewportTransform = true };
                // 첫 frame 계산도 transaction 안에서 끝낸다. 실패하면 기존 scene/선택을 유지한다.
                next.Update(startedAt, 0);
                return (Prepared: prepared, Scene: next);
            });
            var prepared = result.Prepared;
            if (closing || request != revision) { result.Scene.ReleaseFacePreparation(); return false; }
            if (persistent) await Task.Run(() => library.Select(prepared.LibraryId));
            if (closing || request != revision) { result.Scene.ReleaseFacePreparation(); return false; }
            scene?.Stop(); scene?.ReleaseFacePreparation(); scene = result.Scene; Loaded = prepared; faulted = false;
            character.SetRuntimeLayers([], prepared.Package.Manifest.Name); character.SetRuntimeScene(scene);
            Status = prepared.Package.Manifest.Name + " · .voxpet 캐릭터 적용 완료";
            Notify(nameof(IsActive)); Notify(nameof(Loaded)); Notify(nameof(Scene)); InputsChanged?.Invoke(); return true;
        }
        catch (Exception e) when (CharacterPackageLibrary.Expected(e) || e is InvalidOperationException) { if (!closing) Status = "캐릭터 적용 실패 · 기존 출력과 원본을 유지합니다. " + e.Message; return false; }
        finally { io.Release(); busy = false; Notify(nameof(IsBusy)); InputsChanged?.Invoke(); }
    }
    public async Task<LoadedCharacterPackage?> LoadForEditorAsync(string id)
    {
        try { return await LoadPreparedAsync(id, loaded => loaded); }
        catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { Status = "보관함 읽기 실패 · " + e.Message; return null; }
    }
    // DrawingGroup.DrawImage may re-freeze a frozen frame and revisit its thread-owned
    // decoder. Decode and prepare the frozen card in one worker invocation.
    internal async Task<T?> LoadPreparedAsync<T>(string id, Func<LoadedCharacterPackage, T> prepare) where T : class
    {
        if (closing) return null; await io.WaitAsync();
        try { return await Task.Run(() => prepare(library.Load(id))); }
        finally { io.Release(); }
    }
    public async Task<bool> DeleteAsync(string id)
    {
        if (closing || busy) return false;
        if (Loaded?.LibraryId == id) await DeactivateAsync(); await io.WaitAsync();
        try { await Task.Run(() => library.Delete(id)); Status = "관리 복사본을 삭제했습니다. 사용자의 원본 파일은 유지합니다."; return true; }
        catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { Status = "삭제 실패 · " + e.Message; return false; }
        finally { io.Release(); await RefreshAsync(); }
    }
    public string InputStatus { get; private set; } = "";
    public void ReportInputStatus(string message) { InputStatus = message; Notify(nameof(InputStatus)); }
    public void Update(double now, double voice)
    {
        if (scene == null || closing || faulted) return;
        try { scene.Update(now, voice); character.SetRuntimeScene(scene); }
        catch (Exception e) when (CharacterPackageLibrary.Expected(e) || e is InvalidOperationException)
        {
            // 사용자 패키지의 지연 이벤트/변환 오류는 마지막 frame에서 멈추고 입력을 해제한다.
            faulted = true; scene.Stop();
            Status = "캐릭터 재생 중단 · 마지막 화면을 유지합니다. 캐릭터를 수정하거나 다시 선택하세요.";
            InputsChanged?.Invoke();
        }
    }
    public void Input(TriggerEvent input) { if (CanReceiveInput) scene!.Input(input); }
    public void Play(string id) { if (CanReceiveInput) scene!.Runtime.RequestAction(id); }
    public void FollowLegacyExpression(VoxPet.Core.Models.ExpressionKind kind)
    {
        if (!CanReceiveInput) return;
        string id = kind.ToString().ToLowerInvariant();
        var expressions = scene!.Runtime.Definition.Expressions;
        string? target = expressions.FirstOrDefault(e => e.Id == id)?.Id ?? expressions.FirstOrDefault(e => e.Id == "neutral")?.Id ?? expressions.FirstOrDefault()?.Id;
        if (target != null) scene.Runtime.SetExpression(target);
    }
    public void Expression(string id) { if (CanReceiveInput) scene!.Runtime.SetExpression(id); }
    public async Task DeactivateAsync()
    {
        revision++; scene?.Stop(); scene?.ReleaseFacePreparation(); scene = null; Loaded = null; character.SetRuntimeScene(null); character.SetRuntimeLayers(null);
        Notify(nameof(IsActive)); Notify(nameof(Loaded)); Notify(nameof(Scene)); InputsChanged?.Invoke();
        if (persistent && !closing) { await io.WaitAsync(); try { await Task.Run(() => library.Select(null)); } catch (Exception e) when (CharacterPackageLibrary.Expected(e)) { Status = "선택 설정 저장 실패 · " + e.Message; } finally { io.Release(); } }
    }
    public async Task CloseAsync()
    {
        closing = true; revision++; scene?.Stop(); scene?.ReleaseFacePreparation(); InputsChanged?.Invoke(); await io.WaitAsync(); io.Release();
    }
}
