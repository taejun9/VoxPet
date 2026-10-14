using System.Windows;
using System.Windows.Controls;
using VoxPet.App.ViewModels;

namespace VoxPet.App.Views;

public partial class CharacterEditorView : UserControl
{
    private CharacterEditorViewModel? attached;
    public CharacterEditorView()
    {
        InitializeComponent(); DataContextChanged += (_, _) => Attach(); Loaded += (_, _) => Attach(); Unloaded += (_, _) => attached?.PausePreview(); IsVisibleChanged += (_, _) => { if (IsVisible) attached?.StartPreview(); else attached?.PausePreview(); };
    }
    private void Attach()
    {
        if (DataContext is not CharacterEditorViewModel model) return;
        if (!ReferenceEquals(attached, model))
        {
            attached?.PausePreview(); attached = model;
            model.ChoosePackage += () => Pick("VoxPet 캐릭터 불러오기", "VoxPet (*.voxpet)|*.voxpet");
            model.ChoosePng += () => Pick("PNG 이미지 선택", "PNG (*.png)|*.png");
            model.ChooseExport += () =>
            {
                var dialog = new Microsoft.Win32.SaveFileDialog { Title = "캐릭터 내보내기", Filter = "VoxPet (*.voxpet)|*.voxpet", DefaultExt = ".voxpet", AddExtension = true, FileName = model.Document?.Package.Manifest.Name ?? "character" };
                return dialog.ShowDialog(Window.GetWindow(this)) == true ? dialog.FileName : null;
            };
        }
        if (IsVisible) model.StartPreview();
    }
    private string? Pick(string title, string filter)
    { var dialog = new Microsoft.Win32.OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true }; return dialog.ShowDialog(Window.GetWindow(this)) == true ? dialog.FileName : null; }
    private async void PickAsset(object sender, RoutedEventArgs e)
    {
        if (attached == null || sender is not FrameworkElement { DataContext: EditorField field }) return;
        if (Pick("PNG 이미지 교체", "PNG (*.png)|*.png") is { } path) await attached.ReplaceImageAsync(path, field.Key, attached.DetailFields.Contains(field));
    }
    private async void AddDetail(object sender, RoutedEventArgs e) { if (attached != null) { attached.AddTriggerNext = false; await attached.AddDetailAsync(); } }
    private async void AddTrigger(object sender, RoutedEventArgs e) { if (attached != null) { attached.AddTriggerNext = true; await attached.AddDetailAsync(); } }
}
