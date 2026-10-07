namespace VoxPet.Core.Models;

/// <summary>명시적으로 선택하는 표정. 음성의 감정을 분석하지 않는다.</summary>
public enum ExpressionKind { Neutral, Happy, Sad, Angry, Surprised, Sleepy }

/// <summary>슬롯의 불변 설정. SheetId는 앱이 소유하는 복사본 식별자이며 외부 파일 경로가 아니다.</summary>
public sealed record ExpressionProfile(string Name, ExpressionKind Kind = ExpressionKind.Neutral,
    bool Blink = true, bool Tears = false, double TearLeft = .365, double TearRight = .635,
    double TearTop = .46, string? SheetId = null)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 40 || !Enum.IsDefined(Kind) ||
            !double.IsFinite(TearLeft) || !double.IsFinite(TearRight) || !double.IsFinite(TearTop) ||
            TearLeft is < 0 or > 1 || TearRight is < 0 or > 1 || TearTop is < 0 or > .8 ||
            (SheetId != null && !Guid.TryParseExact(SheetId, "N", out _)))
            throw new ArgumentException("표정 설정을 확인하세요.");
    }
    public static ExpressionProfile Default(int slot)
    {
        if (slot is < 0 or >= 12) throw new ArgumentOutOfRangeException(nameof(slot));
        string[] names = ["평상", "기쁨", "슬픔", "화남", "놀람", "졸림"];
        return slot < 6 ? new(names[slot], (ExpressionKind)slot, Tears: slot == 2) : new($"표정 {slot + 1}");
    }
}
