# plan-039-runtime-hardening

## Status

active

## Owner

project_lead / plan_keeper

## User Request

전체 test/lint를 통과시키고 실제 앱의 버그와 위험요소를 조사·수정한다. Windows에서 직접 테스트할 파일을 준비한다.

## Goal

실제 production 경로의 재현 가능한 결함을 회귀 검사와 함께 수정한다. locked restore/format/analyzer/build/Core/harness를 통과시키고 Windows 자동 WPF QA 및 실행 가능한 win-x64 ZIP과 수동 검사 안내를 준비한다.

## Non-Goals

새 기능·음성 저장/전송·개인 PNG 외부 전송·인증서 자동 신뢰를 추가하지 않는다. macOS 또는 합성 CI 결과를 실제 사용자 마이크/GPU 실기 합격으로 표현하지 않는다.

## Context Map

- [개발 환경](../../quality/development.md), [품질 규칙](../../quality/rules.md), [개인정보](../../privacy/principles.md)
- src/VoxPet.App/Services, ViewModels, Views; src/VoxPet.Core; tests/VoxPet.Core.Tests
- harness/scripts/qa.ps1 및 Windows/OBS workflows

## Constraints

codex/plan-039-runtime-hardening / .worktree/plan-039-runtime-hardening에서 작업한다. 단일 에이전트가 QA 후 자체 리뷰한다. 기존 사용자 파일·개인 PNG·다른 worktree를 보존한다. Git 실패는 원인을 확인하고 강제 우회하지 않는다.

## Implementation Plan

- [x] 현재 검사와 production 경로를 확인하고 결함 재현 근거를 수집한다.
- [x] 위험도 높은 결함을 수정하고 의미 있는 회귀 검사를 추가한다.
- [ ] 전체 local QA 및 Windows 자동 QA를 실행한다.
- [ ] Windows 실행 ZIP/테스트 안내를 준비하고 CRC/파일 구성을 확인한다.
- [ ] QA 후 자체 리뷰, 완료 이동/리뷰 미러, main 병합/push와 브랜치/worktree 정리를 수행한다.

## QA Plan

Python verify_base/verify_app, git diff --check, SDK10.0.401 locked restore, dotnet format verify, Release build 경고0/오류0 및 전체 Core tests. Windows CI에서 qa.ps1 -Publish -Smoke와 시작/종료/desktop coverage를 실행한다. 렌더 변경 시 OBS QA도 수행한다. ZIP에는 자체 포함 unsigned EXE·자산·라이선스·수동 테스트 안내를 넣고 CRC와 해시를 확인한다. 개인 자산은 전송하지 않는다.

## Review Plan

QA 이후 같은 에이전트가 production 수명/동시성/패키지·문서 경계/개인정보·회귀를 자체 리뷰한다. 자동 검사와 실기 미확인 사항을 구분한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-15 | plan039 격리 worktree에서 기존 최신 main을 기준으로 조사한다. | 기존 완료355 Core/WPF1744를 보존하며 실제 경로의 결함을 찾는다. |
| 2026-10-15 | scene/document 교체 전 worker에서 첫 frame까지 준비하고 native/편집 preview의 지연 변환 실패를 격리한다. | 유효한 패키지에서도 극단적 변환의 합성 overflow가 첫 tick 또는 action 중 발생해 기존 상태 오염/Dispatcher 오류로 이어질 수 있다. |
| 2026-10-15 | 삭제 후 preview ID를 재선택하고 미적용 입력 중 삭제/드래그를 막으며 앵커 없는 문서 편집을 지원한다. | 존재하지 않는 action/expression/attachment 참조와 암묵적 입력 손실을 방지한다. |
| 2026-10-15 | 입력128개/프레임, 마우스 버튼별 예약1개로 제한하고 standalone 테스트 ZIP을 CI에서 재실행한다. | UI 지연 때 메모리/Dispatcher 작업 누적을 막고 사용자에게 전달할 실제 파일까지 검사한다. |
| 2026-10-15 | 실패한 import는 첫 frame 확인 전에 보관함에 쓰지 않고 기존 손상 항목의 thumbnail 오류를 개별 격리한다. | 실패한 등록이 보관함 용량을 차지하거나 이후 새로고침을 중단하지 않게 한다. |
| 2026-10-15 | Windows 공용 ZIP과 별도로 기존 개인 ZIP의 늘보군/펠리13PNG를 로컬 개인 ZIP에 복사한다. | 실제 사용자 캐릭터 시험을 준비하면서 개인 PNG 외부 전송 금지와 기존 원본 보존을 유지한다. |
| 2026-10-15 | QA 후 자체 리뷰에서 package Canvas의 기본 범위를1~16384로 제한하고 전체 QA를 다시 실행한다. | 기존 Positive 검사는1e-300/1e300도 허용해 WPF 배율 overflow/과대 geometry를 만들 수 있었다. Core의 일반 geometry API는 유지하며 package 로드/편집/export 경계를 강화한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-15 | 설계 | main/origin 동기화와 깨끗한 상태 확인, 전용 worktree와 실행 계획 작성. |
| 2026-10-15 | 검증 | 기존 Core355/355 및 수정 중 Release 빌드 경고0/오류0. 로컬 임시 SDK 중 불완전한 설치 대신 기존 완전한10.0.401 설치를 사용했고 NuGet audit는 네트워크 허용 환경에서 통과했다. |
| 2026-10-15 | 제작 | f97a0c5의 runtime/편집기/보관함 실패 복구와 입력 상한, 합성 ViewModel 회귀23개 및 Windows 테스트 ZIP·실행 도구를 구현했다. local format verify/Release 경고0·오류0/Core355/355/harness/Python compile/diff 통과. |
| 2026-10-15 | 검증 | OBS38712924709의 결과 JSON을 확인했다. OBS32.2.2 WGC/크로마/native alpha/동일 HWND 배경 왕복·각7프레임 변화 통과. 기존 BitBlt 제한 유지. 개인13PNG 원본 해시/PNG CRC를 로컬에서 확인했다. Windows38712924747은 전체 QA/ZIP 시험 진행 중이다. |
| 2026-10-15 | 검증 | 초기 f97a0c5 Windows38712924747 전체 성공: Core355/355·WPF1767/0·신규23검사·desktop250, unsigned/signed 각각9시작·정상 종료/파일 보존, ZIP 내부 PowerShell5.1 도구1535/0. 결과 JSON/TRX/CRC/EXE 해시 대조 완료. |
| 2026-10-15 | 심사 | 초기 QA 후 자체 리뷰에서 Canvas 범위 누락을 찾아 보완했다. Core11회귀와 WPF1회귀를 추가하고 최신 코드의 전체 QA와 새 ZIP을 다시 준비한다. 초기 증거·ZIP은 보존한다. |

## Completion Notes

조사·QA 결과와 Windows 패키지 경로를 완료 시 기록한다.
