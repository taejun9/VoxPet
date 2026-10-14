using System.Windows.Media;

namespace VoxPet.App.Services;

/// <summary>기존 PNG 시트 경계. 디코딩/잘라내기는 worker에서 끝내며 원본을 수정하지 않는다.</summary>
public static class LegacyCharacterImporter
{
    public static ImageSource[,] Load(string path) => CharacterSheetLoader.Load(path);
    public static ImageSource[,] Load(byte[] bytes) => CharacterSheetLoader.Load(bytes);
}
