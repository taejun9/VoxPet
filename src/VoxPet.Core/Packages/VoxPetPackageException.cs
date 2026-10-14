namespace VoxPet.Core.Packages;

public sealed class VoxPetPackageException : Exception
{
    public VoxPetPackageException(string message, Exception? inner = null) : base(message, inner) { }
}
