using System.IO;
using System.Windows;
using System.Windows.Media;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

/// <summary>개인 이미지 대신 임시 합성 시트로 다섯 선택·표정 경합·실패 복구와 실제 Selector를 검사한다.</summary>
internal static class CharacterSelectionQa
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-selection-qa-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        var model = new MainViewModel(false, () => [], _ => throw new InvalidOperationException(), folder);
        var window = new MainWindow(true, new Rect(0, 0, 1000, 730), model);
        try
        {
            foreach (var item in CharacterCatalog.All)
                DetailedCharacterQa.SaveFixture(Path.Combine(folder, item.FileName), CharacterCatalog.All.ToList().IndexOf(item));
            window.Show(); await window.Ready;
            window.SettingsTabs.SelectedIndex = 2; window.UpdateLayout();
            check(window.CharacterSelector.Items.Count == 5, "selection_five_actual_combo_items");
            var profiles = model.Expressions.Slots.Select(slot => slot.Saved).ToArray();
            var wardrobe = model.Wardrobe.Items;
            for (int i = 0; i < model.Characters.Count; i++)
            {
                var item = model.Characters[i];
                check(await model.SelectCharacterAsync(item.Id), "selection_load_" + item.Id);
                check(model.SelectedCharacter == item && model.Character.Name.StartsWith(item.Name) && ReferenceEquals(window.CharacterSelector.SelectedItem, item), "selection_applied_ui_" + item.Id);
                var states = new HashSet<ImageSource>();
                foreach (double eyes in new[] { 0.0, 1.0 })
                    foreach (double level in new[] { 0.0, .05, .15, .30, .45, .65, .82, 1.0 })
                    {
                        model.Character.Update(new(level, 0, 0, 0, eyes, MouthState.Closed)); states.Add(model.Character.Sprite);
                    }
                check(states.Count == 16 && states.All(image => image.IsFrozen), "selection_frozen_mouth_eye_" + item.Id);
                var neutral = model.Character.Sprite;
                await model.Expressions.PreviewBuiltinAsync(2);
                check(ReferenceEquals(neutral, model.Character.Sprite) && model.Character.Name.StartsWith(item.Name), "selection_missing_expression_keeps_" + item.Id);
                check(ReferenceEquals(window.ShowCharacter().DataContext, model), "selection_shared_broadcast_" + item.Id);
            }
            check(model.CharacterStatus.Contains("측면"), "selection_pelly_side_metadata");
            check(model.Expressions.Slots.Select(slot => slot.Saved).SequenceEqual(profiles) && ReferenceEquals(wardrobe, model.Character.CostumeLayers), "selection_preserves_slots_and_costumes");
            model.Character.Update(new(0, 0, 0, 0, 1, MouthState.Closed));
            var previous = model.Character.Sprite;
            File.WriteAllText(Path.Combine(folder, "늘보맨.png"), "broken PNG");
            check(!await model.SelectCharacterAsync("neulbo-man") && model.SelectedCharacter.Id == "pelly" && ReferenceEquals(previous, model.Character.Sprite), "selection_corrupt_rolls_back");
            File.Delete(Path.Combine(folder, "늘보걸.png"));
            check(!await model.SelectCharacterAsync("neulbo-girl") && model.SelectedCharacter.Id == "pelly", "selection_missing_rolls_back");
            check(!await model.SelectCharacterAsync("../private.png") && model.SelectedCharacter.Id == "pelly", "selection_unknown_id_rejected");
            var first = model.SelectCharacterAsync("neulbo"); var last = model.SelectCharacterAsync("drawing-neulbo");
            await Task.WhenAll(first, last);
            check(model.SelectedCharacter.Id == "drawing-neulbo" && model.Character.Name.StartsWith("그림 늘보군"), "selection_latest_request_wins");
            var test = model.Expressions.TestAllAsync(TimeSpan.FromMilliseconds(30));
            var switched = model.SelectCharacterAsync("pelly"); await Task.WhenAll(test, switched);
            check(model.SelectedCharacter.Id == "pelly" && model.Character.Name.StartsWith("펠리") && !model.Expressions.IsTesting, "selection_test_cannot_restore_previous_character");
            window.CharacterSelector.SelectedItem = model.Characters[1];
            for (int i = 0; i < 100 && !model.CanChooseCharacter; i++) await Task.Delay(10);
            check(model.SelectedCharacter.Id == "drawing-neulbo" && model.Character.Name.StartsWith("그림 늘보군"), "selection_combo_changes_character");
            window.CharacterSelector.SelectedItem = null;
            check(model.SelectedCharacter.Id == "drawing-neulbo", "selection_transient_null_preserves_model");
            window.CharacterSelector.SelectedItem = model.SelectedCharacter;
            var closing = model.SelectCharacterAsync("pelly");
            window.Close(); await Task.Delay(100); await closing;
            check(!window.IsVisible && !model.CanChooseCharacter && !await model.SelectCharacterAsync("neulbo"), "selection_close_drops_pending_load");
        }
        finally
        {
            if (window.IsVisible) { window.Close(); await Task.Delay(100); }
            Directory.Delete(folder, true);
        }
    }
}
