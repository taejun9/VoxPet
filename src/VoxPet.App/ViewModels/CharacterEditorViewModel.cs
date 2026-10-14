using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using VoxPet.App.Services;
using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Editing;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Motion;
using VoxPet.Core.Packages;
using VoxPet.Core.Triggers;
using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;

namespace VoxPet.App.ViewModels;

/// <summary>명시 apply transaction/worker 자산 준비와 같은 Core Runtime preview를 조정한다.</summary>
public sealed class CharacterEditorViewModel : ObservableObject
{
    public string[] Pages { get; } = ["기본 정보", "부위 / 이미지", "앵커", "얼굴 · 입 / 눈", "표정", "동작 · 타임라인", "부착물 · 의상", "미리보기 / 내보내기"];
    public ObservableCollection<EditorRow> Rows { get; } = [];
    public ObservableCollection<EditorRow> Details { get; } = [];
    public ObservableCollection<EditorField> Fields { get; } = [];
    public ObservableCollection<EditorField> DetailFields { get; } = [];
    public CharacterDocument? Document { get; private set; }
    public CharacterRuntimeViewModel? Scene { get; private set; }
    public bool HasDocument => Document != null;
    public bool CanInteract => !busy && !closing;
    public bool CanEdit => HasDocument && !busy && !closing;
    public bool CanExport => CanEdit && pendingForms.Count == 0 && !pending && !detailPending;
    private bool busy, closing, pending, detailPending, loadingFields, playing, guides = true;
    private long revision;
    private readonly SemaphoreSlim worker = new(1, 1);
    private readonly Dictionary<string, Dictionary<string, string>> pendingForms = [];
    private string? bankId;
    private string FormKey(bool detailForm) => $"{Page}/{Selected?.Id ?? "metadata"}/{bankId ?? ""}/" + (detailForm ? SelectedDetail?.Id ?? "detail" : "main");
    public string[] SpriteBanks => Document?.Package.Character.SpriteSheet?.Expressions.Select(e => e.Id).ToArray() ?? [];
    public string? SpriteBank
    {
        get => bankId;
        set
        {
            if (busy || !Set(ref bankId, value)) return;
            ReloadRows();
            if (value != null && Expressions.Any(e => e.Id == value)) { PreviewExpression = value; Scene?.Runtime.SetExpression(value); Notify(nameof(PreviewExpression)); UpdateFrame(); }
        }
    }
    public RelayCommand DiscardCommand { get; }
    public AsyncCommand AddPoseCommand { get; }
    private int page;
    private EditorRow? selected, detail;
    private double voice, time, scrub;
    private string status = "새 캐릭터를 만들거나 .voxpet을 불러오세요.";
    private readonly DispatcherTimer timer;
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private double previous;
    public string Status { get => status; private set => Set(ref status, value); }
    public bool IsBusy => busy;
    public int Page { get => page; set { if (busy || value < 0 || value >= Pages.Length || !Set(ref page, value)) return; ReloadRows(); } }
    public EditorRow? Selected { get => selected; set { if (!busy && Set(ref selected, value)) LoadFields(); } }
    public EditorRow? SelectedDetail { get => detail; set { if (!busy && Set(ref detail, value)) LoadDetailFields(); } }
    public bool Guides { get => guides; set { if (Set(ref guides, value)) Notify(nameof(Scene)); } }
    public double Voice { get => voice; set { if (Set(ref voice, Math.Clamp(value, 0, 1))) UpdateFrame(); } }
    public double Scrub { get => scrub; set { if (Set(ref scrub, Math.Max(0, value))) Seek(value); } }
    public double Duration => Selected?.Item is ActionDefinition a ? a.DurationMs : Actions.FirstOrDefault(a => a.Id == PreviewAction)?.DurationMs ?? 1000;
    public string PreviewState => Scene == null ? "" : $"표정 {Scene.Runtime.State.ExpressionId} · 입 {Scene.Runtime.MouthFrame + 1} · 동작 {Scene.Runtime.State.ActionId ?? "idle"}";
    public IReadOnlyList<ExpressionDefinition> Expressions => Document?.Package.Character.Expressions ?? [];
    public IReadOnlyList<ActionDefinition> Actions => Document?.Package.Character.Actions ?? [];
    public IReadOnlyList<AttachmentDefinition> Attachments => Document?.Package.Character.Attachments ?? [];
    public string? PreviewExpression { get; set; }
    private string? previewAction;
    public string? PreviewAction { get => previewAction; set { if (Set(ref previewAction, value)) Notify(nameof(Duration)); } }
    public string? PreviewAttachment { get; set; }
    public AsyncCommand NewCommand { get; }
    public AsyncCommand ImportCommand { get; }
    public AsyncCommand ApplyCommand { get; }
    public AsyncCommand ApplyDetailCommand { get; }
    public AsyncCommand AddCommand { get; }
    public AsyncCommand DeleteCommand { get; }
    public AsyncCommand AddDetailCommand { get; }
    public AsyncCommand DeleteDetailCommand { get; }
    public AsyncCommand ReplaceImageCommand { get; }
    public AsyncCommand ValidateCommand { get; }
    public AsyncCommand ExportCommand { get; }
    public RelayCommand PlayCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ExpressionCommand { get; }
    public RelayCommand AttachmentCommand { get; }
    public event Func<string?>? ChoosePackage;
    public event Func<string?>? ChoosePng;
    public event Func<string?>? ChooseExport;
    public event Action? Changed;
    public CharacterEditorViewModel()
    {
        DiscardCommand = new(() => { pendingForms.Clear(); pending = detailPending = false; LoadFields(); Status = "미적용 입력을 취소했습니다. 적용된 문서는 유지합니다."; }, () => CanEdit);
        AddPoseCommand = new(AddPoseAsync, () => CanEdit && Page == 5);
        NewCommand = new(NewAsync, () => !busy && !closing);
        ImportCommand = new(async () => { if (ChoosePackage?.Invoke() is { } path) await OpenAsync(path); }, () => !busy && !closing);
        ApplyCommand = new(ApplyAsync, () => CanEdit);
        ApplyDetailCommand = new(ApplyDetailAsync, () => CanEdit && SelectedDetail != null);
        AddCommand = new(AddAsync, () => CanEdit && Page is > 0 and < 7);
        DeleteCommand = new(DeleteAsync, () => CanEdit && Selected != null && Page is > 0 and < 7);
        AddDetailCommand = new(AddDetailAsync, () => CanEdit && Page is 5 or 6);
        DeleteDetailCommand = new(DeleteDetailAsync, () => CanEdit && SelectedDetail != null && Page is 5 or 6);
        ReplaceImageCommand = new(async () => { if (ChoosePng?.Invoke() is { } path) await ReplaceImageAsync(path); }, () => CanEdit);
        ValidateCommand = new(ValidateAsync, () => CanExport);
        ExportCommand = new(async () => { if (ChooseExport?.Invoke() is { } path) await ExportAsync(path); }, () => CanExport);
        PlayCommand = new(() => { ResetPreview(); string? id = Selected?.Item is ActionDefinition a ? a.Id : Actions.FirstOrDefault(a => a.Id == PreviewAction)?.Id ?? Actions.FirstOrDefault()?.Id; if (id != null) Scene?.Runtime.RequestAction(id); playing = true; UpdateFrame(); }, () => CanExport);
        StopCommand = new(() => { playing = false; Scene?.Stop(); UpdateFrame(); }, () => CanEdit);
        ExpressionCommand = new(() => { if (PreviewExpression != null) Scene?.Runtime.SetExpression(PreviewExpression); UpdateFrame(); }, () => CanEdit);
        AttachmentCommand = new(() => { if (PreviewAttachment != null && Scene != null) { bool enabled = Scene.Runtime.Attachments.EnabledIds.Contains(PreviewAttachment); Scene.Runtime.Attachments.SetEnabled(PreviewAttachment, !enabled); UpdateFrame(); } }, () => CanEdit);
        timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1000d / 60) }; timer.Tick += Tick;
    }
    private void Refresh()
    {
        foreach (string name in new[] { nameof(HasDocument), nameof(CanInteract), nameof(CanEdit), nameof(CanExport), nameof(IsBusy), nameof(Document), nameof(Scene), nameof(Expressions), nameof(Actions), nameof(Attachments), nameof(Duration), nameof(PreviewState), nameof(SpriteBanks), nameof(SpriteBank) }) Notify(name);
        foreach (var c in new[] { NewCommand, ImportCommand, ApplyCommand, ApplyDetailCommand, AddCommand, DeleteCommand, AddDetailCommand, DeleteDetailCommand, AddPoseCommand, ReplaceImageCommand, ValidateCommand, ExportCommand }) c.Refresh();
        foreach (var c in new[] { PlayCommand, StopCommand, ExpressionCommand, AttachmentCommand, DiscardCommand }) c.Refresh();
        Changed?.Invoke();
    }
    private async Task<bool> TransactionAsync(Func<CharacterDocument> edit, bool resetForms = false, bool detailApply = false, bool preserveForms = false)
    {
        if (busy || closing) return false;
        string formKey = FormKey(detailApply);
        long request = ++revision; busy = true; Refresh(); await worker.WaitAsync();
        try
        {
            var result = await Task.Run(() => { var next = edit(); var prepared = CharacterPackageLibrary.Decode(next.Package); return (Next: next, Images: prepared.Images, Runtime: next.Package.CreateRuntime(new Random(1))); });
            if (closing || request != revision) return false;
            var previousScene = resetForms ? null : Scene;
            Scene?.Stop(); Document = result.Next; Scene = new(result.Runtime, result.Images); time = 0; playing = true;
            string? expression = previousScene?.Runtime.State.ExpressionId;
            if (expression != null && Expressions.Any(e => e.Id == expression)) Scene.Runtime.SetExpression(expression);
            foreach (string id in previousScene?.Runtime.Attachments.EnabledIds ?? []) if (Attachments.Any(a => a.Id == id)) Scene.Runtime.Attachments.SetEnabled(id, true);
            if (resetForms) { PreviewExpression = Expressions.FirstOrDefault()?.Id; PreviewAction = Actions.FirstOrDefault()?.Id; PreviewAttachment = Attachments.FirstOrDefault()?.Id; }
            Notify(nameof(PreviewExpression)); Notify(nameof(PreviewAction)); Notify(nameof(PreviewAttachment));
            if (resetForms) { pendingForms.Clear(); bankId = Document.Package.Character.SpriteSheet?.Expressions.FirstOrDefault()?.Id; } else if (!preserveForms) pendingForms.Remove(formKey);
            if (bankId != null && !Document.Package.Character.SpriteSheet!.Expressions.Any(e => e.Id == bankId)) bankId = Document.Package.Character.SpriteSheet.Expressions.FirstOrDefault()?.Id;
            pending = detailPending = false; ReloadRows(); UpdateFrame(); Status = "편집 내용을 적용했습니다. 검증 후 내보낼 수 있습니다."; return true;
        }
        catch (Exception e) when (Expected(e)) { Status = "적용 실패 · 원본과 마지막 미리보기를 유지합니다. " + e.Message; return false; }
        finally { worker.Release(); busy = false; Refresh(); }
    }
    private static bool Expected(Exception e) => CharacterPackageLibrary.Expected(e) || e is InvalidOperationException;
    public Task<bool> OpenAsync(string path) => TransactionAsync(() => new(new VoxPetPackageLoader().Load(path)), resetForms: true);
    public Task<bool> OpenPackageAsync(VoxPetPackage package) => TransactionAsync(() => new(package), resetForms: true);
    public async Task NewAsync()
    {
        await TransactionAsync(() =>
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "Assets", "ImageSetTemplate");
            var original = ImageSetLoader.Load(Path.Combine(folder, "imageset.json")).Definition;
            var source = new LegacyImageSetSource(original).Load();
            AnchorDefinition[] anchors = [new("root"), new("body",256,330,"root"), new("head",0,-180,"body"), new("face",parent:"head"), new("eyes",parent:"face"), new("mouth",0,55,"face"),
                new("leftShoulder",-96,-50,"body"), new("rightShoulder",96,-50,"body"), new("leftHand",-8,90,"leftShoulder"), new("rightHand",8,90,"rightShoulder"),
                new("hips",0,70,"body"), new("leftFoot",-46,70,"hips"), new("rightFoot",46,70,"hips"), new("back",parent:"body")];
            var definition = new CharacterDefinition(source.Parts.Select(p => new CharacterPart(p.Id, p.Type, p.Asset, p.Type == "head" ? "head" : p.Side == "left" && p.Type == "arm" ? "leftHand" : p.Side == "right" && p.Type == "arm" ? "rightHand" : "body", p.ZIndex, p.Transform, p.Side)), anchors,
                mouth: source.Mouth, expressions: source.Expressions, face: source.Face);
            var assets = definition.Assets.ToDictionary(p => p, p => File.ReadAllBytes(Path.Combine(folder, p)));
            return new(new VoxPetPackageValidator().Validate(new("character." + Guid.NewGuid().ToString("N"), "새 캐릭터", "1.0.0"), definition, assets));
        }, resetForms: true);
    }
    private void ReloadRows()
    {
        string? id = selected?.Id, detailId = detail?.Id; Rows.Clear(); Details.Clear(); Fields.Clear(); DetailFields.Clear(); selected = detail = null;
        if (Document == null) return; var c = Document.Package.Character;
        ImageSource? Image(string? asset) => asset != null && Scene!.Images.TryGetValue(asset, out var image) ? image : null;
        IEnumerable<EditorRow> rows = Page switch
        {
            1 => c.Parts.Select(p => new EditorRow(p.Id, p.Id + " · " + p.Type, p, Image(p.Asset))),
            2 => c.Anchors.Select(a => new EditorRow(a.Id, a.Id, a)),
            3 => Enumerable.Range(0, c.Mouth?.Frames.Count ?? c.SpriteSheet?.Selector.Frames.Count ?? 0).Select(i => new EditorRow(i.ToString(), "입 프레임 " + i, i, Image(c.Mouth?.Frames[i] ?? (c.SpriteSheet?.Expressions.FirstOrDefault(e => e.Id == bankId) ?? c.SpriteSheet?.Expressions[0])?.Open[i]))),
            4 => c.Expressions.Select(e => new EditorRow(e.Id, e.Name, e, Image(e.EyesOpen))),
            5 => c.Actions.Select(a => new EditorRow(a.Id, a.Name, a)),
            6 => c.Attachments.Select(a => new EditorRow(a.Id, a.Name, a, Image(a.Layers.FirstOrDefault()?.Asset))),
            _ => []
        };
        foreach (var row in rows) Rows.Add(row); selected = Rows.FirstOrDefault(r => r.Id == id) ?? Rows.FirstOrDefault();
        Notify(nameof(Selected)); LoadFields(); if (detailId != null) { detail = Details.FirstOrDefault(r => r.Id == detailId) ?? Details.FirstOrDefault(); Notify(nameof(SelectedDetail)); LoadDetailFields(); }
        Refresh();
    }
    private void Field(ObservableCollection<EditorField> fields, string key, string label, object? value, IEnumerable<string>? choices = null, bool asset = false)
    {
        var f = new EditorField(key, label, value, choices, asset); f.PropertyChanged += (_, _) =>
        {
            if (!loadingFields)
            {
                bool isDetail = !ReferenceEquals(fields, Fields); if (!isDetail) pending = true; else detailPending = true;
                pendingForms[FormKey(isDetail)] = Values(fields); Status = "미적용 입력이 있습니다. 설정 적용 또는 입력 취소를 선택하세요."; Refresh();
            }
        }; fields.Add(f);
    }
    private static string[] Booleans => ["True", "False"];
    private void TransformFields(ObservableCollection<EditorField> f, Transform2D t)
    { Field(f, "x", "X", t.X); Field(f, "y", "Y", t.Y); Field(f, "sx", "배율 X", t.ScaleX); Field(f, "sy", "배율 Y", t.ScaleY); Field(f, "rotation", "회전 (°)", t.Rotation); }
    private void LoadFields()
    {
        loadingFields = true; Fields.Clear(); Details.Clear(); DetailFields.Clear(); detail = null;
        if (Document == null) { loadingFields = false; return; }
        var c = Document.Package.Character; var m = Document.Package.Manifest; string[] anchorIds = ["", .. c.Anchors.Select(a => a.Id)];
        switch (Page)
        {
            case 0:
                Field(Fields, "id", "ID", m.Id); Field(Fields, "name", "이름", m.Name); Field(Fields, "version", "버전", m.Version); Field(Fields, "author", "제작자", m.Author); Field(Fields, "description", "설명", m.Description);
                Field(Fields, "thumbnail", "썸네일 PNG", m.Thumbnail, asset: true); Field(Fields, "width", "Canvas 너비", c.CanvasWidth); Field(Fields, "height", "Canvas 높이", c.CanvasHeight); break;
            case 1 when Selected?.Item is CharacterPart p:
                Field(Fields, "id", "ID", p.Id); Field(Fields, "type", "유형", p.Type, ["body", "head", "arm", "leg", "custom"]); Field(Fields, "asset", "PNG", p.Asset, asset: true); Field(Fields, "anchor", "앵커", p.Anchor, anchorIds); Field(Fields, "side", "좌우", p.Side, ["", "left", "right"]); Field(Fields, "z", "Z-index", p.ZIndex); Field(Fields, "visible", "표시", p.Visible, Booleans); TransformFields(Fields, p.Transform); break;
            case 2 when Selected?.Item is AnchorDefinition a:
                Field(Fields, "id", "ID", a.Id); Field(Fields, "parent", "부모 (빈 값=Canvas)", a.Parent, anchorIds.Where(id => id != a.Id)); TransformFields(Fields, a.Transform); break;
            case 3:
                Field(Fields, "curve", "입 곡선", c.Mouth?.Curve ?? c.SpriteSheet?.Curve ?? "smooth", ["smooth", "linear", "legacy3", "legacy8"]);
                Field(Fields, "anchor", "입 앵커", c.Mouth?.Anchor ?? "mouth", anchorIds); Field(Fields, "z", "입 Z-index", c.Mouth?.ZIndex ?? 300);
                int index = Selected?.Item is int i ? i : 0;
                if (c.Mouth is { } mouth) { Field(Fields, "asset", "입 PNG", mouth.Frames[index], asset: true); TransformFields(Fields, mouth.FrameTransforms[index]); }
                if (c.SpriteSheet is { } bank) { Field(Fields, "sheetPart", "전체 시트 파츠", bank.Part, c.Parts.Select(p => p.Id)); var b = bank.Expressions.FirstOrDefault(e => e.Id == bankId) ?? bank.Expressions[0]; Field(Fields, "open", "눈 뜸 PNG", b.Open[index], asset: true); Field(Fields, "closed", "눈 감음 PNG", b.Closed[index], asset: true); Field(Fields, "bankBlink", "선택 표정 깜빡임", b.Blink, Booleans); }
                MotionFields(c); break;
            case 4 when Selected?.Item is ExpressionDefinition e:
                Field(Fields, "id", "ID", e.Id); Field(Fields, "name", "이름", e.Name); Field(Fields, "open", "눈 뜸 PNG", e.EyesOpen, asset: true); Field(Fields, "closed", "눈 감음 PNG", e.EyesClosed, asset: true); Field(Fields, "brows", "눈썹 PNG (선택)", e.Brows, asset: true);
                Field(Fields, "eyesAnchor", "눈 앵커", c.Face.EyesAnchor, anchorIds); Field(Fields, "browsAnchor", "눈썹 앵커", c.Face.BrowsAnchor, anchorIds); Field(Fields, "eyesZ", "눈 Z-index", c.Face.EyesZIndex); Field(Fields, "browsZ", "눈썹 Z-index", c.Face.BrowsZIndex); Field(Fields, "required", "얼굴 표시 파츠", c.Face.RequiredVisiblePart, ["", .. c.Parts.Select(p => p.Id)]); break;
            case 5 when Selected?.Item is ActionDefinition action:
                Field(Fields, "id", "ID", action.Id); Field(Fields, "name", "이름", action.Name); Field(Fields, "priority", "우선순위 (0~100)", action.Priority); Field(Fields, "duration", "길이 (ms)", action.DurationMs); Field(Fields, "mode", "재생", action.Playback.Mode, Enum.GetNames<ActionPlaybackMode>()); Field(Fields, "interrupt", "중단 가능", action.Playback.Interruptible, Booleans); Field(Fields, "idle", "idle 복귀", action.Playback.ReturnToIdle, Booleans); Field(Fields, "tag", "호환 태그", action.CompatibilityTag);
                foreach (var (evt, n) in action.Timeline.Select((x, n) => (x, n))) Details.Add(new("event-" + n, $"{evt.TimeMs:0.##}ms · {evt.Kind}", evt));
                foreach (var trigger in action.Triggers) Details.Add(new(trigger.Id, "트리거 · " + trigger.Kind, trigger));
                foreach (var pose in c.Poses) Details.Add(new("pose-" + pose.Id, "포즈 · " + pose.Name, pose)); break;
            case 6 when Selected?.Item is AttachmentDefinition attachment:
                Field(Fields, "id", "ID", attachment.Id); Field(Fields, "name", "이름", attachment.Name); Field(Fields, "category", "분류", attachment.Category, AttachmentSystem.Categories); Field(Fields, "tag", "호환 태그", attachment.CompatibilityTag); Field(Fields, "group", "배타 그룹", attachment.ExclusiveGroup);
                foreach (var layer in attachment.Layers) Details.Add(new(layer.Id, layer.Id, layer)); break;
        }
        RestoreForm(Fields, false); loadingFields = false; detail = Details.FirstOrDefault(); Notify(nameof(SelectedDetail)); LoadDetailFields(); pending = pendingForms.ContainsKey(FormKey(false)); Refresh();
    }
    private void MotionFields(CharacterDefinition c)
    {
        var m = c.Motion; Field(Fields, "blink", "눈 깜빡임", m.BlinkEnabled, Booleans); Field(Fields, "min", "최소 간격 (초)", m.IntervalMin); Field(Fields, "max", "최대 간격 (초)", m.IntervalMax); Field(Fields, "blinkDuration", "감는 시간 (초)", m.BlinkDuration);
        Field(Fields, "idleMotion", "대기 움직임", m.IdleEnabled, Booleans); Field(Fields, "breathing", "숨쉬기", m.Breathing); Field(Fields, "sway", "좌우 흔들림", m.Sway); Field(Fields, "mouthWeight", "입 반응", m.Mouth); Field(Fields, "bodyBounce", "몸 이동", m.BodyBounce); Field(Fields, "headBounce", "머리 이동", m.HeadBounce); Field(Fields, "headRotation", "머리 회전", m.HeadRotation); Field(Fields, "mouthTravel", "입 이동", m.MouthTravel); Field(Fields, "bodyAnchor", "몸 앵커", m.BodyAnchor, c.Anchors.Select(a => a.Id)); Field(Fields, "headAnchor", "머리 앵커", m.HeadAnchor, c.Anchors.Select(a => a.Id)); Field(Fields, "viewport", "전체 출력 이동", m.ViewportMotion, Booleans);
    }
    private void LoadDetailFields()
    {
        loadingFields = true; DetailFields.Clear(); var c = Document?.Package.Character;
        if (c != null) switch (SelectedDetail?.Item)
            {
                case ActionTimelineEvent e:
                    Field(DetailFields, "time", "시간 (ms)", e.TimeMs); Field(DetailFields, "kind", "이벤트", e.Kind, Enum.GetNames<ActionEventKind>()); Field(DetailFields, "target", "대상 ID", e.Target, c.Anchors.Select(a => a.Id).Concat(c.Parts.Select(p => p.Id)).Append("eyes").Append("mouth")); Field(DetailFields, "value", "값 / PNG / 상태 ID", e.Value, asset: true); Field(DetailFields, "enabled", "표시 / 착용", e.Enabled, Booleans); Field(DetailFields, "duration", "효과 길이 (ms)", e.DurationMs); Field(DetailFields, "hasTransform", "변환 사용", e.Transform != null, Booleans); TransformFields(DetailFields, e.Transform ?? Transform2D.Identity); break;
                case TriggerDefinition t:
                    Field(DetailFields, "id", "트리거 ID", t.Id); Field(DetailFields, "kind", "종류", t.Kind, Enum.GetNames<TriggerKind>()); Field(DetailFields, "key", "키 / 수동 ID", t.Key); Field(DetailFields, "button", "마우스 버튼", t.Button, ["left", "right", "middle"]); Field(DetailFields, "modifiers", "조합 키", t.Modifiers.ToString()); Field(DetailFields, "threshold", "음량 경계 (0~1)", t.Threshold); Field(DetailFields, "interval", "간격 (초)", t.IntervalSeconds); Field(DetailFields, "immediate", "바로 시작", t.StartImmediately, Booleans); Field(DetailFields, "enabled", "사용", t.Enabled, Booleans); break;
                case AttachmentLayer l:
                    Field(DetailFields, "id", "레이어 ID", l.Id); Field(DetailFields, "asset", "PNG", l.Asset, asset: true); Field(DetailFields, "anchor", "앵커", l.Anchor, c.Anchors.Select(a => a.Id)); Field(DetailFields, "z", "Z-index", l.ZIndex); TransformFields(DetailFields, l.Transform);
                    Field(DetailFields, "space", "위치 기준", l.Placement.Space, Enum.GetNames<AttachmentSpace>()); Field(DetailFields, "follow", "영향도 (0~1)", l.Placement.Follow); Field(DetailFields, "width", "너비 (빈 값=Canvas)", l.Placement.Width); Field(DetailFields, "height", "높이 (빈 값=Canvas)", l.Placement.Height); Field(DetailFields, "deform", "소매 격자 변형", l.Placement.DeformWithRig, Booleans);
                    Field(DetailFields, "position", "위치 상속", l.Inherit.Position, Booleans); Field(DetailFields, "rotationInherit", "회전 상속", l.Inherit.Rotation, Booleans); Field(DetailFields, "scaleInherit", "배율 상속", l.Inherit.Scale, Booleans); Field(DetailFields, "mouthMotion", "입 움직임 상속", l.Inherit.MouthMotion, Booleans); Field(DetailFields, "during", "동작 중에만", l.OnlyDuringAction, Booleans); Field(DetailFields, "missing", "포즈 없으면 숨김", l.HideOnMissingPose, Booleans);
                    Field(DetailFields, "required", "필요 파츠 (쉼표)", string.Join(",", l.RequiredVisibleParts));
                    foreach (string pose in l.Poses.Keys.Concat(c.Poses.Select(p => p.Name)).Append("idle").Distinct()) Field(DetailFields, "pose:" + pose, "포즈 PNG · " + pose, l.Poses.GetValueOrDefault(pose), asset: true); break;
                case PoseDefinition pose:
                    Field(DetailFields, "id", "포즈 ID", pose.Id); Field(DetailFields, "name", "포즈 이름", pose.Name);
                    foreach (var part in c.Parts) Field(DetailFields, "sprite:" + part.Id, "교체 · " + part.Id, pose.Sprites.TryGetValue(part.Id, out var file) ? file ?? "숨김" : "", new[] { "", "숨김" }.Concat(Document!.Package.AssetPaths));
                    foreach (string id in c.Anchors.Select(a => a.Id).Concat(c.Parts.Select(p => p.Id)).Distinct()) Field(DetailFields, "transform:" + id, "변환 · " + id + " (X,Y,SX,SY,°)", pose.Transforms.TryGetValue(id, out var t) ? string.Join(",", new[] { t.X, t.Y, t.ScaleX, t.ScaleY, t.Rotation }.Select(n => n.ToString(CultureInfo.InvariantCulture))) : ""); break;
            }
        RestoreForm(DetailFields, true); loadingFields = false; detailPending = pendingForms.ContainsKey(FormKey(true)); Refresh();
    }
    private void RestoreForm(ObservableCollection<EditorField> fields, bool detailForm)
    {
        if (pendingForms.TryGetValue(FormKey(detailForm), out var values)) foreach (var f in fields) if (values.TryGetValue(f.Key, out var value)) f.Value = value;
    }
    private static Dictionary<string, string> Values(IEnumerable<EditorField> fields) => fields.ToDictionary(f => f.Key, f => f.Value);
    private static string S(Dictionary<string, string> v, string key) => v.GetValueOrDefault(key) ?? "";
    private static string? Opt(Dictionary<string, string> v, string key) => string.IsNullOrWhiteSpace(S(v, key)) ? null : S(v, key);
    private static double N(Dictionary<string, string> v, string key) => double.Parse(S(v, key), CultureInfo.InvariantCulture);
    private static int I(Dictionary<string, string> v, string key) => int.Parse(S(v, key), CultureInfo.InvariantCulture);
    private static bool B(Dictionary<string, string> v, string key) => bool.Parse(S(v, key));
    private static Transform2D T(Dictionary<string, string> v) => new(N(v, "x"), N(v, "y"), N(v, "sx"), N(v, "sy"), N(v, "rotation"));
    private static TEnum E<TEnum>(Dictionary<string, string> v, string key) where TEnum : struct, Enum => Enum.Parse<TEnum>(S(v, key));
    public async Task ApplyAsync()
    {
        if (Document == null) return; var doc = Document; var item = Selected?.Item; int selectedPage = Page; string? selectedBank = bankId; var v = Values(Fields);
        if (v.TryGetValue("id", out var newId) && Selected != null && newId != Selected.Id && pendingForms.Keys.Any(k => k != FormKey(false)))
        { Status = "ID 변경 전에 다른 미적용 입력을 적용하거나 취소하세요."; return; }
        await TransactionAsync(() => selectedPage == 0 ? doc.Metadata(S(v, "id"), S(v, "name"), S(v, "version"), S(v, "author"), S(v, "description"), Opt(v, "thumbnail"), N(v, "width"), N(v, "height")) : doc.Edit(d =>
        {
            switch (selectedPage)
            {
                case 1 when item is CharacterPart p: d.Parts[d.Parts.FindIndex(x => x.Id == p.Id)] = new(S(v, "id"), S(v, "type"), S(v, "asset"), Opt(v, "anchor"), I(v, "z"), T(v), Opt(v, "side"), B(v, "visible")); break;
                case 2 when item is AnchorDefinition a: d.Anchors[d.Anchors.FindIndex(x => x.Id == a.Id)] = new(S(v, "id"), parent: Opt(v, "parent"), transform: T(v)); break;
                case 3:
                    int index = item is int i ? i : 0;
                    if (d.Mouth is { } m) { var frames = m.Frames.ToArray(); var offsets = m.FrameTransforms.ToArray(); frames[index] = S(v, "asset"); offsets[index] = T(v); d.Mouth = new(frames, S(v, "anchor"), S(v, "curve"), offsets, I(v, "z")); }
                    if (d.SpriteSheet is { } bank) { var banks = bank.Expressions.ToArray(); int bi = Array.FindIndex(banks, e => e.Id == selectedBank); if (bi < 0) bi = 0; var a = banks[bi]; var open = a.Open.ToArray(); var closed = a.Closed.ToArray(); open[index] = S(v, "open"); closed[index] = S(v, "closed"); banks[bi] = new(a.Id, open, closed, B(v, "bankBlink")); d.SpriteSheet = new(S(v, "sheetPart"), banks, S(v, "curve")); }
                    d.Motion = new(B(v, "blink"), N(v, "min"), N(v, "max"), N(v, "blinkDuration"), B(v, "idleMotion"), N(v, "breathing"), N(v, "sway"), N(v, "mouthWeight"), N(v, "bodyBounce"), N(v, "headBounce"), N(v, "headRotation"), N(v, "mouthTravel"), S(v, "bodyAnchor"), S(v, "headAnchor"), B(v, "viewport")); break;
                case 4 when item is ExpressionDefinition e:
                    d.Expressions[d.Expressions.FindIndex(x => x.Id == e.Id)] = new(S(v, "id"), S(v, "open"), S(v, "closed"), Opt(v, "brows"), S(v, "name")); if (d.SpriteSheet is { } sheet) d.SpriteSheet = new(sheet.Part, sheet.Expressions.Select(b => b.Id == e.Id ? new SpriteSheetExpression(S(v, "id"), b.Open, b.Closed, b.Blink) : b), sheet.Curve); d.Face = new(S(v, "eyesAnchor"), S(v, "browsAnchor"), I(v, "eyesZ"), I(v, "browsZ"), Opt(v, "required")); break;
                case 5 when item is ActionDefinition a: d.Actions[d.Actions.FindIndex(x => x.Id == a.Id)] = new(S(v, "id"), S(v, "name"), N(v, "duration"), a.Timeline, I(v, "priority"), new(E<ActionPlaybackMode>(v, "mode"), B(v, "interrupt"), B(v, "idle")), Opt(v, "tag"), a.Triggers.Select(t => new TriggerDefinition(t.Id, S(v, "id"), t.Kind, t.Key, t.Button, t.Modifiers, t.Threshold, t.IntervalSeconds, t.StartImmediately, t.Enabled))); break;
                case 6 when item is AttachmentDefinition a: d.Attachments[d.Attachments.FindIndex(x => x.Id == a.Id)] = new(S(v, "id"), S(v, "name"), S(v, "category"), a.Layers, Opt(v, "tag"), Opt(v, "group")); break;
            }
        }));
    }
    public async Task ApplyDetailAsync()
    {
        if (Document == null) return; var doc = Document; var owner = Selected?.Item; var item = SelectedDetail?.Item; var v = Values(DetailFields);
        await TransactionAsync(() => doc.Edit(d =>
        {
            if (owner is ActionDefinition a)
            {
                var events = a.Timeline.ToList(); var triggers = a.Triggers.ToList();
                if (item is ActionTimelineEvent evt)
                {
                    var kind = E<ActionEventKind>(v, "kind");
                    events[events.IndexOf(evt)] = new(N(v, "time"), kind,
                        kind is ActionEventKind.Transform or ActionEventKind.Visibility or ActionEventKind.Sprite or ActionEventKind.Effect ? Opt(v, "target") : null,
                        kind is ActionEventKind.Transform or ActionEventKind.Visibility ? null : Opt(v, "value"),
                        kind is ActionEventKind.Transform or ActionEventKind.Effect && B(v, "hasTransform") ? T(v) : null, B(v, "enabled"), kind == ActionEventKind.Effect ? N(v, "duration") : 0);
                }
                if (item is TriggerDefinition tr) triggers[triggers.IndexOf(tr)] = new(S(v, "id"), a.Id, E<TriggerKind>(v, "kind"), Opt(v, "key"), Opt(v, "button"), Enum.Parse<TriggerModifiers>(S(v, "modifiers")), N(v, "threshold"), N(v, "interval"), B(v, "immediate"), B(v, "enabled"));
                d.Actions[d.Actions.FindIndex(x => x.Id == a.Id)] = new(a.Id, a.Name, a.DurationMs, events, a.Priority, a.Playback, a.CompatibilityTag, triggers);
            }
            if (owner is AttachmentDefinition attachment && item is AttachmentLayer l)
            {
                var layers = attachment.Layers.ToArray(); layers[Array.IndexOf(layers, l)] = new(S(v, "id"), S(v, "asset"), S(v, "anchor"), I(v, "z"), inherit: new(B(v, "position"), B(v, "rotationInherit"), B(v, "scaleInherit"), B(v, "mouthMotion")), poses: v.Where(p => p.Key.StartsWith("pose:") && !string.IsNullOrWhiteSpace(p.Value)).ToDictionary(p => p.Key[5..], p => p.Value), requiredVisibleParts: S(v, "required").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), onlyDuringAction: B(v, "during"), hideOnMissingPose: B(v, "missing"),
                    placement: new(T(v), E<AttachmentSpace>(v, "space"), N(v, "follow"), Opt(v, "width") == null ? null : N(v, "width"), Opt(v, "height") == null ? null : N(v, "height"), B(v, "deform")));
                d.Attachments[d.Attachments.FindIndex(x => x.Id == attachment.Id)] = new(attachment.Id, attachment.Name, attachment.Category, layers, attachment.CompatibilityTag, attachment.ExclusiveGroup);
            }
            if (item is PoseDefinition pose)
            {
                Transform2D ParseTransform(string text) { var n = text.Split(',').Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray(); if (n.Length != 5) throw new ArgumentException("포즈 변환은 X,Y,SX,SY,각도5개입니다."); return new(n[0], n[1], n[2], n[3], n[4]); }
                var sprites = v.Where(p => p.Key.StartsWith("sprite:") && !string.IsNullOrWhiteSpace(p.Value)).ToDictionary(p => p.Key[7..], p => p.Value == "숨김" ? null : p.Value);
                var transforms = v.Where(p => p.Key.StartsWith("transform:") && !string.IsNullOrWhiteSpace(p.Value)).ToDictionary(p => p.Key[10..], p => ParseTransform(p.Value));
                d.Poses[d.Poses.FindIndex(p => p.Id == pose.Id)] = new(S(v, "id"), S(v, "name"), sprites, transforms);
            }
        }), detailApply: true);
    }
    public async Task AddAsync()
    {
        if (!CanChangeStructure()) return;
        if (Document == null) return; var doc = Document; int p = Page; string id = "item-" + Guid.NewGuid().ToString("N")[..8];
        await TransactionAsync(() => doc.Edit(d =>
        {
            string asset = doc.Package.AssetPaths.First();
            switch (p)
            {
                case 1: d.Parts.Add(new(id, "custom", asset, d.Anchors.FirstOrDefault()?.Id)); break;
                case 2: d.Anchors.Add(new(id, d.Width / 2, d.Height / 2)); break;
                case 3:
                    if (d.Mouth is { } m) { if (m.Frames.Count >= 16) throw new ArgumentException("입은16프레임까지입니다."); d.Mouth = new(m.Frames.Append(m.Frames[^1]), m.Anchor, "smooth", m.FrameTransforms.Append(Transform2D.Identity), m.ZIndex); }
                    else if (d.SpriteSheet is { } bank) { if (bank.Selector.Frames.Count >= 16) throw new ArgumentException("입은16프레임까지입니다."); d.SpriteSheet = new(bank.Part, bank.Expressions.Select(e => new SpriteSheetExpression(e.Id, e.Open.Append(e.Open[^1]), e.Closed.Append(e.Closed[^1]), e.Blink)), "smooth"); }
                    else d.Mouth = new([asset], d.Anchors.FirstOrDefault(a => a.Id == "mouth")?.Id ?? d.Anchors.First().Id); break;
                case 4: d.Expressions.Add(new(id, asset, asset, name: "새 표정")); if (d.SpriteSheet is { } sheet) { var bank = sheet.Expressions[0]; d.SpriteSheet = new(sheet.Part, sheet.Expressions.Append(new SpriteSheetExpression(id, bank.Open, bank.Closed, bank.Blink)), sheet.Curve); } break;
                case 5: d.Actions.Add(new(id, "새 동작", 1000, [])); break;
                case 6: d.Attachments.Add(new(id, "새 부착물", "hat", [new("image", asset, d.Anchors.First().Id)])); break;
            }
        }));
    }
    public async Task DeleteAsync()
    {
        if (Document == null || Selected == null) return; var doc = Document; var item = Selected.Item; int p = Page;
        await TransactionAsync(() => doc.Edit(d =>
        {
            switch (item)
            {
                case CharacterPart part: d.Parts.RemoveAll(x => x.Id == part.Id); break;
                case AnchorDefinition a: d.Anchors.RemoveAll(x => x.Id == a.Id); break;
                case ExpressionDefinition e: d.Expressions.RemoveAll(x => x.Id == e.Id); if (d.SpriteSheet is { } sheet) d.SpriteSheet = new(sheet.Part, sheet.Expressions.Where(b => b.Id != e.Id), sheet.Curve); break;
                case ActionDefinition a: d.Actions.RemoveAll(x => x.Id == a.Id); break;
                case AttachmentDefinition a: d.Attachments.RemoveAll(x => x.Id == a.Id); break;
                case int i when p == 3:
                    if (d.Mouth is { } m) d.Mouth = m.Frames.Count == 1 ? null : new(m.Frames.Where((_, n) => n != i), m.Anchor, "smooth", m.FrameTransforms.Where((_, n) => n != i), m.ZIndex);
                    if (d.SpriteSheet is { } bank) { if (bank.Selector.Frames.Count == 1) throw new ArgumentException("시트의 마지막 프레임은 유지하세요."); d.SpriteSheet = new(bank.Part, bank.Expressions.Select(e => new SpriteSheetExpression(e.Id, e.Open.Where((_, n) => n != i), e.Closed.Where((_, n) => n != i), e.Blink)), "smooth"); }
                    break;
            }
        }));
    }
    public async Task AddPoseAsync()
    {
        if (Document == null) return; var doc = Document; string id = "pose-" + Guid.NewGuid().ToString("N")[..8];
        if (await TransactionAsync(() => doc.Edit(d => d.Poses.Add(new(id))), preserveForms: true)) SelectedDetail = Details.FirstOrDefault(r => r.Item is PoseDefinition p && p.Id == id);
    }
    private bool CanChangeStructure()
    {
        if (pendingForms.Count == 0) return CanEdit;
        Status = "추가·삭제 전에 미적용 입력을 적용하거나 취소하세요."; return false;
    }
    public bool AddTriggerNext { get; set; }
    public async Task AddDetailAsync()
    {
        if (!CanChangeStructure()) return;
        if (Document == null) return; var doc = Document; var item = Selected?.Item; bool trigger = AddTriggerNext;
        await TransactionAsync(() => doc.Edit(d =>
        {
            if (item is ActionDefinition a)
            {
                var triggers = trigger ? a.Triggers.Append(new TriggerDefinition("trigger-" + Guid.NewGuid().ToString("N")[..8], a.Id, TriggerKind.Manual, a.Id)) : a.Triggers;
                var events = trigger ? a.Timeline : a.Timeline.Append(new ActionTimelineEvent(0, ActionEventKind.Transform, d.Anchors.First().Id, transform: Transform2D.Identity));
                d.Actions[d.Actions.FindIndex(x => x.Id == a.Id)] = new(a.Id, a.Name, a.DurationMs, events, a.Priority, a.Playback, a.CompatibilityTag, triggers);
            }
            if (item is AttachmentDefinition outfit) d.Attachments[d.Attachments.FindIndex(x => x.Id == outfit.Id)] = new(outfit.Id, outfit.Name, outfit.Category, outfit.Layers.Append(new("layer-" + Guid.NewGuid().ToString("N")[..8], doc.Package.AssetPaths.First(), d.Anchors.First().Id)), outfit.CompatibilityTag, outfit.ExclusiveGroup);
        }));
    }
    public async Task DeleteDetailAsync()
    {
        if (!CanChangeStructure()) return;
        if (Document == null) return; var doc = Document; var owner = Selected?.Item; var item = SelectedDetail?.Item;
        await TransactionAsync(() => doc.Edit(d =>
        {
            if (item is PoseDefinition pose) d.Poses.RemoveAll(p => p.Id == pose.Id);
            if (owner is ActionDefinition a) d.Actions[d.Actions.FindIndex(x => x.Id == a.Id)] = new(a.Id, a.Name, a.DurationMs, a.Timeline.Where(e => !ReferenceEquals(e, item)), a.Priority, a.Playback, a.CompatibilityTag, a.Triggers.Where(t => !ReferenceEquals(t, item)));
            if (owner is AttachmentDefinition outfit) d.Attachments[d.Attachments.FindIndex(x => x.Id == outfit.Id)] = new(outfit.Id, outfit.Name, outfit.Category, outfit.Layers.Where(l => !ReferenceEquals(l, item)), outfit.CompatibilityTag, outfit.ExclusiveGroup);
        }));
    }
    public async Task<bool> ReplaceImageAsync(string path, string? fieldKey = null, bool detailField = false)
    {
        if (Document == null) return false;
        var doc = Document; string asset = "images/" + Guid.NewGuid().ToString("N") + ".png";
        var savedFields = Values(Fields); var savedDetailFields = Values(DetailFields); string? detailId = SelectedDetail?.Id;
        bool wasPending = pending, wasDetailPending = detailPending;
        bool ok = await TransactionAsync(() =>
        {
            using var file = File.OpenRead(path);
            if (file.Length > 16 * 1024 * 1024) throw new ArgumentException("PNG는16MB 이하입니다.");
            var bytes = new byte[checked((int)file.Length)]; file.ReadExactly(bytes);
            return doc.Edit(_ => { }, addedAssets: new Dictionary<string, byte[]> { { asset, bytes } });
        }, preserveForms: true);
        if (ok)
        {
            SelectedDetail = Details.FirstOrDefault(d => d.Id == detailId) ?? Details.FirstOrDefault();
            loadingFields = true;
            foreach (var field in Fields) if (savedFields.TryGetValue(field.Key, out var value)) field.Value = value;
            foreach (var field in DetailFields) if (savedDetailFields.TryGetValue(field.Key, out var value)) field.Value = value;
            loadingFields = false; pending = wasPending; detailPending = wasDetailPending;
            var fields = detailField ? DetailFields : Fields;
            var selectedField = fields.FirstOrDefault(f => f.Key == fieldKey) ?? fields.FirstOrDefault(f => f.IsAsset);
            if (selectedField != null) selectedField.Value = asset;
            Status = "PNG를 준비했습니다. 기존 입력을 유지하며, 적용을 누르면 선택 항목에 연결됩니다."; Refresh();
        }
        return ok;
    }
    public async Task MoveAnchorAsync(string id, double canvasX, double canvasY)
    {
        if (Document == null || busy) return; var doc = Document;
        await TransactionAsync(() => doc.Edit(d =>
        {
            int i = d.Anchors.FindIndex(a => a.Id == id); if (i < 0) return; var a = d.Anchors[i]; var solver = new RigMotionSolver(new RigDefinition(d.Anchors));
            var p = a.Parent == null ? new RigPoint(canvasX, canvasY) : solver.Static[a.Parent].Inverse().Map(canvasX, canvasY);
            d.Anchors[i] = new(a.Id, parent: a.Parent, transform: new(p.X, p.Y, a.Transform.ScaleX, a.Transform.ScaleY, a.Transform.Rotation));
        }));
    }
    public async Task ValidateAsync()
    {
        if (Document == null || !CanExport) return;
        var document = Document; busy = true; Refresh(); await worker.WaitAsync();
        try { await Task.Run(() => new VoxPetPackageLoader().Load(new MemoryStream(document.Export()))); Status = "검증 통과 · 패키지 재읽기와 모든 PNG 참조를 확인했습니다."; }
        catch (Exception e) when (Expected(e)) { Status = "검증 실패 · " + e.Message; }
        finally { worker.Release(); busy = false; Refresh(); }
    }
    public async Task<bool> ExportAsync(string path)
    {
        if (Document == null || !CanExport) return false; busy = true; Refresh(); var package = Document.Package; await worker.WaitAsync();
        try { await Task.Run(() => new VoxPetPackageExporter().Export(package, path)); Status = ".voxpet 내보내기 완료 · " + Path.GetFileName(path); return true; }
        catch (Exception e) when (Expected(e)) { Status = "내보내기 실패 · " + e.Message; return false; }
        finally { worker.Release(); busy = false; Refresh(); }
    }
    private void ResetPreview()
    {
        if (Document == null || Scene == null) return; var old = Scene; Scene = new(Document.Package.CreateRuntime(new Random(1)), old.Images); time = 0;
        if (PreviewExpression != null && Expressions.Any(e => e.Id == PreviewExpression)) Scene.Runtime.SetExpression(PreviewExpression);
        foreach (string id in old.Runtime.Attachments.EnabledIds) Scene.Runtime.Attachments.SetEnabled(id, true);
    }
    public void Seek(double milliseconds)
    {
        if (!CanEdit || Scene == null || Document == null) return; playing = false; ResetPreview(); string? action = Selected?.Item is ActionDefinition a ? a.Id : PreviewAction;
        if (action != null) { Scene!.Runtime.RequestAction(action); Scene.Update(0, Voice); }
        time = Math.Max(0, milliseconds) / 1000; UpdateFrame();
    }
    private void UpdateFrame() { if (Scene == null) return; Scene.Update(time, Voice); Notify(nameof(Scene)); Notify(nameof(PreviewState)); }
    private void Tick(object? sender, EventArgs e)
    { double now = clock.Elapsed.TotalSeconds, dt = now - previous; previous = now; if (playing) { time += dt; Set(ref scrub, Math.Min(Duration, time * 1000), nameof(Scrub)); } UpdateFrame(); }
    public void StartPreview() { previous = clock.Elapsed.TotalSeconds; timer.Start(); }
    public void PausePreview() { timer.Stop(); }
    public async Task CloseAsync() { closing = true; revision++; timer.Stop(); timer.Tick -= Tick; Scene?.Stop(); Refresh(); await worker.WaitAsync(); worker.Release(); }
}
