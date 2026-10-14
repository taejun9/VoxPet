using System.Globalization;

namespace VoxPet.App.ViewModels;

public sealed class EditorField : ObservableObject
{
    public string Key { get; }
    public string Label { get; }
    public IReadOnlyList<string> Choices { get; }
    public bool HasChoices => Choices.Count > 0;
    public bool IsAsset { get; }
    private string value;
    public string Value { get => value; set => Set(ref this.value, value); }
    public EditorField(string key, string label, object? value, IEnumerable<string>? choices = null, bool asset = false)
    {
        Key = key; Label = label; this.value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        Choices = (choices ?? []).ToArray(); IsAsset = asset;
    }
}
public sealed record EditorRow(string Id, string Name, object Item)
{
    public override string ToString() => Name;
}
