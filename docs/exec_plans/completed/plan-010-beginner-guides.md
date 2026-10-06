# plan-010-beginner-guides

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

코드를 모르는 사람도 사용할 수 있도록 사용·설치·설정 방법을 별도 Markdown 파일로 작성한다.

## Goal

다운로드부터 압축 풀기·실행·마이크 설정·캐릭터 조정·종료까지 마우스로 따라 하는 초보자 안내 3개를 작성한다.

## Non-Goals

앱 기능/빌드/배포 방식 변경, 새 Release 발행, 사용자 시스템 설정 변경, 실제 마이크/OBS 실기 검증은 제외한다.

## Context Map

- docs/guides/installation.md, usage.md, settings.md
- README.md, docs/user-guide.md, docs/quality/windows-checklist.md
- src/VoxPet.App/Views/MainWindow.xaml, ViewModels/MainViewModel.cs, Services/SettingsStore.cs
- .github/workflows/windows.yml, 현재 main Windows QA의 VoxPet-win-x64 배포 artifact
- Microsoft 마이크 권한·Windows UI 공식 안내, GitHub artifact 다운로드 공식 문서

## Constraints

No Exec Plan, No Work. codex/plan-010-beginner-guides / .worktree/plan-010-beginner-guides 사용.
root Markdown 추가 금지. 기존 문서는 새 안내로 연결하는 최소 추가만 수행한다.
개발 도구/명령어 실행을 사용자에게 요구하지 않는다. 실제 UI 라벨을 그대로 쓰되 뜻을 함께 설명한다.
서브에이전트 없이 QA → 자체 리뷰 → completed/리뷰 미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] 실제 다운로드 경로와 압축·실행·첫 확인을 installation.md에 작성한다.
- [x] 일상 사용·마이크 교체·방송창·종료·문제 해결을 usage.md에 작성한다.
- [x] 권한·프리셋·반응 조정·PNG·OBS·저장/복구를 settings.md에 작성한다.
- [x] README/기존 설명서 연결과 문서 검증, 자체 리뷰 및 완료 기록을 수행한다.

## QA Plan

python3 harness/scripts/verify_base.py, python3 harness/scripts/verify_app.py, git diff --check.
링크·파일명·UI 라벨·설정 저장 범위를 소스와 대조하고 다운로드 artifact 존재를 확인한다.
문서만 변경하므로 .NET 빌드와 앱 실기 시험을 새로 실행하지 않는다. 기존 상태를 검증 완료로 바꾸지 않는다.

## Review Plan

QA 후 단일 에이전트 자체 리뷰 완료. 다운로드 전제·선택할 파일·클릭 순서·성공 확인·막혔을 때의 조치·실제 UI/저장 범위를 검토했다. 독립 리뷰나 Windows 실기 실행으로 주장하지 않는다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-06 | 설치·사용·설정을 docs/guides/의 3개 파일로 분리하고 읽는 순서를 연결한다. | 필요한 단계만 쉽게 찾게 한다. |
| 2026-10-06 | 현재 공개 Release가 없어 성공한 main Windows QA의 VoxPet-win-x64 다운로드를 안내한다. | 존재하지 않는 설치 파일/Release 링크를 안내하지 않는다. GitHub 로그인이 필요함을 설명한다. |
| 2026-10-06 | 외부 UI 경로는 Microsoft/GitHub 공식 자료와 현재 저장소 정보를 확인한다. | 설치/권한 안내 정확성을 확보한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-06 | 지도 | main 깨끗함·origin/main 동기화. 공개 taejun9/VoxPet 확인. 공개 Release 없음. main Windows37414425676 성공 확인. |
| 2026-10-06 | 정리 | installation/usage/settings 3문서를 작성하고 README/전체 설명서에서 읽는 순서대로 연결했다. 외부 안내의 공식 자료 5개를 확인/기록했다. |
| 2026-10-06 | 검증 | Windows37414425676의 VoxPet-win-x64 artifact가 만료되지 않았음을 API로 확인. 문서38개 구조/링크/출처, 자산 검사, diff 통과. 신규 안내의 로컬 heading anchor와 UI 라벨 및 명령어 단계 없음 검사 통과. |
| 2026-10-06 | 심사 | QA 이후 자체 리뷰. OBS/PNG 선택 기능을 구분하고 Stop/음소거/외부 OBS 오디오 차이와 설정 저장/복구를 확인했다. 새 문서 제목의 선택사항 표기를 괄호로 바꾸어 링크 anchor와 일치시켰다. |


## Completion Notes

- 완료: docs/guides/installation.md, usage.md, settings.md. 처음 사용하는 사람이 프로그래밍/명령어 없이 마우스 조작으로 따라 하도록 작성했다.
- 설치: Windows 컴퓨터 종류 확인, GitHub 로그인/실제 실행용 ZIP 항목/만료 대응, 전체 압축 풀기, .exe 찾기, 첫 데모 확인, 업데이트/삭제.
- 사용: Start/Stop의 뜻, 성공 상태 확인, 프리셋, 마이크 교체, 입 음소거, 방송창 조작, 개인 PNG, 종료/재실행, 증상별 대응.
- 설정: Windows 마이크 권한/장치·볼륨, 프리셋 중심 조정, 슬라이더의 쉬운 뜻/변경 예시, 고급 입력 범위, 캐릭터 제작 요청 문구, OBS 연결, 정상 저장 범위와 파일 내용 편집 없는 복구.
- 연결: README와 docs/user-guide.md에 초보자 문서 읽는 순서 추가. 기존 본문을 보존했다. Microsoft/GitHub 공식 자료를 출처 레지스트리에 추가했다.
- QA: verify_base.py/verify_app.py/git diff --check 통과. 신규 문서 anchor/실제 UI 라벨/명령어 단계 없음/Markdown 변경 범위 확인. 2026-10-06 Windows37414425676 실행용 artifact 존재·미만료 확인.
- 자체 리뷰: 중요 미해결 발견 없음. 앱 코드·자산·패키지·workflow를 변경하지 않았다. 실제 마이크/OBS 실기·새 .NET 빌드를 수행하지 않았고 기존 검증 상태를 변경하지 않았다.
- 완료 기록 및 리뷰 미러를 추가한 뒤 문서 QA를 다시 수행한다. main 병합/push/branch -d/worktree 제거는 저장소의 Git 순서를 따르며 실패 시 중단한다.
