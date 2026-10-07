# 앱 설계 — MVP 구현

이 문서는 구현 계약과 구현 상태를 함께 기록한다. 아래 계층을 생성했으며 추가 수명/설정 구성은 하단에 기록한다.

## 계층과 폴더

```text
VoxPet.sln
src/
  VoxPet.Core/                 # net10.0, UI/NAudio 의존성 없음
    Models/AudioLevel.cs
    Models/CharacterParameters.cs
    Models/AudioSettings.cs
    Services/AudioAnalyzer.cs
    Services/AudioLevelProcessor.cs
  VoxPet.App/                  # net10.0-windows, WPF, x64, MVVM
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
512×512 RGBA PNG 6장(입 3단계 × 눈 2단계)을 표정별로 사용한다. plan011은 표정6종의 총36장으로 확장했다. 자산 manifest에 크기·anchor·저작권을 기록한다.
CharacterWindow는 MainWindow와 같은 파라미터 snapshot을 표시하고 오디오를 중복 캡처하지 않는다.

투명 WPF 창이 OBS Window Capture에서 alpha를 유지한다는 보장은 없다.
[OBS Window Capture 공식 설명](https://obsproject.com/kb/window-capture-sources)을 참고하고 Windows/OBS/캡처 방식별 실기로 검증한다.
후속 실험에서 투명창·단색 배경/chroma key 중 확인된 경로만 사용자에게 지원한다고 안내한다.

## 현재 수명과 표시 구현

Core의 AudioSession은 IAudioInput을 주입받고 SemaphoreSlim으로 Start/Stop/Dispose를 직렬화한다.
각 세션에 별도 숫자 snapshot과 콜백 closure를 두어 이전 입력이 다음 세션으로 새지 않는다.
공유 최신값은 Volatile로 전달하며, 250ms 이상 오래된 입력은 무음으로 바꾼다.
Stop은 callback 종료까지 기다린 뒤 이벤트를 해제하고 캡처/endpoint를 dispose한다.
WASAPI 생성/초기화는 Task.Run에서 수행하고 native capture는 전용 worker에서만 실행한다.
NAudio AudioClient/AudioCaptureClient의 shared-mode 20ms 버퍼를 사용한다.
CaptureLoop가 capture와 native Stop 예외를 모두 completion 상태로 보고해 장치 제거 후 종료 예외가 작업 스레드 밖으로 빠지지 않게 한다.
native 프레임은 반드시 ReleaseBuffer하며 PCM 메모리는 dispose에서 지운다.
Stop 이벤트가 5초 이내 오지 않으면 오류를 표시하고 자원을 유지한 채 재시도하도록 한다.

MainViewModel은 DispatcherTimer 목표 60Hz에서 monotonic 경과시간으로 envelope와 blink를 업데이트한다.
UI 표시 주기 자체를 attack/release 상수로 사용하지 않는다. callback에서는 decode/숫자 분석만 수행한다.
설정은 UI 소유 불변 record로 바꾸며 캡처 callback이 설정 객체를 읽거나 UI를 갱신하지 않는다.
CharacterViewModel의 frozen PNG를 두 창이 공유하며 WPF Image를 유일한 렌더러로 사용한다.
SettingsStore는 조정값과 방송창 topmost/background만 원자적 파일 교체로 저장한다.

AudioSettings는 유한한 dBFS -120~0, normalize min<max, sensitivity 0~10, 시간 0~5000ms를 허용한다.
UI는 gate -90~-10, sensitivity 0~4, attack 0~300, release 0~1000의 실용 범위를 제공한다.
고급 입력 반응 범위는 normalize min -120~-1 / max -119~0을 제공하고 min<max가 유지되도록 상대 경계를 함께 조정한다.
잘못된 float sample은 0으로 바꾸고 극단적인 full-scale 값은 ±16에서 제한한다. 일반 clipping은 진단에 보존한다.
미지원/불완전 PCM 프레임은 오류로 종료해 잘못된 수치를 표시하지 않는다.

별도 CharacterWindow의 초록 배경/투명/항상 위, 드래그/크기 조절/Esc/우클릭 닫기를 제공한다.
기본 배경은 초록색이며 OBS alpha는 실기 통과 전 지원을 보장하지 않는다.
기본 PNG는 generate_character.py의 독자 제작 도형 자산으로 manifest에 CC0 출처/anchor를 기록했다.
실제 마이크/장시간/OBS 검증은 [체크리스트](../quality/windows-checklist.md)에 남아 있다.

방송창은 설정창과 Win32 소유 관계를 만들지 않는 독립 top-level 창이다. 설정창 최소화가 방송창을 숨기지 않아야 한다.
메인 종료는 MainWindow.ShutdownAsync에서 캡처 종료 뒤 방송창을 명시적으로 닫는다.

## 설정창의 작은 화면 대응

최소 크기는 480×320 WPF 논리 단위다. 시작 시 `SystemParameters.WorkArea`의 주 화면 작업 영역에 크기와 위치를 맞춘다.
폭870 미만 또는 높이600 미만에서는 조작 영역/미리보기 순서의 단일 열 배치로 바꾼다. 넓은 창에서는 두 열을 유지한다.
설정과 미리보기는 한 ScrollViewer를 사용해 내측 스크롤 영역이 마우스 휠을 가로채지 않도록 한다. 버튼 그룹은 WrapPanel로 줄바꿈한다.
작은 창은 제목/상태 영역을 줄이며 긴 상태는 별도 스크롤로 읽는다. 마이크 세션/숫자 처리/방송창에는 영향이 없다.
LayoutQa는 실제 WPF 창의 논리 크기/컨트롤 가시성/휠 routed event와 고해상도 render를 검사한다. 실제 OS DPI 변경과 구분한다.

## 사용자 캐릭터와 반응 편의 기능

plan008의 CharacterSheetLoader는 작업 스레드에서 16MB 이하 파일을 읽고 PNG signature/IHDR의 8비트 RGBA 및 셀 크기(128~1024px, 정사각형 3열×2행)를 디코딩 전에 확인한다.
OnLoad로 원본 파일을 해제하고 각 셀을 BGRA로 변환해 알파 영역 기준 중앙/바닥을 정렬한 뒤 Freeze한다. 빈 셀 또는 투명 여백이 없는 셀은 거부한다.
입 열림 3단계와 눈 뜸/감음 2단계의 완전한 배열을 모두 만들고 난 뒤 UI에서 한 번에 교체하므로 오류 시 이전 캐릭터가 유지된다.
닫기 중 도착한 결과는 적용하지 않는다. 일반 불러오기의 외부 파일 경로와 캐릭터는 영구 설정에 저장하지 않는다. plan011 표정 슬롯에 명시적으로 저장하면 관리 PNG 복사본을 유지한다.
CharacterStatus는 마이크 Status와 분리돼 적용/실패 안내가 Running/Stop 안내를 덮어쓰지 않는다.

Core의 ReactionPresets가 일반 대화·조용한 목소리·빠른 반응의 불변 AudioSettings를 제공한다. UI는 preset 적용 후 전체 조정값을 통지하며 개별 슬라이더 조정을 계속 허용한다.
CharacterMuted는 envelope와 VoiceLevel만 즉시 초기화한다. RAW/RMS/Peak와 마이크 세션은 유지하고 blink/idle은 독립적으로 계속된다.
음소거 상태를 해제하면 현재 입력부터 반응하고, Stop은 기존대로 캡처와 레벨을 해제한다. 음소거 상태도 영구 저장하지 않는다.


## 표정 슬롯·전환·모션 (plan011)

Core의 ExpressionProfile은 표정 이름·내장 종류·blink/tears·정규화 눈물 좌표·관리 PNG GUID만 갖는다. ExpressionSlotStore는 슬롯별 4KiB 이하 JSON과 16MB 이하 PNG 복사본을 저장한다. 외부 파일 경로는 보관하지 않으며 GUID 검증으로 경로 이탈을 거부한다. 작업 스레드에서 새 PNG를 완성한 뒤 슬롯 JSON을 원자적으로 교체하고 이전 복사본을 정리한다. 실패하면 이전 저장값이 유지된다. 슬롯별 손상은 다른 슬롯에 영향을 주지 않는다.

ExpressionViewModel은 편집 초안/마지막 저장값을 분리한다. 저장은 즉시 수행하며 시작 시 저장된 F1 표정을 적용한다. UI 타이머/오디오 callback에는 파일 IO를 추가하지 않는다. 시트 로딩/인코딩/저장은 작업 스레드, 표시 적용은 UI에서 한다. SemaphoreSlim과 요청 revision으로 로딩을 직렬화하고 연속 단축키 요청의 오래된 결과를 버린다. 종료 시 새 적용을 막고 진행 중 저장을 기다린다. QA는 사용자 폴더 대신 주입한 temp 저장소나 메모리를 사용한다.

기본 고양이의 여섯 표정마다 입3×눈2 상태를 생성하여 총36장 frozen PNG를 공유한다. 사용자 슬롯은 독립적인 3×2 시트이며 내장 얼굴을 합성하지 않는다. CharacterViewModel은 누적 monotonic 초와 240ms smoothstep으로 표정을 전환한다. 진행 중 재전환은 현재 합성 이미지/눈물을 512px snapshot 한 장으로 고정해 이전 합성 참조가 쌓이지 않는다. CharacterView는 두 창에서 동일한 Viewbox/512px 좌표계로 PNG와 독립적인 두 눈물 방울을 표시한다. 눈물 위치는 슬롯별로 조정하며 Stop/음소거와 무관하게 시간축을 유지한다.

ExpressionHotkeys는 설정창의 HwndSource에 RegisterHotKey(MOD_CONTROL|MOD_SHIFT|MOD_NOREPEAT)를 등록한다. F1~F11은 전역, F12는 Windows 예약 키이므로 두 창의 PreviewKeyDown fallback이다. 등록 충돌을 표시하고 충돌 키도 앱 안에서 전환한다. 전역 메시지와 로컬 키 이벤트가 중복 전환하지 않으며 키보드 훅/입력 로그를 만들지 않는다. 성공한 종료 또는 Closed에서 등록과 hook을 해제한다.
