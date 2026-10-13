using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

public sealed record LoadedImageSet(ImageSet Definition, IReadOnlyDictionary<string, BitmapSource> Images, IReadOnlyDictionary<string, BitmapSource> Previews);

/// <summary>worker에서 제한된 로컬 PNG만 읽는다. 심볼릭 링크·경로 탈출·과대 이미지/JSON을 거부한다.</summary>
public static class ImageSetLoader
{
    public static LoadedImageSet Load(string manifest)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(manifest))!;
        OrdinaryFile(manifest, folder);
        if (new FileInfo(manifest).Length > 256 * 1024) throw new ArgumentException("이미지 세트 JSON은256KiB 이내입니다.");
        var definition = JsonSerializer.Deserialize<ImageSet>(File.ReadAllText(manifest), ImageSet.JsonOptions) ?? throw new ArgumentException("이미지 세트가 비어 있습니다.");
        definition.Validate();
        var images = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase); long compressed = 0;
        if ((long)definition.Files.Count() * definition.CanvasSize * definition.CanvasSize * 4 > 128 * 1024 * 1024) throw new ArgumentException("디코딩 이미지 합계는128MiB 이내입니다.");
        Span<byte> header = stackalloc byte[26];
        foreach (string name in definition.Files)
        {
            string path = Path.Combine(folder, name.Replace('/', Path.DirectorySeparatorChar)); OrdinaryFile(path, folder);
            using var file = File.OpenRead(path); compressed += file.Length;
            if (compressed > 32 * 1024 * 1024) throw new ArgumentException("PNG 파일 합계는32MiB 이내입니다.");
            file.ReadExactly(header);
            if (!header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) || header[25] != 6 ||
                System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[16..20]) != definition.CanvasSize ||
                System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[20..24]) != definition.CanvasSize)
                throw new ArgumentException($"{name}: 모든 PNG는 동일한 RGBA Canvas여야 합니다.");
            file.Position = 0;
            var decoder = new PngBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var bitmap = decoder.Frames[0]; bitmap.Freeze(); images.Add(name, bitmap);
        }
        var previews = definition.Weapons.Select(w => w.Layers.Single(l => l.Part == ImagePart.Weapon).Poses["idle"]).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(name => name, name => Preview(images[name]), StringComparer.OrdinalIgnoreCase);
        return new(definition, images, previews);
    }
    // 관리 썸네일만 투명 여백을 제거한다. 실제 출력은 원본 Canvas/방향을 그대로 사용한다.
    private static BitmapSource Preview(BitmapSource source)
    {
        var rgba = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int width = source.PixelWidth, height = source.PixelHeight;
        byte[] pixels = new byte[width * height * 4]; rgba.CopyPixels(pixels, width * 4, 0);
        int left = width, top = height, right = -1, bottom = -1;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            if (pixels[(y * width + x) * 4 + 3] == 0) continue;
            left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y);
        }
        if (right < left) return source;
        var crop = new CroppedBitmap(source, new Int32Rect(left, top, right - left + 1, bottom - top + 1)); crop.Freeze(); return crop;
    }
    internal static void OrdinaryFile(string file, string root)
    {
        string path = Path.GetFullPath(file), folder = Path.GetFullPath(root);
        if (!path.StartsWith(folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("세트 밖의 파일은 읽지 않습니다.");
        for (string? current = path; current != null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("링크 대신 실제 PNG 폴더를 선택하세요.");
    }
    public static string Import(string manifest, string library, string characterId)
    {
        var loaded = Load(manifest);
        if (loaded.Definition.CharacterId != characterId) throw new ArgumentException("세트의 CharacterId가 선택한 캐릭터와 다릅니다.");
        string id = Guid.NewGuid().ToString("N"), target = Path.Combine(library, id);
        Directory.CreateDirectory(target);
        try
        {
            string source = Path.GetDirectoryName(Path.GetFullPath(manifest))!;
            foreach (string name in loaded.Definition.Files)
            {
                string original = Path.Combine(source, name); OrdinaryFile(original, source);
                string copy = Path.Combine(target, name); Directory.CreateDirectory(Path.GetDirectoryName(copy)!); File.Copy(original, copy);
            }
            File.WriteAllText(Path.Combine(target, "imageset.json"), JsonSerializer.Serialize(loaded.Definition, ImageSet.JsonOptions));
            Load(Path.Combine(target, "imageset.json")); return id;
        }
        catch { Directory.Delete(target, true); throw; }
    }
}
