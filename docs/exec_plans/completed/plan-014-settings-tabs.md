# plan-014-settings-tabs

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

흰색 셀렉트 박스의 글씨를 검정색으로 변경. 기본 캐릭터를 늘보군으로 변경. 설정 조작은 상단 탭으로 나누어 스크롤 없이 선택한 항목만 표시. 추가 요청: 방송창 크기 조절.

## Goal

선택값과 펼친 목록의 가독성을 확보하고 기존 명령·바인딩을 유지한 탭 화면을 제공한다. 기존 늘보군 시트를 변경하지 않고 기본 복원과 시작에 적용한다.

## Non-Goals

오디오 엔진·캡처·음성 저장/전송 변경, 새 캐릭터 제작, 사용자 기존 슬롯 덮어쓰기.

## Context Map

- src/VoxPet.App/App.xaml, Views/MainWindow.xaml 및 code-behind: 색상과 레이아웃.
- ViewModels/CharacterViewModel.cs 및 ExpressionViewModel.cs: 기본 캐릭터와 슬롯.
- Views/LayoutQa.cs, CharacterQa.cs: Windows 합성 QA.
- artifacts/characters/neulbo/늘보군.png: 기존 개인 자산(원본 보존).
- docs/quality/development.md, rules.md, docs/privacy/principles.md: QA와 자산 권한.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-014-settings-tabs` / `.worktree/plan-014-settings-tabs` 사용.
QA → 자체 리뷰 → completed 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.
라이선스 미확인 개인 PNG는 공개 저장소에 포함하지 않고 개인 실행 패키지에 기본 자산으로 둔다. 사용자는 개인 실행용 기본 설정만 요청했으므로 공개 자산에 포함하지 않는다.

## Implementation Plan

- [x] ComboBox 선택값·항목용 검정 TextBlock/Foreground 스타일.
- [x] 상단 탭과 스크롤 없는 화면, 작은 창에서 내용 크기 조정.
- [x] 기존 개인 늘보군의 기본 시작/복원 경로와 기존 슬롯 보존.
- [x] QA fixture와 사용자 안내 동기화.
- [x] QA, 자체 리뷰, Git 수명 완료 및 실행 패키지 제공.

## QA Plan

macOS: verify_base.py, verify_app.py, git diff --check, SDK10.0.401 locked restore, format/analyzer, Release build, Core tests.
Windows: qa.ps1 -Publish -Smoke로 각 탭 접근/전환·전체 조작 가시성·스크롤 없음·선택값/팝업 검정 글씨를 확인. 캐릭터 불러오기/복원/표정/창 수명 회귀 수행. Windows CI 증거 PNG를 직접 확인. 물리 마이크/DPI는 이 작업의 합격 근거에 포함하지 않는다.

## Review Plan

QA 이후 동일 에이전트 자체 리뷰. 세 요구사항, 기존 명령과 상태 유지, 슬롯 보존, 자산 경계, 작은 창 가시성 및 문서 확인.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-08 | 다섯 탭에 기존 바인딩을 분배하고 작은 창에서는 DownOnly 크기 조정 | 스크롤로 해결하지 말라는 사용자 요청. UI/렌더러와 오디오 분리 유지 |
| 2026-10-08 | 기본 PNG는 실행 폴더의 선택적 캐릭터 파일로 지원 | 개인 자산의 공개 라이선스는 기존 문서상 미확인. 원본·기존 슬롯 보존 |

| 2026-10-08 | 사용자 답변에 따라 개인 패키지에만 늘보군 포함 | 공개 저장소/공용 배포에 포함하지 말라는 선택 |
| 2026-10-08 | 방송창 너비·높이 조절과 우클릭 프리셋 추가 | 사용자 추가 요청. 기존 invisible native grip 유지 |

| 2026-10-08 | 하위 탭으로 조작을 세분화하고 ItemTemplate에 검정 글씨 직접 지정 | 첫 Windows 검사305개 통과/선택 텍스트24개 실패. 생성 텍스트의 스타일 우선순위를 피하고 480×320에서 지나친 축소를 개선 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-08 | 지도 | main clean/origin 동기화 확인. Git 메타데이터 sandbox 제한은 승인된 Git 명령으로 정상 처리. plan014 전용 worktree 생성 |

| 2026-10-08 | 검증 | 문서47개/36자산/locked restore/format(analyzer 포함)/Release 경고0·오류0/Core110개 통과. 초기 sandbox 네트워크·IPC 제한은 승인된 실행으로 해결 |

| 2026-10-08 | 검증 | 첫 Windows37767394347: restore/format/build/Core110개/publish 통과. smoke에서 셀렉트 텍스트24개 실패를 확인하고 수정. 탭 전환/가시성/방송 크기/기본 복원 검사 통과. 실제 작은 창에서 과도한 축소를 발견해 하위 탭으로 세분화 |

| 2026-10-08 | 검증 | 최종 Windows37768105239: Core110/110/format/analyzer/Release/publish 경고0·오류0/385고유 검사·실패0. 4크기 모든 상단/하위 탭의 가시성·스크롤 없음·검정 선택값/팝업/편집 유지 통과. 실제 PNG 직접 확인 |
| 2026-10-08 | 검증 | OBS37768105246: WGC/Chroma Key/native alpha65.08%/7프레임 통과. 개인 실행 ZIP13파일/CRC/기존 PNG SHA256 일치. 개인 이미지는 공개 전송 없음 |
| 2026-10-08 | 심사 | QA 이후 자체 리뷰: 기존 명령/편집 잠금/오디오·저장 경계/기존 슬롯/OBS 수명/문서 확인. completed 이동과 리뷰 미러 작성 |

## Completion Notes

요청한 검정 셀렉트 글씨/개인 늘보군 기본/스크롤 없는 상단 탭/방송창 크기를 구현했다. 최종 실행코드 a81bc4e의 Windows37768105239 및 OBS37768105246 통과. Core110/110, format/analyzer, Release/publish 경고·오류0, Windows smoke 실패0/고유385검사. 탭별 PNG에서 검정 선택값과 작은 창의 조작 가시성을 직접 확인했다.

QA 이후 자체 리뷰를 완료하고 [리뷰 미러](../../reviews/plan-014-settings-tabs-review.md)를 작성했다. 개인 실행 ZIP은 root artifacts/VoxPet-win-x64-neulbo-plan014.zip이며13파일/ZIP CRC/원본 SHA256 일치 확인. 개인 PNG는 저장소에 커밋하거나 CI에 전송하지 않았다. 기존 저장 슬롯은 보존한다. 작은 창은 내용 크기를 줄이고 크기 설정은 이번 실행만 유지한다. 실제 마이크·물리 DPI/입력·사용자 OBS 실기와 이번 개인 패키지의 실제 Windows 시작 화면은 검증하지 않았으며 기존 체크리스트와 구분한다.
