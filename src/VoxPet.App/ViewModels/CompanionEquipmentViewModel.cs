using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.ViewModels;

/// <summary>무기 선택과 상시 들기를 캐릭터별로 기억한다. 저장은 worker에서 직렬화한다.</summary>
public sealed class CompanionEquipmentViewModel : ObservableObject
{
    private readonly CharacterViewModel character;
    private readonly CompanionEquipmentStore store;
    private readonly bool persistent;
    private readonly DispatcherTimer timer;
    private readonly SemaphoreSlim gate = new(1, 1);
    private Dictionary<string, CompanionEquipment> choices = [];
    private bool closing, ready, dirty;
    private long revision;
    private string status = "상시 들기를 끄면 무기는 무기 동작을 재생하는 동안만 표시합니다.";
    public string[] WeaponNames { get; } = new[] { "무기 없음" }.Concat(CompanionWeapon.All.Select(w => w.Name)).ToArray();
    public bool CanUse => ready && !closing && character.HasCompanionSamples;
    private CompanionEquipment Current => choices.GetValueOrDefault(character.CompanionCharacterId) ?? new();
    public int WeaponIndex
    {
        get => Current.Weapon is { } id ? CompanionWeapon.All.ToList().FindIndex(w => w.Id == id) + 1 : 0;
        set { if (!CanUse || value < 0 || value >= WeaponNames.Length || value == WeaponIndex) return; Change(Current with { Weapon = value == 0 ? null : CompanionWeapon.All[value - 1].Id }); }
    }
    public bool HoldWeapon { get => Current.Hold; set { if (CanUse && value != Current.Hold) Change(Current with { Hold = value }); } }
    public ImageSource? WeaponPreview => CompanionWeapon.Find(Current.Weapon) is { } weapon ? character.CompanionScene.Images.GetValueOrDefault(weapon.Asset) : null;
    public string Status { get => status; private set => Set(ref status, value); }
    public CompanionEquipmentViewModel(CharacterViewModel character, bool persistent, string folder)
    {
        this.character = character; this.persistent = persistent; store = new(Path.Combine(folder, "companion-equipment.json"));
        timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(350) }; timer.Tick += SaveTick;
        character.PropertyChanged += CharacterChanged;
    }
    private void CharacterChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    { if (e.PropertyName is nameof(character.CompanionCharacterId) or nameof(character.HasCompanionSamples)) Apply(); }
    public async Task InitializeAsync()
    { if (persistent) choices = await Task.Run(store.Load); if (closing) return; ready = true; Apply(); }
    private void Apply()
    {
        if (closing) return;
        character.SetCompanionEquipment(Current.Weapon, Current.Hold);
        Notify(nameof(WeaponIndex)); Notify(nameof(HoldWeapon)); Notify(nameof(WeaponPreview)); Notify(nameof(CanUse));
    }
    private void Change(CompanionEquipment value)
    {
        value.Validate();
        if (Current.Weapon != value.Weapon) character.StopCompanionActions();
        choices[character.CompanionCharacterId] = value; dirty = true; revision++; Apply();
        if (persistent) { timer.Stop(); timer.Start(); }
    }
    public void SelectForAction(string id)
    { if (CanUse && CompanionWeapon.Find(id) != null && Current.Weapon != id) Change(Current with { Weapon = id }); }
    private async void SaveTick(object? sender, EventArgs e)
    { timer.Stop(); await SaveAsync(); }
    private async Task SaveAsync()
    {
        if (!persistent || !dirty) return; await gate.WaitAsync();
        try
        {
            long version = revision; var snapshot = new Dictionary<string, CompanionEquipment>(choices);
            if (await Task.Run(() => store.Save(snapshot))) { if (revision == version) dirty = false; }
            else Status = "무기 설정을 저장하지 못했습니다. 현재 선택은 유지하며 종료할 때 다시 시도합니다.";
        }
        finally { gate.Release(); }
    }
    public async Task CloseAsync()
    { closing = true; timer.Stop(); character.PropertyChanged -= CharacterChanged; await SaveAsync(); }
}
