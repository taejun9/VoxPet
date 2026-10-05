# 앱 설계 — 구현 예정

이 문서는 구현 계약이다. 아래 프로젝트와 클래스는 아직 생성하지 않았다.

## 계층과 폴더

```text
VoxPet.sln
src/
  VoxPet.Core/                 # net8.0, UI/NAudio 의존성 없음
    Models/AudioLevel.cs
    Models/CharacterState.cs
    Models/CharacterParameters.cs
    Models/AudioSettings.cs
    Services/AudioAnalyzer.cs
    Services/AudioLevelProcessor.cs
  VoxPet.App/                  # net8.0-windows, WPF, x64, MVVM
    Services/AudioCaptureService.cs
    ViewModels/MainViewModel.cs
    ViewModels/CharacterViewModel.cs
    Views/MainWindow.xaml
    Views/CharacterWindow.xaml
    Assets/Characters/
tests/
  VoxPet.Core.Tests/           # 합성 신호와 dB 배열; 마이크 없이 실행
```

Core는 수치 계산과 캐릭터 파라미터 모델을 담당한다. App은 Windows 캡처, 수명 관리, Dispatcher와 PNG 표시를 담당한다.
두 프로젝트로 분리하여 숫자 로직을 OS 없이 검증한다. 추가 프레임워크나 범용 플러그인 시스템은 요구가 생기면 검토한다.
nullable 활성화, 주요 클래스 역할 주석, 명확한 단위와 오류 처리가 구현 기준이다.

## 오디오 계약

```text
Microphone → WASAPI → PCM decode → RMS / Peak → dBFS
           → Noise Gate → Normalize × Sensitivity → clamp
           → Attack / Release Smoothing → VoiceLevel
           → CharacterParameters → WPF PNG renderer
```

캡처의 실제 WaveFormat을 확인한다. float PCM이라고 가정하거나 system loopback을 사용하지 않는다.
선택한 패키지의 지원 형식을 확인하여 IEEE float와 필요한 integer PCM을 decode하며 미지원 형식은 안내한다.
인터리브된 전체 채널의 제곱 에너지로 RMS를 계산하여 반대 위상 채널이 mono 평균에서 상쇄되지 않게 한다.
callback 버퍼는 재사용될 수 있으므로 비동기 작업에서 원본 참조를 보관하지 않는다.

- N개의 유한한 full-scale 정규화 sample에서 `rms = sqrt(sum(x*x)/N)`, `peak = max(abs(x))`.
- 무음/빈 버퍼는 RMS/Peak 0. dBFS는 `20*log10(max(rms, 1e-6))`, 표시 바닥값 -120.
- 양의 dBFS와 peak > 1은 clipping 진단에 보존 가능하지만 최종 애니메이션은 clamp한다.
- NaN/Infinity, 불완전 sample, 잘못된 설정은 입력 경계에서 처리한다. 최종 값은 항상 유한하다.
- raw dBFS가 gate 미만이면 target 0. 이상이면 `raw = clamp((db-min)/(max-min), 0, 1)`.
- `target = clamp(raw * sensitivity, 0, 1)`. gate는 원본 dBFS 기준이며 sensitivity가 gate를 넘기지 않는다.
- `min < max`, sensitivity ≥ 0, attack/release ≥ 0을 검증한다. 설정은 불변 snapshot으로 전달한다.

기획의 gain/sensitivity는 MVP에서 정규화 값 배율로 정의한다. PCM gain이나 dB offset으로 바꿀 때는 결정 로그와 테스트를 갱신한다.
측정값은 상대 레벨 dBFS이며 실제 음압 dB SPL이나 감정·발화 여부를 판정하지 않는다.

## 시간 기반 smoothing과 무음

현재 y보다 target이 크면 attack, 작으면 release를 쓴다.
`alpha = 1 - exp(-dt/tau)`, `next = y + alpha*(target-y)` 후 0~1로 제한한다.
`dt`는 monotonic 경과 초, `tau`는 ms를 초로 변환한 시정수이다. tau=0은 즉시 target, dt=0은 이전 값 유지.
40ms는 target에 완전히 도달하는 시간이 아니라 시정수이다. 다른 callback 주기에서도 결과가 일관되어야 한다.
gate가 닫혀도 release를 적용하고, 충분히 작은 값(초기 제안 0.001)은 0으로 정착시킨다.
입력 callback이 끊기면 stale snapshot을 계속 쓰지 않는다. 무입력 timeout을 정의하고 target 0으로 release한다.
Stop/장치 변경/오류는 envelope와 표시값을 0으로 초기화해 이전 세션 값이 남지 않게 한다.

## 스레드와 리소스

WPF UI 접근은 Dispatcher에서만 수행한다. [공식 threading 문서](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model)를 기준으로 한다.
callback은 decode와 bounded O(N) 분석만 수행하고 숫자 snapshot을 교체한다. 파일 IO, 로그 대량 출력, 렌더링, 동기 Dispatcher 호출을 넣지 않는다.
UI는 최신 snapshot만 30~60Hz 목표로 읽어 대기열이 누적되지 않게 한다. smoothing의 시간축과 UI 표시 주기를 분리한다.
기본은 callback 분석이지만 측정에서 지연이 확인되면 소유권이 있는 bounded buffer worker를 별도 계획으로 도입한다.

캡처 상태는 Stopped → Starting → Running → Stopping → Stopped이며 실패 시 오류 상태와 재시작 경로를 제공한다.
Start 중복, Stop 중복, 장치 교체, 창 종료, 권한 거부, 장치 제거, callback/Stop race를 처리한다.
Stop 완료 전에 capture를 dispose하지 않고, Stop 완료 후 이벤트 구독·타이머를 정리하고 capture를 dispose한 다음 선택 endpoint를 해제한다.
이전 세션 callback은 session ID로 무시한다. async/await는 수명 관리에 필요한 곳에만 사용하고 UI에서 Wait/Result를 호출하지 않는다.

## 캐릭터와 방송 확장

CharacterParameters는 MouthOpen, BodyBounce, HeadTilt, EarMotion, EyeOpen을 렌더러에 넘기는 계약이다.
MVP의 MouthOpen은 VoiceLevel, body 이동 상한은 초기 제안 6 WPF DIP, head/ear는 0이다.
blink/idle 시간은 음성 입력과 독립이다. 디버깅에서는 seeded random을 주입해 blink 시퀀스를 재현한다.
PNG의 몸·눈·입 레이어 또는 같은 크기의 상태 이미지를 사용한다. 자산 manifest에 크기·anchor·저작권을 기록한다.
CharacterWindow는 MainWindow와 같은 파라미터 snapshot을 표시하고 오디오를 중복 캡처하지 않는다.

투명 WPF 창이 OBS Window Capture에서 alpha를 유지한다는 보장은 없다.
[OBS Window Capture 공식 설명](https://obsproject.com/kb/window-capture-sources)을 참고하고 Windows/OBS/캡처 방식별 실기로 검증한다.
후속 실험에서 투명창·단색 배경/chroma key 중 확인된 경로만 사용자에게 지원한다고 안내한다.
