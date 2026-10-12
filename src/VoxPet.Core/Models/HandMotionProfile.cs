namespace VoxPet.Core.Models;

public enum HandGesture { HandRaise = 0, ToyGun, Heart, ToySword, MagicWand }
public enum HandTriggerMode { Always, Interval, Hotkey }

public sealed record HandShortcut(uint Modifiers, uint Key)
{
    public void Validate()
    {
        bool supported = Key is >= 0x41 and <= 0x5a or >= 0x30 and <= 0x39 or >= 0x70 and <= 0x7a;
        if (!supported || Modifiers is not (1 or 2 or 3 or 5 or 6 or 7) ||
            (Modifiers == 6 && (Key == 0x4d || Key is >= 0x70 and <= 0x7a)))
            throw new ArgumentException("Ctrl/Alt 조합과 지원 키를 선택하세요. 기존 음소거·표정 키는 사용할 수 없습니다.");
    }
}

public sealed record HandMotionProfile(HandGesture Gesture, bool Enabled, HandTriggerMode Mode,
    double IntervalSeconds, HandShortcut Shortcut, double X = 0, double Y = 0, double Scale = 1, bool LeftClick = true)
{
    public static HandMotionProfile Default(HandGesture gesture) => new(gesture, gesture == HandGesture.HandRaise, HandTriggerMode.Hotkey, 5, new(3, 0x31 + (uint)gesture));
    public void Validate()
    {
        if (!Enum.IsDefined(Gesture) || !Enum.IsDefined(Mode) || Shortcut == null ||
            !double.IsFinite(IntervalSeconds) || IntervalSeconds is < 2 or > 3600 ||
            !double.IsFinite(X) || !double.IsFinite(Y) || X is < -128 or > 128 || Y is < -128 or > 128 ||
            !double.IsFinite(Scale) || Scale is < .5 or > 1.5) throw new ArgumentException("손동작 설정 범위를 확인하세요.");
        Shortcut.Validate();
    }
}

public sealed record HandMotionSettings(HandMotionProfile[] Profiles, Dictionary<string, CharacterRig>? Rigs = null)
{
    public static HandMotionSettings Default() => new(Enum.GetValues<HandGesture>().Select(HandMotionProfile.Default).ToArray());
    public void Validate()
    {
        if (Profiles == null || Profiles.Length != 5 || Profiles.Any(profile => profile == null) ||
            Profiles.Select(profile => profile.Gesture).Distinct().Count() != 5) throw new ArgumentException("손동작5종을 확인하세요.");
        foreach (var profile in Profiles) profile.Validate();
        if (Rigs != null)
        {
            if (Rigs.Count > 7 || Rigs.Any(pair => pair.Value == null || pair.Key is not ("custom" or "builtin") && !CharacterCatalog.All.Any(character => character.Id == pair.Key))) throw new ArgumentException("캐릭터 연결 설정을 확인하세요.");
            foreach (var rig in Rigs.Values) rig.Validate();
        }
        var keys = Profiles.Where(profile => profile.Enabled && profile.Mode == HandTriggerMode.Hotkey && !profile.LeftClick).Select(profile => profile.Shortcut).ToArray();
        if (keys.Distinct().Count() != keys.Length) throw new ArgumentException("사용 중인 손동작 핫키가 중복됩니다.");
        if (Profiles.Count(profile => profile.Enabled && profile.Mode == HandTriggerMode.Hotkey && profile.LeftClick) > 1) throw new ArgumentException("좌클릭 동작은 하나만 활성화할 수 있습니다.");
    }
}
