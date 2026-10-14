namespace VoxPet.Core.Attachments;

public sealed record AttachmentInheritance(bool Position = true, bool Rotation = true, bool Scale = true, bool MouthMotion = false);
