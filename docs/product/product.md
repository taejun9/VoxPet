# VoxPet 제품 정의

사용자의 목소리 크기를 로컬에서 분석해 캐릭터의 입과 몸을 움직이는 Windows 방송용 프로그램.
제품명은 VoxPet, 향후 Windows 실행 파일명은 VoxPet.exe를 목표로 한다.
Core/App/Tests C# 솔루션, WASAPI 캡처와 독자 제작 PNG 6장, 조정 UI와 별도 방송창을 구현했다.
실제 마이크/OBS 합격 여부는 [검증 기록](../quality/windows-checklist.md), 실행은 [사용 안내](../user-guide.md)를 따른다.

## 사용자와 성공 조건

얼굴캠 없이 캐릭터를 사용하는 스트리머와 음성 반응형 캐릭터를 원하는 사용자.
마이크 선택 → Start → 말하기 → 캐릭터 반응 → Stop 흐름이 간단해야 한다.
침묵에서는 입이 닫히고, 작은 소음은 gate로 줄이며, 음절에는 빠르게 반응하고 말이 끝나면 부드럽게 닫힌다.
음량 gate는 소음과 음성을 의미적으로 구별하지 못한다. 큰 키보드 소리가 반응할 수 있으며 실기에서 조정한다.

## MVP 범위

| 영역 | 요구사항 |
|---|---|
| 입력 | 마이크 장치 선택, WASAPI 캡처, Start/Stop, 연결 해제 안내 |
| 분석 | RMS, Peak, dBFS, raw normalized level, smoothed VoiceLevel |
| 조정 | Noise Gate, Sensitivity, Attack, Release 슬라이더, 고급 입력 반응 min/max |
| 캐릭터 | PNG 기반 idle, 입 닫힘/반 열림/전체 열림, 눈 깜빡임 |
| 움직임 | VoiceLevel에 따른 제한된 상하 이동, 내부 MouthOpen 0~1 유지 |
| UI | 장치 ComboBox, 분석값 표시, 조정값, 미리보기, 상태/오류 안내 |
| 방송 확장 | 캐릭터 표시를 설정 UI와 분리 가능한 구조; 투명 캡처는 후속 실험 |

## 기본값과 경계

| 설정 | 초기값 | 의미 |
|---|---|---|
| Noise Gate | -50 dBFS | 이보다 작은 입력의 목표 레벨은 0 |
| Normalize Min / Max | -50 / -10 dBFS | clamp된 선형 매핑 범위 |
| Sensitivity | 1.0 | gate 이후 정규화 레벨에 곱하는 무차원 배율 |
| Attack / Release | 40 / 140ms | 시간 기반 envelope의 시정수 |
| Blink 간격 | 2~6초 | 무음에도 독립적으로 동작 |

후반 오디오 기획의 40/140ms를 초기 예시 50/150ms보다 우선한다.
입 상태는 `[0, 0.2)` 닫힘, `[0.2, 0.6)` 반 열림, `[0.6, 1]` 전체 열림이다.
경계의 시각 떨림이 남으면 상태 hysteresis를 별도 결정 기록으로 추가한다.

## 기술 기준과 이후 기능

기획 기준: Windows x64, C#, .NET 10 LTS, WPF, MVVM, NAudio.Wasapi 2.2.1, WPF PNG 렌더링.
.NET 지원 수명·OS 빌드·패키지 확인은 [개발 환경](../quality/development.md)에 기록한다.
별도 방송창, 투명/항상 위/Green Screen 옵션은 구현했다. OBS 실기 검증은 남아 있다.
OBS 32.2.2/Windows Server2025/Basic Render Driver의 WGC 합성 데모는 green+chroma/native alpha와60.17분 자원·변화 시험을 통과했다. 실제 마이크와 사용자 GPU/OBS 조합은 별도 조건으로 남긴다.
클릭 통과/OBS Browser Capture는 다음 단계다.
Pitch, STT, 키워드·감정 반응, Live2D, Spine, 캐릭터 에디터는 장기 후보이며 MVP 요구사항이 아니다.
웹캠·얼굴 인식·오디오 재생/녹음 저장은 초기 범위에 없다.

자세한 [설계](../architecture/application.md)와 [로드맵](roadmap.md)을 따른다.

## 구현에 추가한 사용성

마이크 없는 합성 데모, 오디오 조정값/방송창 옵션 로컬 저장, 기본값 복구를 제공한다.
Windows 11 지원 중인 버전을 우선 검증한다. Windows 10은 .NET 10 공식 지원 edition/build와 실제 QA를 확인해야 한다.

표정 확장(plan011): 로컬 영구 F1~F12 슬롯, 내장6종/슬롯별 사용자 PNG, Ctrl+Shift+F1~F11 전역 전환(F12 앱 내부), 240ms 전환과 설정 가능한 눈물 모션을 제공한다. 슬롯 사용은 [사용 안내](../user-guide.md)를 따른다.

사용성 개선(plan013): Ctrl+Shift+M 전역 입 음소거, 3초 주변 소음 Gate 추천과 직접 적용/취소, Windows 마이크 권한 설정 바로가기를 추가했다. [사용자 기능 조사와 구현 격차](user-feature-research.md)에 18개 기능의 근거·구현 여부·우선순위를 기록했다. 자동 QA와 실기 상태는 검증 기록을 따른다.
