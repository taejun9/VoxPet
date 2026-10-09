using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

/// <summary>메뉴 검정 글씨/순서와 저장 경계/12종 테스트 수명을 실제 WPF에서 합성 자료로 확인한다.</summary>
internal static class ExpressionManagementQa
{
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    public static async Task RunAsync(MainWindow main, Action<bool, string> check)
    {
        var broadcast = main.ShowCharacter();
        var target = (FrameworkElement)broadcast.Content; var menu = target.ContextMenu;
        menu.PlacementTarget = target; menu.IsOpen = true; await Task.Delay(80); menu.UpdateLayout();
        var texts = Descendants(menu).OfType<TextBlock>().Where(text => text.IsVisible && text.Text.Length > 0).ToArray();
        check(texts.Length >= 4 && texts.All(text => text.Foreground is SolidColorBrush brush && brush.Color == Colors.Black), "broadcast_context_menu_black_text");
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(menu.ActualWidth), (int)Math.Ceiling(menu.ActualHeight), 96, 96, PixelFormats.Pbgra32); bitmap.Render(menu);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), "voxpet-expression-menu.png"))) encoder.Save(file);
        menu.IsOpen = false;
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-management-qa-" + Guid.NewGuid());
        var character = new CharacterViewModel(); var editor = new ExpressionViewModel(character, true, folder);
        ExpressionViewModel? restart = null;
        try
        {
            await editor.InitializeAsync(); editor.Selected = editor.Slots[2];
            await editor.SaveAsync(); string slotPath = Path.Combine(folder, "slot-3.json");
            var savedBytes = File.ReadAllBytes(slotPath); var draft = editor.Selected;
            draft.Name = "저장하지 않은 초안"; var unsaved = draft.Draft;
            await editor.MoveAsync(-1);
            check(ReferenceEquals(editor.Selected, draft) && draft.Position == 1 && draft.Draft == unsaved && File.ReadAllBytes(slotPath).SequenceEqual(savedBytes), "order_move_preserves_draft_and_slot_file");
            check(editor.Slots[1].Index == 2 && editor.Slots[1].Label.StartsWith("F2") && editor.TestExpressions[1].Index == 2, "order_label_and_test_order_follow_position");
            await editor.ActivateAsync(1);
            check(character.Name.Contains("슬픔"), "order_position_activates_moved_expression");
            restart = new(character, true, folder); await restart.InitializeAsync();
            check(restart.Slots[1].Index == 2 && restart.Slots[1].Saved.Kind == ExpressionKind.Sad, "order_survives_restart");
            restart.Selected = restart.Slots[0];
            check(!restart.MoveUpCommand.CanExecute(null), "order_first_boundary"); restart.Selected = restart.Slots[11];
            check(!restart.MoveDownCommand.CanExecute(null), "order_last_boundary");
            string memoryPath = Path.Combine(folder, "memory.png"); DetailedCharacterQa.SaveFixture(memoryPath, 1);
            var memoryCharacter = new CharacterViewModel(); var memory = new ExpressionViewModel(memoryCharacter, false);
            try
            {
                check(await memory.ImportAsync(memoryPath), "order_memory_sheet_import"); await memory.SaveAsync(); await memory.MoveAsync(1);
                await memory.PreviewBuiltinAsync(5); await memory.ActivateAsync(1);
                check(memoryCharacter.MouthFrameCount == 8 && memory.Slots[1].Index == 0, "order_memory_sheet_uses_storage_identity");
            }
            finally { await memory.CloseAsync(); }
            string before = character.Name;
            await editor.TestAllAsync(TimeSpan.FromMilliseconds(10));
            check(editor.LastTestedKinds.Count == 12 && editor.LastTestedKinds.Distinct().Count() == 12 && editor.LastTestedKinds[1] == ExpressionKind.Sad && character.Name == before && !editor.IsTesting, "twelve_test_all_ordered_and_restores");
            var snapshots = editor.Slots.Select(slot => slot.Saved).ToArray();
            foreach (int i in Enumerable.Range(0, 12)) await editor.PreviewBuiltinAsync(i);
            check(editor.Slots.Select(slot => slot.Saved).SequenceEqual(snapshots), "twelve_preview_preserves_saved_slots");
            before = character.Name;
            var running = editor.TestAllAsync(TimeSpan.FromSeconds(1)); await Task.Delay(20); editor.CancelTest(); await running;
            check(!editor.IsTesting && character.Name == before && editor.LastTestedKinds.Count < 12, "twelve_test_cancel_restores");
            running = editor.TestAllAsync(TimeSpan.FromSeconds(1)); await Task.Delay(20); await editor.ActivateAsync(0); await running;
            check(character.Name.Contains(editor.Slots[0].Saved.Name), "twelve_test_key_interrupt_wins");
            running = editor.TestAllAsync(TimeSpan.FromSeconds(1)); await Task.Delay(20); await editor.CloseAsync(); await running;
            check(!editor.IsTesting && !editor.CanEdit, "twelve_test_close_cancels");
        }
        finally
        {
            await editor.CloseAsync(); if (restart != null) await restart.CloseAsync();
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        int microphoneCalls = 0;
        var model = new MainViewModel(false, () => [], _ => { microphoneCalls++; throw new InvalidOperationException("QA must not open a microphone"); });
        await model.InitializeAsync();
        await model.TestExpressionsAsync(TimeSpan.FromMilliseconds(10));
        check(microphoneCalls == 0 && model.VoiceLevel == 0 && model.CanChooseDevice && model.Expressions.LastTestedKinds.Count == 12, "twelve_test_owns_only_synthetic_demo");
        await model.DisposeAsync();
    }
}
