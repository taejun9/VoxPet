# plan-002-desktop-app

## Status

active

## Owner

project_lead / plan_keeper

## User Request

실제 사용할 수 있도록 프로젝트의 기능 구현 및 버그 테스트를 진행한다.

## Goal

Windows용 VoxPet MVP 전체 구현, 재현 가능한 수치·수명 테스트, 빌드 및 배포 경로 제공. Windows 실기와 OBS 확인 전에는 실사용 검증 완료라고 보고하지 않는다.

## Non-Goals

음성 저장/전송, STT/감정, Live2D/Spine, 웹캠, 원격 서비스. 투명 OBS alpha 보장과 자동 업데이트는 포함하지 않는다.

## Context Map

제품/설계/개발/QA/개인정보 문서, NAudio release/2.x WasapiCapture 공식 소스, Microsoft .NET 지원 정책.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-002-desktop-app` / `.worktree/plan-002-desktop-app` 사용.
QA → 리뷰 → completed 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.
README 원문 보존. 기존 설계 문서는 변경점을 추가하거나 필요한 부분을 편집한다.

## Implementation Plan

- [ ] SDK 준비 및 Core/App/Tests 솔루션과 pinned 의존성 생성
- [ ] PCM/RMS/gate/smoothing/timeout/캐릭터 수치 계약 구현
- [ ] WASAPI 장치 목록·Start/Stop·오류·세션/리소스 수명 구현
- [ ] PNG 자산·미리보기·blink·슬라이더·독립 방송창·설정 보존 구현
- [ ] 합성 신호/가짜 캡처 버그 테스트 및 Windows CI/배포 경로 생성
- [ ] QA 이후 자체 리뷰, 문서 동기화와 Git 완료 절차

## QA Plan

macOS arm64: 로컬 SDK로 Core 테스트, WPF 교차 컴파일 및 win-x64 publish 시도. `python3 harness/scripts/verify_base.py`, `git diff --check` 필수.
수치: silence/sine/full scale/clipping, PCM 8/16/24/32/float, 반대 위상, invalid 입력/설정, gate 경계, smoothing 주기 독립, stale/Stop reset, blink/body 경계.
수명: fake capture Start/Stop 반복/동시 요청/오류/이전 세션 callback, 종료 정리.
Windows CI: restore/build/test/publish, headless WPF smoke. 실제 마이크·권한·제거·OBS·DPI·장시간 QA는 체크리스트로 제공하고 수행 여부를 별도로 기록한다.

## Review Plan

단일 에이전트의 QA 이후 자체 리뷰. 수식·스레드·자원·개인정보·패키징·문서 계약을 읽고 발견 사항을 수정/재검증한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-05 | roadmap 002~005의 MVP를 하나의 계획으로 구현 | 사용자 요청이 전체 실사용 기능 구현이며 기존 계획은 예시 번호임 |
| 2026-10-05 | .NET 10 LTS, NAudio 2.2.1 고정 | .NET 8의 지원 종료가 임박함; 2.x API 계약을 유지하면서 최신 LTS로 시작 |
| 2026-10-05 | Windows 실기 완료 조건 유지 | 현재 호스트는 macOS이며 WPF/WASAPI 실제 실행 불가 |

| 2026-10-05 | SDK 10.0.401 및 win-x64/single-file restore 속성 고정 | 잠금 모드 publish에서 RID/ILLink 참조 차이를 발견; 빌드/배포 restore 계약을 일치시킴 |

| 2026-10-05 | Windows QA를 위한 작업 브랜치 checkpoint commit/push 허용 | main 통합 전 원격 runner에서 WPF 실행 근거를 얻어야 함; main 완료 절차는 QA/리뷰 이후 유지 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 지도 | main 깨끗함 및 원격 동기화 확인, 지정 worktree 생성. Git 쓰기는 sandbox escalation 후 성공 |

## Completion Notes

진행 중. 초기 53개/설정 추가 후 64개 테스트 통과, WPF 교차 빌드 경고/오류 0, win-x64 EXE 생성. 자체 리뷰에서 극단값 overflow, Stop 실패 후 재시도 버튼 조건, smoke의 사용자 설정 격리를 수정했다. 최종 회귀/Windows CI를 실행한다.
