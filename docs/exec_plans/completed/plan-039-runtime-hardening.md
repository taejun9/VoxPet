# plan-039-runtime-hardening

## Status

completed

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
- [x] 전체 local QA 및 Windows 자동 QA를 실행한다.
- [x] Windows 실행 ZIP/테스트 안내를 준비하고 CRC/파일 구성을 확인한다.
- [x] QA 후 자체 리뷰와 완료 이동/리뷰 미러를 준비했다. 완료 커밋 뒤 main 병합/push와 브랜치/worktree 정리를 수행한다.

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
| 2026-10-15 | 검증 | 최종 bdde9ae Windows38714843197와 OBS38714843135 성공. Core366/366·WPF1768/0·safety24·desktop250·unsigned/signed 각9개 시작/정상 종료/파일 보존, ZIP 내부 PowerShell5.1 도구1536/0. TRX/JSON·실행 버전·공용/개인 EXE 해시·개인13PNG byte equality·CRC를 직접 대조했다. |
| 2026-10-15 | 심사 | 최종 QA 후 자체 리뷰에서 worker/scene 교체·지연 예외·입력 상한/해제·Canvas 범위·편집 미적용 값·privacy·문서/패키지를 재검토했다. 필수 미해결 사항 없음. 합성 키/좌클릭 시험은 전용 Windows/VM에서 수행하도록 안내를 명확히 했다. |

## Completion Notes

캐릭터 첫 frame 실패의 기존 scene/document/선택 보존, 지연 변환 오류의 앱 종료 방지와 입력 해제/재선택 복구, 보관함 실패 격리, 삭제된 preview 참조 복구, 미적용 입력 보호, 앵커 없는 문서 편집, 입력 대기 상한을 구현했다. 리뷰에서 발견한 극소/과대 Canvas도 package1~16384 범위로 보완했다. 음성 캡처/저장/전송 경계는 변경하지 않았다.

최종 코드bdde9ae 기준 local locked restore/format verify/analyzer/Release 경고0·오류0/Core366/harness/Python compile/diff 통과. Windows38714843197의 Core366/366·실제 WPF1768/0·새 safety24검사·실제 desktop250화면·unsigned/signed 각9개 일반 시작/정상 종료/저장 파일 보존을 TRX/JSON으로 확인했다. 실제 ZIP에서 PowerShell5.1 실행 도구1536/0을 통과했다. 일반 사용자 도구는 CI desktop232검사를 켜지 않는다. OBS38714843135는 WGC/chroma/native alpha·같은 HWND 배경 왕복·각7프레임 변화 통과, 기존 BitBlt 제한 유지다.

최종 개인 파일은 `artifacts/VoxPet-Windows-test-plan039-final.zip`이다. 자체 포함 unsigned win-x64 EXE·자산·라이선스·START.cmd·TEST-SMOKE.cmd/ps1·TEST-README.txt와 기존 개인13PNG를 담았다.50파일/95,456,545bytes·CRC 통과·SHA256 `d782eeb7540f4f12039c3506904ecf4096840542791d8b322b1b03e2e2a387c6`. EXE SHA256 `89190e0bf2a6889e99c9f8aea9ae3a5ab80797667fc2733b6df1aea424394509`는 최종 공용 CI publish와 같다. 개인13PNG 원본 bytes/해시/CRC와 기존 원본·ZIP 보존을 확인했고 외부 전송하지 않았다. 증거는 `artifacts/test-results/plan039/final/`에 있다.

물리 마이크/권한/USB·사용자 GPU/OBS·혼합 DPI·60분 실기와 개인 PNG의 Windows 모습은 대상PC의 수동 확인 사항이다. 실행 도구의 실제 합성 키/좌클릭은 전용 Windows/VM에서 사용한다. unsigned 배포를 사용자 PC에서 신뢰 검증된 서명으로 표현하지 않는다. QA 후 자체 리뷰와 완료 미러를 작성했으며 앱/실행 도구 코드는 최종 CI와 동일하다. 이후 변경은 완료 기록·체크리스트·안내 텍스트뿐이다.

완료 기록 커밋 후 main ff-only 병합/push·branch -d·worktree 제거를 순서대로 수행하고 실제 성공 상태를 사용자에게 보고한다.
