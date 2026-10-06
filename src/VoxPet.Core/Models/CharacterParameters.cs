namespace VoxPet.Core.Models;

/// <summary>
/// PNG 열 인덱스와 같은 순서의 입 상태: 닫힘·중간·열림.
/// </summary>
public enum MouthState { Closed, Half, Open }
/// <summary>
/// 렌더러와 독립적인 캐릭터 상태. MouthOpen/EyeOpen은 0~1, BodyBounce는 WPF DIP 이동량이다.
/// HeadTilt/EarMotion은 확장용이며 현재 Animator는 0을 반환한다.
/// </summary>
public sealed record CharacterParameters(double MouthOpen, double BodyBounce, double HeadTilt,
    double EarMotion, double EyeOpen, MouthState Mouth);
