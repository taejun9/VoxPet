namespace VoxPet.Core.Models;

public sealed record CompanionWeapon(string Id, string Name, string Asset)
{
    public static IReadOnlyList<CompanionWeapon> All { get; } = Array.AsReadOnly(new[]
    {
        new CompanionWeapon("toy-gun", "장난감 총", "samples/toy-gun.png"),
        new CompanionWeapon("toy-sword", "장난감 칼", "samples/toy-sword.png"),
        new CompanionWeapon("magic-wand", "마법봉", "samples/magic-wand.png"),
        new CompanionWeapon("firearm", "권총", "samples/toy-gun.png"),
        new CompanionWeapon("bow", "활", "samples/bow.png"),
        new CompanionWeapon("one-hand", "한손 무기", "samples/toy-sword.png"),
        new CompanionWeapon("two-hand", "양손 무기", "samples/toy-sword.png")
    });
    public static CompanionWeapon? Find(string? id) => All.FirstOrDefault(w => w.Id == id);
}
public sealed record CompanionEquipment(string? Weapon = null, bool Hold = true)
{
    public void Validate()
    { if (Weapon != null && CompanionWeapon.Find(Weapon) == null) throw new ArgumentException("지원하지 않는 무기입니다."); }
}
