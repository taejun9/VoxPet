namespace VoxPet.Core.Packages;

public sealed record VoxPetPackageLimits(long ArchiveBytes = 64L * 1024 * 1024, long TotalBytes = 64L * 1024 * 1024,
    int AssetBytes = 16 * 1024 * 1024, int JsonBytes = 512 * 1024, int Entries = 512, int PngDimension = 4096,
    long PngPixels = 8_000_000, long DecodedBytes = 128L * 1024 * 1024)
{
    public void Validate()
    {
        if (ArchiveBytes is < 22 or > int.MaxValue || TotalBytes is < 1 or > int.MaxValue || AssetBytes < 1 || JsonBytes < 1 || Entries is < 1 or > 65534 ||
            PngDimension < 1 || PngPixels < 1 || DecodedBytes < 1) throw new ArgumentException("패키지 상한을 확인하세요.");
    }
}
