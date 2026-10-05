# 구현 로드맵

plan-001은 저장소 기반이다. plan-002-desktop-app에서 아래 예시 단계의 MVP 구현을 통합 진행했다.
단계명은 기존 예시이며 003~005 번호를 이미 사용한 계획으로 해석하지 않는다.
작업 시작 때 순번 충돌을 확인하고 [계획 템플릿](../../harness/templates/exec-plan.md)을 복사한다.

| 순서 / 계획 이름 예시 | 산출물 | 완료 근거 |
|---|---|---|
| plan-002-desktop-scaffold | 솔루션, Core/App/Tests, Windows 개발 환경, pinned NuGet | 실제 restore/build/test, 창 실행 |
| plan-003-audio-engine | 합성 입력 Analyzer/Processor, WASAPI 장치 선택·수명 관리 | 수치 단위 테스트, Start/Stop/제거/권한 거부 실기 |
| plan-004-character-preview | PNG 입/idle/blink/body, MVVM 조정 UI | 합성 VoiceLevel 데모, 경계/설정/무음/리소스 실기 |
| plan-005-streaming-window | 캐릭터 별도 창, OBS 캡처 실험, 배경 경로 | Windows/OBS 캡처 매트릭스와 방송 smoke test |

구현 시작 전 .NET 지원 수명, 사용할 Windows 빌드, NAudio 2.x 버전을 다시 확인한다.
.NET 10 전환을 택하면 IDE·target framework·NAudio API 변경을 함께 계획에 기록한다.
독자 제작 CC0 기본 PNG 자산과 GitHub Actions Windows QA 경로를 확보했다.
실제 마이크/OBS/장시간 환경은 아직 확보하지 못했으며 [검증 기록](../quality/windows-checklist.md)을 따른다.

확장 순서는 방송창 검증 → 캐릭터 에디터 → Live2D/Spine 검토이며,
Pitch/STT/감정은 실제 필요와 음성 데이터 경계를 별도 설계한 뒤 검토한다.

## 다음 완료 조건

실제 Windows 마이크·권한·제거·DPI/장시간·OBS 매트릭스 통과가 실사용 목표의 남은 조건이다.
코드 빌드 및 UI smoke를 통과해도 이 실기 조건을 통과했다고 간주하지 않는다.
