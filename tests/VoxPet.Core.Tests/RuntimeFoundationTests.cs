using VoxPet.Core.Character;
using VoxPet.Core.Packages;
using VoxPet.Core.Rendering;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class RuntimeFoundationTests
{
    private static CharacterPart Body(string? anchor = null) => new("body", "body", "parts/body.png", anchor, 100);
    private static VoxPetManifest Manifest() => new("com.herbhub.neulbo", "늘보군", "1.0.0");

    public static IEnumerable<object[]> NonFiniteTransformFields()
    {
        foreach (int field in Enumerable.Range(0, 5))
            foreach (double value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                yield return [field, value];
    }

    [Theory]
    [MemberData(nameof(NonFiniteTransformFields))]
    public void EveryTransformFieldRejectsNonFiniteValues(int field, double value)
    {
        double[] values = [0, 0, 1, 1, 0]; values[field] = value;
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transform2D(values[0], values[1], values[2], values[3], values[4]));
    }

    [Fact]
    public void TransformDefaultsAndReflectionAreExplicit()
    {
        Assert.Equal(new Transform2D(0, 0, 1, 1, 0), Transform2D.Identity);
        var mirrored = new Transform2D(-20, 35, -1, 2, -90);
        Assert.Equal(-1, mirrored.ScaleX); Assert.Equal(-90, mirrored.Rotation);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transform2D(scaleX: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Transform2D(scaleY: 0));
    }

    [Fact]
    public void DefinitionSupportsDefaultAndArbitraryNonSquareCanvasesAndCustomParts()
    {
        var defaults = new CharacterDefinition([Body()]);
        Assert.Equal(2, defaults.SchemaVersion); Assert.Equal(512, defaults.CanvasWidth); Assert.Equal(512, defaults.CanvasHeight);
        var tail = new CharacterPart("tail-01", "tail", "parts/tail.png", "tail", -10, new(4, 2), "left");
        var wide = new CharacterDefinition([Body("body"), tail],
            [new("root"), new("body", 500, 300, "root"), new("tail", 30, 20, "body")], 1200, 675);
        Assert.Equal(1200, wide.CanvasWidth); Assert.Equal(675, wide.CanvasHeight);
        Assert.Equal("tail", wide.Parts[1].Type); Assert.Equal("left", wide.Parts[1].Side);
        Assert.Equal("body", wide.Anchors[2].Parent); Assert.Equal(30, wide.Anchors[2].X);
        Assert.Equal(20, wide.Anchors[2].Y); Assert.Equal(-10, wide.Parts[1].ZIndex);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    public void BothCanvasDimensionsMustBeFiniteAndPositive(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterDefinition([Body()], canvasWidth: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterDefinition([Body()], canvasHeight: value));
    }

    [Fact]
    public void DefinitionRejectsMissingAndDuplicateReferences()
    {
        Assert.Throws<ArgumentNullException>(() => new CharacterDefinition(null!));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([null!]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([Body(), Body()]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([Body()], [null!]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([Body()], [new("head"), new("head")]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([Body("absent")]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([Body()], [new("head", parent: "absent")]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterDefinition([Body()], schemaVersion: 1));
        // ID 비교는 ordinal이며 철자·대소문자 차이를 별칭으로 보정하지 않는다.
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([Body("HEAD")], [new("head")]));
    }

    [Fact]
    public void CyclesAreDetectedRegardlessOfOrderOrConnectionToRoot()
    {
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([Body()], [new("head", parent: "head")]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([Body()],
            [new("mouth", parent: "face"), new("root"), new("head", parent: "mouth"), new("face", parent: "head")]));
        var valid = new CharacterDefinition([Body("body")], [new("mouth", parent: "head"), new("head", parent: "body"), new("body")]);
        Assert.Equal(3, valid.Anchors.Count);
    }

    [Fact]
    public void DeepAnchorTreesDoNotUseRecursiveValidation()
    {
        var anchors = Enumerable.Range(0, 20000).Select(i => new AnchorDefinition("anchor-" + i, parent: i == 19999 ? null : "anchor-" + (i + 1)));
        var definition = new CharacterDefinition([Body("anchor-0")], anchors);
        Assert.Equal(20000, definition.Anchors.Count);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AnchorPositionsRejectNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnchorDefinition("head", x: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnchorDefinition("head", y: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AnchorDefinition("head", x: value, transform: Transform2D.Identity));
    }

    [Fact]
    public void AnchorsPreserveTransformsAndRejectAmbiguousPositions()
    {
        var transform = new Transform2D(10, 20, 2, 3, 45);
        var anchor = new AnchorDefinition("head", parent: "body", transform: transform);
        Assert.Same(transform, anchor.Transform); Assert.Equal(10, anchor.X); Assert.Equal(20, anchor.Y);
        Assert.Throws<ArgumentException>(() => new AnchorDefinition("head", x: 1, transform: transform));
        Assert.Throws<ArgumentException>(() => new AnchorDefinition("head", y: 1, transform: transform));
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad id")]
    [InlineData("bad/id")]
    [InlineData("bad\n")]
    [InlineData(null)]
    public void StableIdsAreValidatedAtEveryModelBoundary(string? id)
    {
        Assert.Throws<ArgumentException>(() => new CharacterPart(id!, "body", "body.png"));
        Assert.Throws<ArgumentException>(() => new CharacterPart("body", id!, "body.png"));
        Assert.Throws<ArgumentException>(() => new AnchorDefinition(id!));
        Assert.Throws<ArgumentException>(() => new CharacterState(expressionId: id!));
        Assert.Throws<ArgumentException>(() => new CharacterState(attachmentIds: [id!]));
        Assert.Throws<ArgumentException>(() => new RenderLayer(id!, "body.png"));
        Assert.Throws<ArgumentException>(() => new VoxPetManifest(id!, "이름", "1"));
        if (id is not null)
        {
            Assert.Throws<ArgumentException>(() => new AnchorDefinition("head", parent: id));
            Assert.Throws<ArgumentException>(() => new CharacterPart("body", "body", "body.png", anchor: id));
            Assert.Throws<ArgumentException>(() => new CharacterPart("body", "body", "body.png", side: id));
            Assert.Throws<ArgumentException>(() => new CharacterState(actionId: id));
            Assert.Throws<ArgumentException>(() => new CharacterPose(transforms: new Dictionary<string, Transform2D> { [id] = new() }));
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-.01)]
    [InlineData(1.01)]
    public void StatePoseAndOpacityRejectInvalidUnitValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterState(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterPose(mouthOpen: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CharacterPose(eyeOpen: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RenderLayer("body", "body.png", opacity: value));
    }

    [Fact]
    public void StateAndPosePreserveIndependentSelectionsAndMotion()
    {
        var idle = new CharacterState(); var pose = new CharacterPose();
        Assert.Equal(0, idle.VoiceLevel); Assert.Equal("neutral", idle.ExpressionId); Assert.Null(idle.ActionId); Assert.Empty(idle.AttachmentIds);
        Assert.Equal(0, pose.MouthOpen); Assert.Equal(1, pose.EyeOpen); Assert.Empty(pose.Transforms);
        var selected = new CharacterState(1, "happy", "wave", ["hat", "mustache"]);
        Assert.Equal(1, selected.VoiceLevel); Assert.Equal("happy", selected.ExpressionId); Assert.Equal("wave", selected.ActionId);
        var moving = new CharacterPose(1, 0, new Dictionary<string, Transform2D> { ["head"] = new(2, -5, rotation: 4) });
        Assert.Equal(1, moving.MouthOpen); Assert.Equal(0, moving.EyeOpen); Assert.Equal(4, moving.Transforms["head"].Rotation);
        Assert.Throws<ArgumentException>(() => new CharacterState(attachmentIds: ["hat", "hat"]));
        Assert.Throws<ArgumentNullException>(() => new CharacterPose(transforms: new Dictionary<string, Transform2D> { ["head"] = null! }));
    }

    [Fact]
    public void RenderingPreservesEveryFieldAndStablePainterOrder()
    {
        var transform = new Transform2D(2, -5, -1, 1.5, 4);
        var foreground = new RenderLayer("mouth", "face/mouth.png", transform, .5, 320);
        var body = new RenderLayer("body", "parts/body.png", zIndex: 100);
        var sleeve = new RenderLayer("sleeve", "parts/sleeve.png", zIndex: 100);
        var frame = new RenderFrame([foreground, body, sleeve]);
        Assert.Equal(new[] { "body", "sleeve", "mouth" }, frame.Layers.Select(layer => layer.Id));
        Assert.Equal("face/mouth.png", foreground.Asset); Assert.Equal(2, foreground.X); Assert.Equal(-5, foreground.Y);
        Assert.Equal(-1, foreground.ScaleX); Assert.Equal(1.5, foreground.ScaleY); Assert.Equal(4, foreground.Rotation);
        Assert.Equal(.5, foreground.Opacity); Assert.Equal(320, foreground.ZIndex);
        Assert.Empty(new RenderFrame([]).Layers);
        Assert.Equal(0, new RenderLayer("hidden", "body.png", opacity: 0).Opacity);
        Assert.Throws<ArgumentNullException>(() => new RenderFrame(null!));
        Assert.Throws<ArgumentException>(() => new RenderFrame([null!]));
        Assert.Throws<ArgumentException>(() => new RenderFrame([body, body]));
    }

    [Fact]
    public void InputCollectionsCannotMutateExistingSnapshots()
    {
        CharacterPart[] parts = [Body()]; AnchorDefinition[] anchors = [new("head")];
        var definition = new CharacterDefinition(parts, anchors);
        parts[0] = new("tail", "tail", "tail.png"); anchors[0] = new("root");
        Assert.Equal("body", definition.Parts[0].Id); Assert.Equal("head", definition.Anchors[0].Id);
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterPart>)definition.Parts)[0] = parts[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<AnchorDefinition>)definition.Anchors).Clear());

        var transforms = new Dictionary<string, Transform2D> { ["head"] = new(2, 3) };
        var pose = new CharacterPose(transforms: transforms); transforms["head"] = new(99, 99);
        Assert.Equal(2, pose.Transforms["head"].X);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, Transform2D>)pose.Transforms).Clear());

        string[] attachments = ["hat"]; var state = new CharacterState(attachmentIds: attachments); attachments[0] = "cape";
        Assert.Equal("hat", state.AttachmentIds[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)state.AttachmentIds).Clear());

        RenderLayer[] layers = [new("body", "body.png")]; var frame = new RenderFrame(layers); layers[0] = new("head", "head.png");
        Assert.Equal("body", frame.Layers[0].Id);
        Assert.Throws<NotSupportedException>(() => ((IList<RenderLayer>)frame.Layers).Clear());

        string[] actions = ["actions/wave.json"]; string[] accessoryFiles = ["attachments/hat.json"];
        var manifest = new VoxPetManifest("test", "시험", "1", actions: actions, attachments: accessoryFiles);
        actions[0] = "actions/gun.json"; accessoryFiles[0] = "attachments/cape.json";
        Assert.Equal("actions/wave.json", manifest.Actions[0]); Assert.Equal("attachments/hat.json", manifest.Attachments[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)manifest.Actions).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)manifest.Attachments).Clear());
    }

    [Fact]
    public void ManifestSeparatesCharacterVersionAndOptionalDocuments()
    {
        var manifest = Manifest();
        Assert.Equal("voxpet", manifest.Format); Assert.Equal(2, manifest.FormatVersion); Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal("com.herbhub.neulbo", manifest.Id); Assert.Equal("늘보군", manifest.Name);
        Assert.Equal("character.json", manifest.Character); Assert.Null(manifest.Thumbnail);
        Assert.Empty(manifest.Actions); Assert.Empty(manifest.Attachments);
        var populated = new VoxPetManifest("test", "캐릭터", "2026.10", "HerbHub", "설명", "data/character.json", "thumbnail.png",
            ["actions/wave.json"], ["attachments/hat.json"]);
        Assert.Equal(2, populated.FormatVersion); Assert.Equal("2026.10", populated.Version);
        Assert.Equal("HerbHub", populated.Author); Assert.Equal("설명", populated.Description);
        Assert.Equal("thumbnail.png", populated.Thumbnail); Assert.Equal("data/character.json", populated.Character);
        Assert.Throws<ArgumentOutOfRangeException>(() => new VoxPetManifest("test", "이름", "1", formatVersion: 1));
        Assert.Throws<ArgumentException>(() => new VoxPetManifest("test", "", "1"));
        Assert.Throws<ArgumentException>(() => new VoxPetManifest("test", "이름", ""));
        Assert.Throws<ArgumentNullException>(() => new VoxPetManifest("test", "이름", "1", author: null!));
        Assert.Throws<ArgumentNullException>(() => new VoxPetManifest("test", "이름", "1", description: null!));
        Assert.Throws<ArgumentException>(() => new VoxPetManifest("test", "이름", "1", actions: ["wave.json", "WAVE.json"]));
        Assert.Throws<ArgumentException>(() => new VoxPetManifest("test", "이름", "1", attachments: [null!]));
    }

    [Theory]
    [InlineData("../body.png")]
    [InlineData("/body.png")]
    [InlineData("C:/body.png")]
    [InlineData("https://example/body.png")]
    [InlineData("parts\\body.png")]
    [InlineData("parts//body.png")]
    [InlineData("parts/./body.png")]
    [InlineData("body.png\n")]
    [InlineData("body.jpg")]
    [InlineData("")]
    [InlineData(null)]
    public void PngReferencesStayRelative(string? asset)
    {
        Assert.Throws<ArgumentException>(() => new CharacterPart("body", "body", asset!));
        Assert.Throws<ArgumentException>(() => new RenderLayer("body", asset!));
        // thumbnail=null은 생략을 뜻한다.
        if (asset is not null) Assert.Throws<ArgumentException>(() => new VoxPetManifest("test", "시험", "1", thumbnail: asset));
    }

    [Theory]
    [InlineData("../character.json")]
    [InlineData("/character.json")]
    [InlineData("C:/character.json")]
    [InlineData("actions\\wave.json")]
    [InlineData("actions//wave.json")]
    [InlineData("actions/./wave.json")]
    [InlineData("wave.png")]
    [InlineData(null)]
    public void ManifestDocumentReferencesStayRelative(string? path)
    {
        Assert.Throws<ArgumentException>(() => new VoxPetManifest("test", "시험", "1", character: path!));
        Assert.Throws<ArgumentException>(() => new VoxPetManifest("test", "시험", "1", actions: [path!]));
        Assert.Throws<ArgumentException>(() => new VoxPetManifest("test", "시험", "1", attachments: [path!]));
    }
}
