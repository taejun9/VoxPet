# plan-008-character-usability

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

수요가 있는 기능과 사용 편의를 개선하고 첨부 늘보군으로 말하는 캐릭터를 시험한다.

## Goal

로컬 투명 PNG 시트 불러오기와 기본 캐릭터 복원, 반응 프리셋, 캐릭터 음소거를 구현하고 늘보군 합성 데모를 Windows에서 검증한다.

## Non-Goals

음성 생성/STT/저장/원격 전송, 자동 마이크 시작, 외부 캐릭터의 공용 기본 배포, 실제 마이크 실기 통과 주장은 제외한다.

## Context Map

- src/VoxPet.App/ViewModels/MainViewModel.cs, CharacterViewModel.cs
- src/VoxPet.App/Views/MainWindow.xaml, App.xaml.cs, LayoutQa.cs
- src/VoxPet.App/Services/SettingsStore.cs
- tests/VoxPet.Core.Tests/, harness/scripts/qa.ps1
- docs/user-guide.md, docs/quality/windows-checklist.md

## Constraints

No Exec Plan, No Work. codex/plan-008-character-usability 및 .worktree/plan-008-character-usability 사용.
UI 스레드에서 파일 디코딩을 하지 않는다. 오디오는 계속 로컬 처리하고 입력 장치/개인 자산 경로를 영구 설정에 기록하지 않는다.
기본 CC0 자산은 보존한다. 늘보군 생성물은 무시된 artifacts/의 개인 시험 팩으로 보관하며 라이선스를 CC0로 주장하지 않는다.

## Implementation Plan

1. 투명 늘보군 3×2 스프라이트 시트를 생성하고 픽셀·상태를 확인한다.
2. 안전한 로컬 시트 로더, 반응 프리셋과 음소거 UI 및 상태를 구현한다.
3. Core 회귀와 WPF smoke/레이아웃 및 개인 시트 Windows 시험을 수행한다.
4. 문서 갱신, 자체 리뷰, 완료 이동 및 리뷰 미러 후 main 병합·push·정리한다.

## QA Plan

- verify_base.py, verify_app.py, git diff --check
- locked restore, format/analyzer, Release build, Core tests
- Windows publish/smoke: 상태 6개, 잘못된 파일 후 기존 캐릭터 보존, mute/unmute/Stop, 프리셋/reset, 작은 창 레이아웃
- 개인 늘보군 팩으로 Windows 합성 반응/눈깜빡임 PNG와 결과 JSON 확보
- 실제 마이크 및 사용자 OBS 환경은 별도 미실행으로 기록

## Decision Log

- 2026-10-06: 사용자 요청의 말하는 캐릭터는 음량에 따라 입 모양이 반응하는 PNG 캐릭터로 해석. TTS는 추가하지 않는다.
- 2026-10-06: 권리 미확인 첨부 캐릭터는 로컬 개인 팩으로 시험하며 기본 배포에 포함하지 않는다. 재시작 때 자동으로 외부 파일을 읽지 않는다.
- 2026-10-06: 기본 PNG 대신 3열(닫힘/중간/열림)×2행(일반/눈감음) 단일 시트 불러오기를 채택한다.

- 2026-10-06: 생성 시트의 셀별 수평/바닥 위치 차이를 확인해 로더에서 알파 영역 기준 중앙/바닥 정렬을 추가한다. 각 상태의 크기는 보존한다.

- 2026-10-06: Windows 개인 시트 QA는 일시적인 비공개 draft release에서만 입력을 가져오는 수동 workflow 경로를 사용한다. 소스 PNG를 Git 추적/기본 배포에 넣지 않고 로컬 증거 확보 후 임시 자산을 정리한다.

- 2026-10-06: Windows CI에는 커밋된 브랜치가 필요하므로 로컬 QA 이후 checkpoint 커밋/push를 허용한다. 전체 Windows QA와 자체 리뷰를 완료한 뒤 완료 기록 커밋 및 main 통합을 진행한다.

- 2026-10-06: 진행 중 캡처 상태를 캐릭터 불러오기 안내가 덮어쓰지 않도록 별도 CharacterStatus 영역을 추가한다.

- 2026-10-06: 최초 개인 시트 dispatch는 read-only 토큰으로 draft를 조회할 수 없어 입력 단계 실패(37337835219). GitHub 공식 문서 https://docs.github.com/en/rest/releases/releases#list-releases 에 따라 push 접근이 필요하다. contents:write를 추가하는 초안은 자동 승인 검토가 거부해 폐기했다. 모든 workflow는 contents:read를 유지한다. 생성한 draft의 단일 파일을 읽는 만료 URL만 임시 Actions secret으로 전달하고 QA 후 secret/draft/임시 화면 artifact를 정리한다. 장기 계정 토큰은 전달하지 않는다.

- 2026-10-06: 새 수동 workflow는 default branch에 등록되기 전 dispatch할 수 없어 404였다. 기존 windows.yml의 수동 입력(use_personal_sheet)에 단일 파일 URL 읽기 경로를 넣는다. 일반 push QA는 이 입력을 사용하지 않고 모든 권한은 contents:read다.

- 2026-10-06: Windows 화면 QA에서 늘어난 controls 높이 때문에 미리보기 Border가 Auto 행 중앙으로 밀려 하단이 첫 화면에서 잘리는 회귀를 발견했다. PreviewCard를 상단 정렬하고 넓은 창에서 카드 전체가 viewport 안에 있는지 실제 레이아웃 검사를 추가한다.

## Progress Log

2026-10-06: 로컬 문서·기본 자산 검사 통과. SDK 10.0.401 locked restore 및 Release 교차 빌드 통과. Core 84/84 통과. 기본 sandbox의 NuGet/IPC 제한은 승인된 실행으로 재검증했다. checkpoint d481b45의 기본 Windows 37337747211 및 OBS 37337747089 통과. 개인 시트 QA 입력 실패는 위 Decision Log에 기록했고 전용 workflow로 재시험한다.

## Review Plan

전체 QA 후 단일 에이전트 자체 리뷰 완료. 요구사항/회귀/스레드/오디오 수명/개인정보/라이선스/CI 권한과 실제 PNG를 확인했다. 별도 에이전트는 실행하지 않았다. 리뷰 미러는 docs/reviews/plan-008-character-usability-review.md에 둔다.

## Completion Notes

- 완료: 로컬 PNG 시트/기본 복원, 반응 프리셋, 캐릭터 음소거, 별도 캐릭터 안내, 넓은 창 미리보기 정렬.
- QA: verify_base/verify_app/diff 통과, locked restore/format/analyzer/Release 경고·오류0, macOS 및 Windows Core84/84.
- 최종 코드 f5f1757: 기본 Windows37338899300, 늘보군37338981751, OBS37338899198 모두 성공. WPF smoke failures0, 4크기×21컨트롤/3startup bounds/휠/미리보기 검사 통과.
- 늘보군: 내장 imagegen 시트1536×1024 RGBA, 입3×눈2, 실제 Windows 합성 반응 및 PNG 확인. 최종 프롬프트/출처/PNG는 무시된 artifacts/characters/neulbo/에 보존한다.
- OBS 기본 캐릭터 WGC: green+Chroma Key/native alpha65.08%/7프레임 변화 통과. BitBlt는 유효 프레임 없음.
- 임시 단일 이미지 URL secret/draft와 수집한 개인 화면 artifact는 정리했다. 로컬 증거와 개인 실행 ZIP을 artifacts/qa/plan008/ 및 artifacts/VoxPet-win-x64-neulbo-plan008.zip에 보존한다.
- 제한: 실제 마이크/권한/제거/물리 DPI/사용자 GPU/실제 입력 장시간 미실행. 늘보군의 공개 배포 이용 권리는 미확인으로 유지하며 기본 공개 자산을 교체하지 않는다.
- 사용자 README와 기본 CC0 PNG는 보존했다. 완료 문서/리뷰 검증 후 main fast-forward 병합/push, branch -d, worktree 제거 순서로 통합한다.
