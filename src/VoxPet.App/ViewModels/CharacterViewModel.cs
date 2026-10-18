using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using System.Runtime.InteropServices;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using VoxPet.Core.Legacy;
using VoxPet.Core.Character;

namespace VoxPet.App.ViewModels;

/// <summary>두 창이 공유하는 frozen PNG와 표정·모션 상태. UI 스레드에서만 갱신한다.</summary>
public sealed class CharacterViewModel : ObservableObject
{
    private System.Collections.ObjectModel.ObservableCollection<CostumeItemViewModel> costumeLayers = [];
    private readonly HashSet<CostumeItemViewModel> subscribedCostumes = [];
    public System.Collections.ObjectModel.ObservableCollection<CostumeItemViewModel> CostumeLayers
    {
        get => costumeLayers;
        internal set
        {
            costumeLayers.CollectionChanged -= CostumesChanged;
            foreach (var item in subscribedCostumes) item.PropertyChanged -= CostumeChanged;
            subscribedCostumes.Clear(); costumeLayers = value; costumeLayers.CollectionChanged += CostumesChanged; SynchronizeCostumes();
        }
    }
    private void CostumesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => SynchronizeCostumes();
    private void CostumeChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RefreshMotion();
    private void SynchronizeCostumes()
    {
        foreach (var item in subscribedCostumes.Where(item => !costumeLayers.Contains(item)).ToArray()) { item.PropertyChanged -= CostumeChanged; subscribedCostumes.Remove(item); }
        foreach (var item in costumeLayers) if (subscribedCostumes.Add(item)) item.PropertyChanged += CostumeChanged;
        RefreshMotion();
    }
    public CharacterRig Rig { get; private set; } = CharacterRig.Default(CharacterCatalog.DefaultId);
    public MotionPose Motion { get; private set; } = MotionPose.Rest;
    public bool ShowMotionGuides { get; internal set; }
    public IReadOnlyList<RuntimeLayer>? RuntimeLayers { get; private set; }
    public ExpressionKind ExpressionKind => profile.Kind;
    public bool IsBlinking => profile.Blink && current.EyeOpen < .5;
    private string runtimeName = "";
    public void SetRuntimeLayers(IReadOnlyList<RuntimeLayer>? layers, string? displayName = null)
    {
        bool changed = (RuntimeLayers == null) != (layers == null);
        if (displayName != null) runtimeName = displayName;
        RuntimeLayers = layers; Notify(nameof(RuntimeLayers)); if (changed || displayName != null) Notify(nameof(Name));
    }
    private CharacterRuntimeViewModel? runtimeScene;
    private readonly LegacySpriteSheetRuntime legacy = new() { LivePerformance = true };
    private CharacterRuntimeViewModel legacyScene = null!;
    private LoadedCharacterPackage? companionSamples;
    private readonly Dictionary<string, string[]> companionSelections = [];
    private IReadOnlyDictionary<string, ImageSource> partImages = new Dictionary<string, ImageSource>();
    private ImageSource[,]? partSheet;
    private string partCharacter = "";
    public void SetCompanionEquipment(string? weapon, bool hold)
    { legacy.WeaponId = weapon; legacy.HoldWeapon = hold; RefreshMotion(); }
    public string CompanionCharacterId { get; private set; } = "";
    public CharacterRuntimeViewModel CompanionScene => legacyScene;
    public bool HasCompanionSamples => companionSamples != null;
    internal void SetCompanionSamples(LoadedCharacterPackage? loaded, string characterId)
    {
        if (CompanionCharacterId != characterId || (companionSamples == null) != (loaded == null))
        { PreviousSprite = null; previousHeadSnapshot = null; legacy.ResetTransition(); }
        if (companionSamples != null) companionSelections[CompanionCharacterId] = legacyScene.Runtime.Attachments.EnabledIds.ToArray();
        legacyScene.Stop(); companionSamples = loaded; CompanionCharacterId = characterId; legacy.Samples = loaded?.Package.Character;
        legacy.PreserveForegroundWing = characterId == "pelly";
        RebuildLegacyScene();
        var valid = legacyScene.Runtime.Definition.Attachments.Select(a => a.Id).ToHashSet();
        legacyScene.Runtime.Attachments.Restore((companionSelections.GetValueOrDefault(characterId) ?? []).Where(valid.Contains));
        cachedSprites = null; RefreshMotion(); Notify(nameof(HasCompanionSamples)); Notify(nameof(CompanionCharacterId));
    }
    private void RebuildLegacyScene()
    {
        var engine = legacyScene.Runtime.Actions; var enabled = legacyScene.Runtime.Attachments.EnabledIds;
        legacyScene.ReleaseFacePreparation();
        var definition = LegacySpriteSheetAdapter.Convert(sprites.GetLength(0), profile, companionSamples?.Package.Character);
        legacyScene = new(new CharacterRuntime(definition, processor: legacy, geometry: companionSamples == null ? null : legacy,
            startedAt: Math.Max(now, engine.LastTime), actions: engine), legacyScene.SourceImages, Math.Max(now, engine.LastTime),
            interpolateFace: companionSamples != null, sideView: CompanionCharacterId == "pelly");
        legacyScene.FacePreparationChanged += () => Notify(nameof(Motion));
        var valid = definition.Attachments.Select(a => a.Id).ToHashSet();
        legacyScene.Runtime.Attachments.Restore(enabled.Where(valid.Contains));
        Notify(nameof(CompanionScene));
    }
    public void PlayCompanionAction(string id) { if (HasCompanionSamples) legacyScene.Runtime.RequestAction(id); }
    public void StopCompanionActions() { legacyScene.Stop(); RefreshMotion(); }
    internal async Task<VoxPet.Core.Packages.VoxPetPackage?> EditableCompanionAsync()
    {
        if (companionSamples == null) return null;
        var loaded = companionSamples; var sheet = (ImageSource[,])sprites.Clone(); var expression = profile;
        SetLegacyImages(); var separated = partImages;
        string id = CompanionCharacterId; string displayName = defaultName;
        return await Task.Run(() =>
        {
            var definition = LegacySpriteSheetAdapter.Convert(sheet.GetLength(0), expression, loaded.Package.Character,
                id == "pelly" ? VoxPet.Core.Motion.CompanionActing.Pelly : VoxPet.Core.Motion.CompanionActing.Neulbo);
            var assets = loaded.Package.AssetPaths.ToDictionary(p => p, loaded.Package.ReadAsset);
            foreach (var part in separated) assets[part.Key] = CompanionPartImages.Encode(part.Value);
            for (int mouth = 0; mouth < sheet.GetLength(0); mouth++) for (int eye = 0; eye < 2; eye++)
            {
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create((BitmapSource)sheet[mouth, eye]));
                using var buffer = new MemoryStream(); encoder.Save(buffer); assets[LegacySpriteSheetAdapter.Asset(mouth, eye)] = buffer.ToArray();
            }
            var manifest = new VoxPet.Core.Packages.VoxPetManifest("companion." + id, displayName + " · 의상 편집", "1.0.0",
                description: "개인 캐릭터의 현재 표정과 의상·동작. 내보내기는 이 PC에서만 수행합니다.", thumbnail: LegacySpriteSheetAdapter.Asset(0, 1),
                actions: loaded.Package.Manifest.Actions, attachments: loaded.Package.Manifest.Attachments);
            return new VoxPet.Core.Packages.VoxPetPackageValidator().Validate(manifest, definition, assets);
        });
    }
    public VoxPet.Core.Actions.ActionEngine LegacyActions => legacyScene.Runtime.Actions;
    public CharacterRuntimeViewModel RuntimeScene => runtimeScene ?? legacyScene;
    internal async Task CloseFacePreparationAsync()
    {
        legacyScene.ReleaseFacePreparation(); runtimeScene?.ReleaseFacePreparation();
        await legacyScene.FacePreparation;
        if (runtimeScene != null) await runtimeScene.FacePreparation;
    }
    public void SetRuntimeScene(CharacterRuntimeViewModel? scene)
    {
        if (!ReferenceEquals(runtimeScene, scene))
        {
            if (runtimeScene != null) runtimeScene.FacePreparationChanged -= NativeFacePrepared;
            if (scene != null) scene.FacePreparationChanged += NativeFacePrepared;
        }
        runtimeScene = scene; Notify(nameof(RuntimeScene)); Notify(nameof(OutputTransform));
    }
    private void NativeFacePrepared() => Notify(nameof(RuntimeScene));
    private HandPlayback? playback;
    private HandMotionProfile? handProfile;
    public void SetMotion(CharacterRig rig, HandPlayback? hand, HandMotionProfile? selected)
    { Rig = rig; playback = hand; handProfile = selected; RefreshMotion(); }
    public void RefreshMotion()
    {
        if (legacyScene == null) return;
        legacy.Rig = Rig; legacy.ShowGuides = ShowMotionGuides; legacy.Parameters = current; legacy.Playback = playback; legacy.HandProfile = handProfile;
        legacy.Costumes = CostumeLayers.Where(item => item.Enabled && item.Image != null)
            .Select(item => new LegacyCostumeLayer(item.Profile, "legacy/costume-" + item.Id.ToString("N") + ".png")).ToArray();
        SetLegacyImages(); legacyScene.Update(now, Math.Clamp(current.MouthOpen, 0, 1));
        Motion = legacy.Motion; Notify(nameof(Motion));
        Sprite = legacyScene.Images[legacy.Sprite]; Blend = legacy.Blend;
        if (!legacy.HasPrevious) { PreviousSprite = null; previousHeadSnapshot = null; }
        Bounce = -current.BodyBounce;
        if (HasCompanionSamples) Notify(nameof(OutputTransform));
        TearLeftY = legacy.TearLeftY; TearRightY = legacy.TearRightY;
        TearLeftOpacity = legacy.TearLeftOpacity; TearRightOpacity = legacy.TearRightOpacity;
    }
    private ImageSource[,]? cachedSprites;
    private ImageSource? cachedPrevious;
    private CharacterRig? cachedGuideRig;
    private bool cachedGuides;
    private (Guid Id, ImageSource? Image)[] cachedCostumes = [];
    private void SetLegacyImages()
    {
        var costumes = CostumeLayers.Where(item => item.Enabled && item.Image != null).Select(item => (item.Id, item.Image)).ToArray();
        if (ReferenceEquals(cachedSprites, sprites) && ReferenceEquals(cachedPrevious, PreviousSprite) && cachedGuides == ShowMotionGuides &&
            (!ShowMotionGuides || cachedGuideRig == Rig) && costumes.SequenceEqual(cachedCostumes)) return;
        cachedSprites = sprites; cachedPrevious = PreviousSprite; cachedGuides = ShowMotionGuides; cachedGuideRig = Rig; cachedCostumes = costumes;
        var images = new Dictionary<string, ImageSource>(LegacyDrawingAssets.All);
        if (companionSamples != null) foreach (var pair in companionSamples.Images) images.Add(pair.Key, pair.Value);
        if (companionSamples != null)
        {
            if (!ReferenceEquals(partSheet, sprites) || partCharacter != CompanionCharacterId)
            { partSheet = sprites; partCharacter = CompanionCharacterId; partImages = CompanionPartImages.Create(sprites, CompanionCharacterId == "pelly"); }
            foreach (var pair in partImages) images[pair.Key] = pair.Value;
        }
        for (int m = 0; m < sprites.GetLength(0); m++) for (int e = 0; e < 2; e++) images.Add(LegacySpriteSheetAdapter.Asset(m, e), sprites[m, e]);
        if (PreviousSprite != null)
        {
            images.Add("legacy/previous.png", PreviousSprite);
            if (companionSamples != null) images.Add("legacy/previous-head.png", previousHeadSnapshot ?? CompanionPartImages.PreviousHead(PreviousSprite, sprites[0, 1], CompanionCharacterId == "pelly"));
        }
        foreach (var item in CostumeLayers.Where(item => item.Enabled && item.Image != null)) images.Add("legacy/costume-" + item.Id.ToString("N") + ".png", item.Image!);
        if (ShowMotionGuides) images.Add("legacy/guides.png", LegacyDrawingAssets.Guides(Rig));
        legacyScene.SetImages(images);
    }
    private readonly Dictionary<ExpressionKind, ImageSource[,]> defaults = [];
    private ImageSource[,] sprites;
    private Dictionary<ExpressionKind, ImageSource[,]> personalDefaults = [];
    public bool IsBuiltInDefault => personalDefaults.Count == 0;
    public bool UsesCustomSheet { get; private set; }
    public int DefaultExpressionCount => personalDefaults.Count == 0 ? defaults.Values.Distinct().Count() : personalDefaults.Count;
    public int MouthFrameCount => sprites.GetLength(0);
    public int DefaultWarnings { get; private set; }
    private string defaultName = "Violet Cat";
    private CharacterParameters current = new(0, 0, 0, 0, 1, MouthState.Closed);
    private ExpressionProfile profile = ExpressionProfile.Default(0);
    private string name = "Violet Cat · 기본 캐릭터";
    private ImageSource sprite;
    private ImageSource? previousSprite;
    private ImageSource? previousHeadSnapshot;
    private double bounce, blend = 1, now;
    private double tearLeftY, tearRightY, tearLeftOpacity, tearRightOpacity;

    public CharacterViewModel()
    {
        string[] mouths = ["closed", "half", "open"];
        foreach (var expression in Enum.GetValues<ExpressionKind>().Where(kind => kind <= ExpressionKind.Sleepy))
        {
            var sheet = new ImageSource[3, 2];
            string prefix = expression == ExpressionKind.Neutral ? "" : expression.ToString().ToLowerInvariant() + "-";
            for (int mouth = 0; mouth < 3; mouth++)
                for (int eye = 0; eye < 2; eye++)
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri($"pack://application:,,,/Assets/Characters/{prefix}{mouths[mouth]}{(eye == 0 ? "-blink" : "")}.png");
                    bitmap.EndInit(); bitmap.Freeze(); sheet[mouth, eye] = bitmap;
                }
            defaults.Add(expression, sheet);
        }
        // 공개 고양이는 기존6그림을 유지한다. 추가 종류의 실제 얼굴은 개인 표정 가족에서 읽는다.
        foreach (var kind in Enum.GetValues<ExpressionKind>().Where(kind => kind > ExpressionKind.Sleepy))
            defaults.Add(kind, defaults[kind switch
            {
                ExpressionKind.Smug => ExpressionKind.Sleepy,
                ExpressionKind.Confused => ExpressionKind.Surprised,
                _ => ExpressionKind.Happy
            }]);
        sprites = defaults[ExpressionKind.Neutral]; sprite = sprites[0, 1];
        legacyScene = new(new CharacterRuntime(new LegacySpriteSheetSource(3).Load(), processor: legacy),
            new Dictionary<string, ImageSource> { [LegacySpriteSheetAdapter.Asset(0, 1)] = sprite });
        costumeLayers.CollectionChanged += CostumesChanged; RefreshMotion();
    }
    /// <summary>개인 실행 폴더의 선택적 기본 PNG를 비동기로 읽는다. 실패해도 내장 캐릭터로 시작하며 원본과 저장 슬롯은 수정하지 않는다.</summary>
    public async Task<bool> LoadDefaultAsync(string path)
    {
        try
        {
            var loaded = await Task.Run(() => LoadFamily(path));
            SetDefault(loaded, Path.GetFileNameWithoutExtension(path)); return true;
        }
        catch (Exception ex) when (Expected(ex))
        { return false; }
    }
    internal sealed record CharacterFamily(Dictionary<ExpressionKind, ImageSource[,]> Sheets, int Warnings);
    /// <summary>worker에서 가족 전체를 준비한다. UI 적용과 분리하여 오래된 선택 결과를 버린다.</summary>
    internal static CharacterFamily LoadFamily(string path)
    {
        var family = new Dictionary<ExpressionKind, ImageSource[,]> { [ExpressionKind.Neutral] = LegacyCharacterImporter.Load(path) };
        int warnings = 0;
        string folder = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "-표정");
        foreach (var kind in Enum.GetValues<ExpressionKind>().Where(kind => kind != ExpressionKind.Neutral))
        {
            string variant = Path.Combine(folder, kind.ToString().ToLowerInvariant() + ".png");
            if (!File.Exists(variant)) continue;
            try
            {
                var sheet = LegacyCharacterImporter.Load(variant);
                if (sheet.GetLength(0) != family[ExpressionKind.Neutral].GetLength(0) || sheet[0, 0].Width != family[ExpressionKind.Neutral][0, 0].Width)
                    throw new ArgumentException("기본 표정 시트의 크기와 입 단계가 같아야 합니다.");
                family.Add(kind, sheet);
            }
            catch (Exception ex) when (Expected(ex)) { warnings++; }
        }
        return new(family, warnings);
    }
    internal void SetDefault(CharacterFamily family, string displayName)
    {
        personalDefaults = family.Sheets; DefaultWarnings = family.Warnings; defaultName = displayName;
        RestoreDefault(); Notify(nameof(DefaultExpressionCount)); Notify(nameof(MouthFrameCount));
    }
    internal Action CaptureAppearance()
    {
        var savedProfile = profile; var savedSprites = sprites; string savedName = Name; bool savedCustom = UsesCustomSheet;
        return () => { Apply(savedProfile, savedSprites); UsesCustomSheet = savedCustom; Name = savedName; };
    }
    private static bool Expected(Exception ex) => ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or FormatException or OverflowException or COMException;
    public string Name { get => RuntimeLayers != null ? runtimeName : name; private set => Set(ref name, value); }
    public ImageSource Sprite { get => sprite; private set => Set(ref sprite, value); }
    public ImageSource? PreviousSprite { get => previousSprite; private set => Set(ref previousSprite, value); }
    public double Blend { get => blend; private set { if (Set(ref blend, value)) Notify(nameof(PreviousOpacity)); } }
    public double PreviousOpacity => 1 - Blend;
    public double Bounce { get => bounce; private set { if (Set(ref bounce, value)) Notify(nameof(OutputTransform)); } }
    public Transform OutputTransform
    {
        get
        {
            if (runtimeScene == null && HasCompanionSamples)
            {
                var companion = AffineTransform2D.FromTransform(legacyScene.Frame.ViewportTransform);
                return new MatrixTransform(companion.M11, companion.M12, companion.M21, companion.M22, companion.OffsetX, companion.OffsetY);
            }
            if (runtimeScene?.UseViewportTransform != true) return new TranslateTransform(0, Bounce);
            var affine = AffineTransform2D.FromTransform(runtimeScene.Frame.ViewportTransform);
            return new MatrixTransform(affine.M11, affine.M12, affine.M21, affine.M22, affine.OffsetX, affine.OffsetY);
        }
    }
    public double TearLeftX => profile.TearLeft * 512 - 5;
    public double TearRightX => profile.TearRight * 512 - 5;
    public double TearLeftY { get => tearLeftY; private set => Set(ref tearLeftY, value); }
    public double TearRightY { get => tearRightY; private set => Set(ref tearRightY, value); }
    public double TearLeftOpacity { get => tearLeftOpacity; private set => Set(ref tearLeftOpacity, value); }
    public double TearRightOpacity { get => tearRightOpacity; private set => Set(ref tearRightOpacity, value); }

    /// <summary>진행 중인 전환도 현재 합성 픽셀을 한 장으로 고정해 중첩 참조와 시각적인 점프를 방지한다.</summary>
    public void Apply(ExpressionProfile expression, ImageSource[,]? sheet = null)
    {
        expression.Validate();
        if (HasCompanionSamples)
        {
            var head = new DrawingVisual();
            using (var drawing = head.RenderOpen())
                foreach (var layer in legacyScene.Frame.Layers.Where(l => l.Id is "part-head" or "legacy-previous-head"))
                {
                    drawing.PushOpacity(layer.Opacity); drawing.DrawImage(legacyScene.Images[layer.Asset], new Rect(0, 0, 512, 512)); drawing.Pop();
                }
            var captured = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); captured.Render(head); captured.Freeze(); previousHeadSnapshot = captured;
        }
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            if (PreviousSprite != null && Blend < 1)
            {
                drawing.PushOpacity(PreviousOpacity); drawing.DrawImage(PreviousSprite, new Rect(0, 0, 512, 512)); drawing.Pop();
            }
            drawing.PushOpacity(Blend); drawing.DrawImage(Sprite, new Rect(0, 0, 512, 512)); drawing.Pop();
            // 떠나는 표정의 눈물도 전환 snapshot에 포함해 갑자기 사라지지 않게 한다.
            DrawTear(drawing, TearLeftX, TearLeftY, TearLeftOpacity);
            DrawTear(drawing, TearRightX, TearRightY, TearRightOpacity);
        }
        var snapshot = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32);
        snapshot.Render(visual); snapshot.Freeze();
        PreviousSprite = snapshot; Blend = 0;
        profile = expression;
        var builtin = personalDefaults.TryGetValue(expression.Kind, out var variant) ? variant :
            personalDefaults.TryGetValue(ExpressionKind.Neutral, out var neutral) ? neutral : defaults[expression.Kind];
        sprites = sheet ?? builtin; UsesCustomSheet = sheet != null;
        legacy.Apply(expression, sprites.GetLength(0), now);
        // source의 정의도 실제 시트/표정과 동기화한다. scheduler가 참조하는 중앙 ActionEngine은 유지한다.
        RebuildLegacyScene();
        cachedSprites = null;
        Name = sheet == null ? $"{defaultName} · {expression.Name}" : $"{expression.Name} · 사용자 캐릭터";
        Notify(nameof(TearLeftX)); Notify(nameof(TearRightX)); Update(current, now);
    }
    private static void DrawTear(DrawingContext drawing, double x, double y, double opacity)
    {
        if (opacity <= 0) return;
        drawing.PushOpacity(opacity); drawing.PushTransform(new TranslateTransform(x, y));
        drawing.DrawGeometry(Brushes.LightSkyBlue, null, Geometry.Parse("M5,0 C4,5 0,8 0,12 C0,20 10,20 10,12 C10,8 6,5 5,0 Z"));
        drawing.Pop(); drawing.Pop();
    }
    // 기존 일반 PNG 불러오기 경로는 현재 실행의 표정으로 적용한다. 슬롯 저장은 명시적 저장 버튼을 사용한다.
    public void UseSheet(ImageSource[,] sheet, string displayName) => Apply(new(displayName.Length > 40 ? displayName[..40] : displayName), sheet);
    public void RestoreDefault() => Apply(ExpressionProfile.Default(0));
    public void Update(CharacterParameters state) => Update(state, now);
    public void Update(CharacterParameters state, double seconds)
    {
        current = state; now = seconds; RefreshMotion();
    }
    /// <summary>저장용 3×2 또는 8×2 RGBA PNG를 작업 스레드에서 인코딩한다. 모든 원본 이미지는 frozen이다.</summary>
    public static byte[] EncodeSheet(ImageSource[,] sheet)
    {
        int cell = (int)sheet[0, 0].Width;
        int columns = sheet.GetLength(0);
        SpriteSheetLayout.Validate(cell * columns, cell * 2);
        var pixels = new byte[cell * columns * cell * 2 * 4];
        for (int mouth = 0; mouth < columns; mouth++)
            for (int row = 0; row < 2; row++)
            {
                var image = new FormatConvertedBitmap((BitmapSource)sheet[mouth, row == 0 ? 1 : 0], PixelFormats.Bgra32, null, 0);
                var source = new byte[cell * cell * 4]; image.CopyPixels(source, cell * 4, 0);
                for (int y = 0; y < cell; y++)
                    Buffer.BlockCopy(source, y * cell * 4, pixels, ((row * cell + y) * cell * columns + mouth * cell) * 4, cell * 4);
            }
        var bitmap = BitmapSource.Create(cell * columns, cell * 2, 96, 96, PixelFormats.Bgra32, null, pixels, cell * columns * 4);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream(); encoder.Save(stream); return stream.ToArray();
    }
}
