# plan-019-wardrobe

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

모자/안경/얼굴장식/귀걸이/상의/하의/겉옷/한벌옷을 이미지로 업로드·로컬 보관하고 클릭 착용, 크기/미세 위치 조정. 앱 단일 실행. OBS 초록/투명 전환 시 기존 방송창 유지. 앱 아이콘 및 김태중 게시자 포함. 사용자 인증서 없음/개인PC용 서명 방법 선택은 유지한다.

## Goal

8분류 의상 보관함/여러 장식 합성·착용/해제·크기·X/Y/순서·즉시 미리보기와 명시 저장·재시작, 미리보기/방송창 공유. 한벌옷과 상하의 선택 배타, 겉옷 공존. 두 번째 실행은 종료하고 기존 창 복원. 방송창 HWND와 OBS 소스를 유지하며 배경 전환. 기본 실행/트레이 아이콘 및 개인 실행용 김태중 서명 패키지/공개 인증서·직접 신뢰 단계 제공. 개인키/PNG는 공개 Git/CI에 포함하지 않는다.

## Non-Goals

음성 저장/전송·웹캠/얼굴 추적, 의상 자동 인식/리깅, 공용 인증기관의 검증된 게시자 사칭, 사용자 PC 인증서 자동 신뢰 등록, 이전 자산/ZIP 삭제.

## Context Map

- Core CostumeProfile/CostumeStore:8분류/배치/원자적 JSON·관리 이미지.
- App CostumeViewModel/이미지 로더/CharacterView:512기준 좌표·frozen 이미지·UI.
- App single-instance 서비스/CharacterWindow: 로컬 단일 실행과 고정 HWND 배경.
- icon 자산/개인 서명 하네스: 아이콘·김태중 자체 서명 검증과 개인 배포.
- Windows smoke/startup/OBS: 저장/복구·레이어·한벌 배타·중복 실행·고정 창 캡처.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-019-wardrobe` / `.worktree/plan-019-wardrobe` 사용.
QA→자체 리뷰→completed/리뷰 미러→main 병합/push→branch-d→worktree 제거.
사용자 원본은 수정하지 않고 관리 복사본 저장. UI 밖 이미지 IO/Freeze,유한 좌표·크기. 기존 설정/슬롯/늘보군12종 보존. 신뢰되지 않은 자체 서명을 검증된 게시자로 표현하지 않는다.

## Implementation Plan

- [x] 8분류 의상 보관함/합성/배치/저장·복구/세부 UI.
- [x] 단일 실행과 기존 창/트레이 복원.
- [x] 고정 방송창 배경 전환 및 OBS 캡처 검증.
- [x] 실행/트레이 아이콘과 개인 서명·인증서 설치 안내.
- [x] Core/Windows 일반 시작·WPF/OBS·서명 QA,개인 ZIP,리뷰/Git 수명.

## QA Plan

verify_base.py/verify_app.py/diff/locked restore/format/Release/Core. Core 저장·순열·배치 범위·손상/원자성/사용자 원본 보존. WPF 의상 위치/크기·합성·저장 재시작·장식 공존/한벌 배타/잘못된 이미지복구·탭 레이아웃. 일반 EXE 중복 실행 종료/기존 tray 복원/재실행. 방송창 배경 전환 HWND 동일·크기/위치/모델 유지. OBS32.2.2 WGC/호환 캡처/크로마 키. EXE 아이콘 및 김태중 Authenticode 서명자/실제 상태/개인키 미포함 검증. 개인 늘보군은 로컬 해시/ZIP 검사.

## Review Plan

QA 이후 동일 에이전트 자체 리뷰. 저장 소유권·이미지 메모리 제한·좌표/레이어,단일 실행 수명,고정 HWND/OBS 한계,서명과 신뢰의 구분,문서·개인 자산 경계 확인.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-12 | 옷을 상의/하의/겉옷/한벌옷 분류. 한벌과 상하의는 배타·겉옷 공존 | 사용자 추가 지시 |
| 2026-10-12 |512좌표의 이미지 레이어로 캐릭터와 같은 대기 이동을 공유 | 창 크기·DPI와 별개로 정밀 조정 |
| 2026-10-12 | 기존 HWND와 native alpha 유지 위해 always-layered 창 선택, WGC 필수·BitBlt 제한을 공개 | 배경 전환 시 새 창 금지 사용자 요청 |
| 2026-10-12 | 일회용 CI에서 NonExportable 코드 서명 키 생성/최종 EXE 서명·공개 CER만 제공/키 삭제 | 실제 개인 서명 패키지와 검증을 제공, 사용자 키와 개인 PNG 전송 없음 |
| 2026-10-12 | 60자 의상 이름을 실제 layout fixture에 넣고 한 줄 TextBox 내부 caret 가로 이동만 허용 | 페이지/목록 스크롤 없음 계약과 긴 텍스트 편집을 구분 |
| 2026-10-12 | 개인 서명 QA의 임시 Root를 CI LocalMachine에 등록/정리하고 단계 로그·5분 상한 추가 | CurrentUser 보호 저장소의 확인 UI 대기 추정. 사용자PC 수동 신뢰 경로와 동일하고 일회용 runner에 한정 |
| 2026-10-12 | 인증서 없음 조건에서는 개인 자체 서명·공개 인증서만 제공,신뢰 설치는 수동 | 요청된 서명자 김태중을 포함하면서 인증기관 검증을 사칭하지 않음 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-12 | 지도 | main clean/동기화 및 plan019 격리. 기존 CharacterView/방송창 재생성/트레이/메타데이터·QA 경로 확인 |
| 2026-10-12 | 제작 |8분류 의상/로컬 관리 저장·동일 renderer 합성, 단일 실행, 고정 HWND, native icon 및 개인 서명 하네스 구현 |
| 2026-10-12 | 검증 | Core151/WPF844, 일반 unsigned/signed 각5사례·중복 복원, OBS 동일 창/소스·alpha, 실제 서명/변조/아이콘 및 개인ZIP 통과 |
| 2026-10-12 | 심사 | QA 이후 같은 에이전트 자체 리뷰. 미해결 기능 결함 없음, WGC·수동 개인 신뢰/대상PC 실기 한계 기록 |

## Completion Notes

코드73c4b34의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38200870611) 통과: locked restore/format/analyzer/Release/publish 경고0·오류0, Core151/151, WPF844고유검사 실패0.8분류/복수 장식/한벌 배타·겉옷 공존/비율 크기·미세 좌표·레이어/관리 복사본/저장·재시작·손상 복구/원본 보존 및6탭×4크기 실제 항목·60자 이름 배치 검증. 일반 인수 없는 unsigned와 signed EXE 각각5사례×10초 유지/정상 종료/기존 슬롯·순서 불변, 중복 실행0종료·기존 트레이 HWND 복원.

[OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38200259371) 통과: OBS32.2.2/Server2025 build26100/Basic Render Driver/WGC 초록·크로마·7프레임 변화·native alpha65.08%. 같은 HWND와 같은 소스에서 초록→투명→초록을 캡처했다. BitBlt는 유효 프레임 없음 제한을 명시했다. OBS 확인 코드41692f8 이후 src 변경 없음.

김태중 실제 Authenticode 서명자, 신뢰 전 UnknownError/일회용 CI LocalMachine Root 신뢰 후 Valid/변조 HashMismatch를 검사했다. finally에서 해당 지문의 Root/개인키 삭제. 공개 CER만 제공하며 인증기관 신원 검증이 아니다. EXE PE에 자체 아이콘7해상도 payload가 원본 ICO와 일치함을 확인했다. 사용자PC 신뢰 등록은 수동이고 실제 게시자 화면·SmartScreen 경고 제거는 보장하지 않는다.

첫 Windows38199786043에서 새2 ComboBox의 선택/팝업 글씨16검사 실패를 재현하고 Black DataTemplate으로 수정했다. Windows38200259395는 WPF844·실패0이나 CurrentUser Root의 확인 UI 대기로 추정되어 서명 단계에서 취소했다. LocalMachine CI 전용 Root/단계 로그/5분 상한으로 수정한 최종 실행에서 전체 통과했다.

개인 ZIP artifacts/VoxPet-win-x64-neulbo-plan019.zip:27파일/93,271,126bytes/CRC 통과, SHA256 a08844c8aa7c317d6fb1a7b90b46864eb2738baa178b77159cad31b7ce9ce291. 기존12PNG/원본 SHA256·이전ZIP 보존. 실제 개인PNG는 CI로 보내지 않았다. 증거 artifacts/qa/plan019/. QA 이후 동일 에이전트 자체 리뷰 완료. 실제 사용자 Windows11 GPU/물리마이크·Shell/개인 인증서 설치는 별도 실기 항목이다.
