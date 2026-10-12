using System.IO;
using System.Windows;
using System.Windows.Media;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.Views;

/// <summary>개인 자산 없이 공용 고양이 표정 가족으로 캐릭터 순회/꾸미기/좌클릭 경로의 실제 창을 검사한다.</summary>
internal static class CustomizationQa
{
    internal static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-customization-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        var art = new CharacterViewModel(); var sheets = new Dictionary<ExpressionKind, byte[]>();
        foreach (var kind in Enum.GetValues<ExpressionKind>())
        {
            art.Apply(ExpressionProfile.Default((int)kind)); var sheet = new ImageSource[3, 2];
            for (int mouth = 0; mouth < 3; mouth++) for (int eye = 0; eye < 2; eye++)
            { art.Update(new(mouth / 2d, 0, 0, 0, eye, (MouthState)mouth), 100); sheet[mouth, eye] = art.Sprite; }
            sheets[kind] = await Task.Run(() => CharacterViewModel.EncodeSheet(sheet));
        }
        foreach (var item in CharacterCatalog.All)
        {
            File.WriteAllBytes(Path.Combine(folder, item.FileName), sheets[ExpressionKind.Neutral]);
            string variants = Path.Combine(folder, Path.GetFileNameWithoutExtension(item.FileName) + "-표정"); Directory.CreateDirectory(variants);
            foreach (var (kind, bytes) in sheets.Where(pair => pair.Key != ExpressionKind.Neutral)) File.WriteAllBytes(Path.Combine(variants, kind.ToString().ToLowerInvariant() + ".png"), bytes);
        }
        int inputs = 0;
        var model = new MainViewModel(true, () => [], _ => { inputs++; throw new InvalidOperationException(); }, folder, Path.Combine(folder, "data"));
        var main = new MainWindow(true, new Rect(0, 0, 1000, 730), model); Action? resume = null;
        try
        {
            main.Show(); await main.Ready; resume = model.PauseVisualForQa();
            var broadcast = main.ShowCharacter(); broadcast.Left = 30; broadcast.Top = 40; model.GreenBackground = true;
            var slots = model.Expressions.Slots.Select(slot => slot.Saved).ToArray(); double now = 200;
            foreach (var item in CharacterCatalog.All)
            {
                check(await model.SelectCharacterAsync(item.Id) && model.Character.DefaultExpressionCount == 12, "customization_family12_" + item.Id);
                foreach (var kind in Enum.GetValues<ExpressionKind>())
                {
                    await model.Expressions.PreviewBuiltinAsync((int)kind);
                    model.Character.Update(new(.5, 0, 0, 0, 1, MouthState.Half), now++);
                    await ScreenEvidence.CaptureAsync(broadcast, $"custom-expression-{item.Id}-{kind.ToString().ToLowerInvariant()}", check, new { publicFixture = true, character = item.Id, expression = kind });
                }
                await model.Expressions.PreviewBuiltinAsync(0);
                foreach (var category in Enum.GetValues<CostumeCategory>())
                {
                    model.Wardrobe.RemoveAllCommand.Execute(null);
                    check(await model.Wardrobe.AddPresetAsync(category), $"customization_preset-{item.Id}-{category}");
                    var preset = model.Wardrobe.Selected!;
                    check(preset.Image?.IsFrozen == true && preset.Profile.CharacterId == item.Id && preset.Profile.PresetKey == category.ToString(), $"customization_preset-frozen-{item.Id}-{category}");
                    model.Character.Update(new(0, 0, 0, 0, 1, MouthState.Closed), now++);
                    await ScreenEvidence.CaptureAsync(broadcast, $"custom-preset-{item.Id}-{category.ToString().ToLowerInvariant()}", check);
                    check(await model.Wardrobe.AddPresetAsync(category) && ReferenceEquals(preset, model.Wardrobe.Selected), $"customization_preset-reuse-{item.Id}-{category}");
                }
                model.Wardrobe.RemoveAllCommand.Execute(null); await model.Wardrobe.AddPresetAsync(CostumeCategory.Top);
                foreach (var gesture in Enum.GetValues<HandGesture>())
                {
                    model.Hands.SelectedIndex = (int)gesture; model.Hands.ClickCommand.Execute(null);
                    check(model.Hands.Selected.Enabled && model.Hands.Selected.LeftClick && model.Hands.Selected.Mode == HandTriggerMode.Hotkey && model.Hands.Snapshot().Profiles.Count(p => p.Enabled && p.LeftClick && p.Mode == HandTriggerMode.Hotkey) == 1, $"customization_leftclick-{item.Id}-{gesture}");
                    model.Hands.LeftClick(); model.Hands.Update(now); model.Hands.StopCommand.Execute(null);
                    model.Character.SetMotion(CharacterRig.Default(item.Id), new(gesture, .5), HandMotionProfile.Default(gesture));
                    model.Character.Update(new(0, 0, 0, 0, 1, MouthState.Closed), now++);
                    await ScreenEvidence.CaptureAsync(broadcast, $"custom-hand-{item.Id}-{gesture.ToString().ToLowerInvariant()}", check);
                }
                model.Hands.StopCommand.Execute(null);
            }
            await model.SelectCharacterAsync("neulbo-man"); await model.Expressions.PreviewBuiltinAsync(7); string original = model.Character.Name;
            await model.TestCharactersAsync(TimeSpan.FromMilliseconds(3));
            check(model.LastTestedCharacters.SequenceEqual(CharacterCatalog.All.Select(item => item.Id)) && !model.IsTestingCharacters && model.Character.Name == original && model.Character.DefaultExpressionCount == 12, "customization_tour-five-restore");
            check(model.Expressions.Slots.Select(slot => slot.Saved).SequenceEqual(slots), "customization_tour-preserves-slots");
            var cancelled = model.TestCharactersAsync(TimeSpan.FromMilliseconds(30)); await Task.Delay(30); model.StopCharacterTestsCommand.Execute(null); await cancelled;
            check(!model.IsTestingCharacters && model.Character.Name == original, "customization_tour-cancel-restore");
            var switched = model.TestCharactersAsync(TimeSpan.FromMilliseconds(30)); await Task.Delay(30); await model.SelectCharacterAsync("pelly"); await switched;
            check(model.SelectedCharacter.Id == "pelly" && model.Character.Name.StartsWith("펠리") && !model.IsTestingCharacters, "customization_tour-new-selection-wins");
            var requested = model.TestCharactersAsync(TimeSpan.FromMilliseconds(30)); await Task.Delay(30); await model.Expressions.PreviewBuiltinAsync(3); await requested;
            check(model.SelectedCharacter.Id == "pelly" && model.Character.Name.StartsWith("펠리") && model.Character.Name.Contains("화남"), "customization_tour-requested-expression-wins");
            check(model.Wardrobe.Items.Count == 45, "customization_all45-items-preserved"); await model.Wardrobe.SaveAsync();
            var restart = new CostumeViewModel(true, Path.Combine(folder, "data", "Wardrobe")); await restart.InitializeAsync();
            foreach (var item in CharacterCatalog.All) { restart.SetCharacter(item.Id); check(restart.Filtered.Count() == 9, "customization_restart-scope-" + item.Id); }
            await restart.CloseAsync();
            main.SettingsTabs.SelectedIndex = 3; await ScreenEvidence.CaptureAsync(main, "custom-tour-restored", check);
            main.SettingsTabs.SelectedIndex = 5; await ScreenEvidence.CaptureAsync(main, "custom-preset-settings", check);
            var closeTour = model.TestCharactersAsync(TimeSpan.FromMilliseconds(30)); await Task.Delay(15); await main.ShutdownAsync(); await closeTour;
            check(!model.IsTestingCharacters && inputs == 0, "customization_close-tour-no-microphone");
        }
        finally { resume?.Invoke(); await main.ShutdownAsync(); main.Close(); Directory.Delete(folder, true); }
    }
}
