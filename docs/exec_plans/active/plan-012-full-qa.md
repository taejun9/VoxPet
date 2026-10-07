# plan-012-full-qa

## Status

active

## Owner

project_lead / plan_keeper

## User Request

모든 테스트와 lint를 통과시키고 주석과 README를 확인한다. 실제 화면 시험은 늘보군으로 수행한다. 사용자는 늘보군의 임시 CI 전송을 명시 승인했으며 직접 시험할 때도 사용하도록 캐릭터를 삭제하지 말라고 요청했다.

## Goal

고정 SDK의 전체 자동 QA와 Windows 실제 WPF 화면 검증을 다시 실행하고, 늘보군 시트의 표시·반응·표정 전환을 확인한다. 주석과 README의 불일치를 수정하고 증거를 남긴다.

## Non-Goals

음성 녹음·전송, 공개 배포에 개인 캐릭터 추가, 실제 마이크가 없는 CI를 물리 장치 실기로 주장하기.

## Context Map

- README.md, docs/quality/development.md, docs/quality/windows-checklist.md: 사용자 안내와 QA 기록.
- src/, tests/, harness/scripts/: 구현·주석·자동 검증.
- .github/workflows/windows.yml, obs.yml: Windows WPF와 OBS 실행 환경.
- root artifacts/characters/neulbo/: 기존 개인 시험 시트, Git 추적 제외.

## Constraints

`codex/plan-012-full-qa` / `.worktree/plan-012-full-qa`에서 작업한다. 사용자 요청 범위의 기존 파일을 부분 수정한다. 개인 시트를 Git/공개 배포에 넣지 않는다. QA → 자체 리뷰 → 완료 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거 절차를 따른다. 서브에이전트를 실행하지 않는다.

## Implementation Plan

- [x] README와 코드·하네스 주석을 실제 구현과 비교하고 불일치 수정.
- [x] 문서/자산/공백 검사, locked restore, format/analyzer, Release build, 전체 Core 테스트.
- [ ] Windows 전체 QA와 늘보군 실제 WPF 화면·상태/표정 전환 검증, OBS 회귀 확인.
- [ ] 결과 기록, QA 이후 자체 리뷰, Git 통합과 정리.

## QA Plan

macOS arm64의 기존 임시 SDK10.0.401로 verify_base.py, verify_app.py, git diff --check, locked restore, dotnet format --verify-no-changes, Release build, Core test를 수행한다. Windows에서 qa.ps1 -Publish -Smoke를 실행하고 실제 WPF 이미지와 JSON을 직접 확인한다. 기존 늘보군3×2 시트로 여섯 상태·합성 반응·미리보기와 방송창·표정 슬롯/눈물/blink를 확인한다. OBS WGC green/chroma/native alpha 회귀를 수행한다. 합성 입력과 물리 마이크·키보드·DPI 시험을 구분한다. 승인된 개인 시트는 기존 scoped URL 방식만 사용한다. 임시 URL secret은 QA 뒤 제거하고 늘보군 원본·private draft asset·개인 화면 증거·사용자 직접 시험용 패키지는 보존한다. 일반 공개 배포의 기본 캐릭터를 교체하지 않는다.

## Review Plan

QA 이후 동일 에이전트가 주석의 설명/구현 일치, README의 기능·명령·검증 상태, QA 증거/개인정보·회귀를 자체 리뷰한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-07 | 기존 늘보군 로컬 시트를 재사용하고 Windows 실행 PNG를 확인 | 사용자가 실제 화면 시험과 캐릭터를 지정 |
| 2026-10-07 | macOS의 Git metadata 쓰기 제한은 승인 실행으로 동일 명령 재시도 | FETCH_HEAD 쓰기 거부, 사용자 변경 없이 worktree 규칙 준수 |
| 2026-10-07 | 로컬 QA 뒤 작업 브랜치 checkpoint commit/push로 Windows CI 실행 | 실제 WPF 실행에는 Windows runner가 필요, 완료 판정은 전체 QA 이후 |
| 2026-10-07 | 개인 시트의 표정 슬롯 재시작·손상 복구와 두 창 캡처를 기존 smoke에 추가 | 이전 개인 시험은 일반 불러오기의 입 반응만 확인 |
| 2026-10-07 | 개인 PNG 전송은 명시 승인 대기, 기본 QA와 경고 제거부터 진행 | 자동 승인 검토가 개인 외부 전송 승인 부족으로 거부. PUBLIC 저장소 확인, draft도 별도 승인 없이 생성하지 않음 |
| 2026-10-07 | Windows/OBS Actions를 공식 Node24 버전 checkout7/dotnet6/python7/upload7로 갱신 | 첫 CI에서 Node20 및 의존성 deprecation 경고 확인. 공식 릴리스와 action.yml runtime 확인 |
| 2026-10-07 | 사용자 명시 승인으로 개인 PNG 전송 진행, 늘보군과 시험 자료 보존 | 사용자가 임시 전송을 승인하고 직접 시험에도 사용하므로 삭제하지 말라고 요청. URL secret만 정리 |
| 2026-10-07 | 개인 smoke 상한을90초로 조정, 기본60초 유지. 늘보군 눈물 높이와 무음 표본 초기 상태 보정 | 첫 개인 실행은 모션/화면까지 렌더했으나 마지막 작은 창 검사 중60초 상한에 종료. 캡처에서 눈물 시작점이 눈보다 위에 있어 캐릭터별 좌표 조정 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-07 | 지도 | main/origin 동기화와 clean 상태 확인. plan012 worktree 생성. 기존 SDK와 늘보군 시트 확인. |
| 2026-10-07 | 제작 | README의84개/PNG6장 기록을97개/36장과 표정 슬롯 기능으로 동기화. 주석의 시간·좌표·저장/종료·키 범위를 보완. 정적 검사 성공 문구의 픽셀 정렬 주장을 헤더 검사로 정정. |
| 2026-10-07 | 검증 | 초기 fixture 컴파일에서 private StopAsync 접근 오류를 발견하고 기존 StopCommand로 수정. 재실행한 locked restore/format/analyzer/Release build(경고0/오류0)/Core97/97 통과. C#28파일의XML doc93블록, XAML4개, Python4개 구문 통과. |
| 2026-10-07 | 검증 | src/tests 전체35 C#파일의XML doc100블록 구문 통과. 93a74e5 기본Windows37611971009(97/97, smoke failures0/명명 검사200개,4크기×45컨트롤) 및OBS37611970944(WGC green/chroma/native alpha 각7프레임) 통과. Actions 런타임 경고 제거 뒤 다시 실행한다. |
| 2026-10-07 | 검증 | 1d8cd05 최종Windows37612609047/OBS37612608423 모두 통과. Windows Core97/97, format/analyzer/Release/publish/WPF smoke 실패0. workflow annotation·의존성 deprecation·빌드 경고/오류0 확인. 로컬 증거 root artifacts/qa/plan012/summary.json. |
| 2026-10-07 | 심사 | 기본 QA 이후 중간 자체 리뷰: README 기존 링크 보존·97개/36장·12슬롯/F12·저장 경계 확인, 오디오 production 실행문 변경 없음. 새 개인 fixture는 임시 저장소/QA메모리만 사용하고 원본 보존. 기본 PNG의 실제 슬픔/눈물과480×320 화면 확인. 최종 리뷰와 완료 판정은 늘보군 실행 이후로 남김. |
| 2026-10-07 | 검증 | 사용자 승인 후 private draft의 단일 자산 만료 URL로37636070658 실행. Core97/97/Release 경고0, 늘보군 여섯 상태·미리보기·두 방송창 PNG 확보. 기존60초 상한으로 마지막 배치 시험 중 timeout. 시험 항목을 줄이지 않고 개인 상한90초로 수정. 늘보군 원본 SHA256/manifest 일치 확인, 사진에서 눈물 시작 좌표 조정. |

## Completion Notes

기본 자동 QA·주석·README 점검은 통과했다. [최종 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37612609047)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37612608423)의 로그/PNG/JSON을 로컬에 수집했다. OBS WGC는 통과하며 BitBlt 유효 프레임 없음은 기존 runner 제한이다.

초기 개인 전송은 자동 승인 검토에서 거부되었으나 사용자의 명시적 임시 전송 승인을 받았다. 늘보군을 삭제하지 말라는 추가 요청에 따라 원본·private draft asset·시험 증거·직접 시험용 패키지를 보존한다. Windows 개인 화면 시험을 진행 중이며 최종 리뷰와 main 통합은 성공 확인 이후다. 실제 물리 마이크·키보드·DPI와 사용자 OBS 실기는 별도 미실행 항목이다.
