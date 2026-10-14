using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;
using VoxPet.Core.Character;
using VoxPet.Core.Models;

namespace VoxPet.Core.Legacy;

public sealed class LegacyImageSetSource : ICharacterDefinitionSource
{
    private readonly CharacterDefinition definition;
    public LegacyImageSetSource(ImageSet source) => definition = LegacyImageSetAdapter.Convert(source);
    public CharacterDefinition Load() => definition;
}
