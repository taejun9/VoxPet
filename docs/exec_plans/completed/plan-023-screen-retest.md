# plan-023-screen-retest

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

모든 테스트를 다시 전부 실제 화면에서 수행한 뒤 다시 제공. 사용자는 GitHub Windows 화면 시험으로 진행하고 개인 PNG는 외부로 전송하지 않는다고 명시했다.

## Goal

기존 문서/자산/locked restore/format/Release/Core와 Windows WPF 전체 검사·일반/서명 실행·OBS를 다시 수행한다. 실제 표시된 Windows 설정창/방송창의 화면 캡처로7탭/하위 페이지×4창크기·12표정·5손동작/의상 연결·머리/입 장식·재실행/트레이/배경을 확인한다. RenderTargetBitmap 합성과 데스크톱 화면 캡처를 구분하고 후자를 실패 시 대체하지 않는다. 모든 화면을 직접 시각 검토하고 발견한 회귀를 수정/재검증한다. 재검증한 EXE와 변경되지 않은 로컬 개인 PNG로 새 ZIP을 만든다.

## Non-Goals

개인 PNG/설정/음성 외부 전송, 물리 마이크/USB 제거/실제 모니터 DPI/사용자 GPU 시험을 GitHub 합성으로 주장, 이전 ZIP/원본 삭제, 자동 인증서 신뢰 설치, 외부 개인 캐릭터 생성.

## Context Map

MainWindow/App smoke·LayoutQa/CharacterSelectionQa/SettingsPersistenceQa/HandMotionQa, ScreenEvidence desktop capture, qa/startup/OBS 워크플로와 화면 증거, 로컬 개인 ZIP/원본 검증.

## Constraints

codex/plan-023-screen-retest / .worktree/plan-023-screen-retest. QA→단일 에이전트 자체 리뷰→completed/리뷰 미러→main ff-only/push→branch-d/worktree 제거. 서브에이전트 없음. public/합성 PNG만 Git/CI 사용. 음성 기록/전송 없음. 화면 캡처는 일회용 Windows runner 앱 창 영역만 저장한다.

## Implementation Plan

- [x] 실제 HWND/desktop pixels의 화면 캡처와 메타데이터/실패 증거 추가.
- [x] 기존 전체 WPF/일반 시작·서명/OBS 검사 재실행 및 화면 검토.
- [x] 모든 기존 테스트/화면 증거 목록과 합성·실기 구분 기록.
- [x] 검증된 새 ZIP/원본16PNG 보존·서명/CRC, 자체 리뷰/완료 Git 절차.

## QA Plan

python3 harness/scripts/verify_base.py, verify_app.py, git diff --check, locked restore/format --verify-no-changes, Release 경고0·오류0, Core177 전체. Windows qa.ps1 -Publish -Smoke, unsigned/signed 일반 프로세스9사례씩, 실제 desktop 캡처와 원래 WPF 렌더에 공용 고양이/fixture 표식 존재·빈/단색/잘린 화면 거부, HWND/OS/bounds/크기/모드/시간 메타데이터 확인.7탭 및 모든 하위 페이지×4창크기, 표정12종/입·눈/눈물, 손5종/의상 머리·입·소매 연결, 실제 재실행/창 크기·위치/트레이/같은 방송 HWND. 추가 데스크톱 캡처 시간을 위해 smoke 기본180초/개인210초로 조정하며 기존 검사·통과 기준 유지. OBS WGC/alpha/같은 source·HWND·장시간10분 warmup+60분 측정(RSS50MiB/핸들50 이하·매분 프레임 변화) 재실행. 개인 PNG Windows 실기/물리 마이크는 사용자 선택에 따라 이번 CI 범위 제외로 기록한다.

## Review Plan

QA 이후 같은 에이전트 자체 리뷰. 실제 화면 캡처·합성 렌더 구분, 화면 보호 범위/파일 이름/실패 차단, 기존 앱 동작·설정 불변, 공유 renderer/프레임 검토, 개인 자산 외부 전송 없음과 모든 테스트 실행 증거를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-12 | 화면 장면의 의상도 실제 Wardrobe.ImportAsync 경로로 추가/착용 | 첫 화면 시험은 종료 시 수동 fixture 항목의 WearCommand 미설정으로 NullReferenceException/오류 UI 대기 발생. 제품 생성 경로로 전체 수명을 검증하도록 수정 |
| 2026-10-12 | GitHub Windows 화면 시험/개인 PNG 로컬 보존 | 사용자 명시 선택. macOS에는 Windows 화면 연결 없음 |
| 2026-10-12 | 표시된 앱 HWND의 실제 데스크톱 픽셀 캡처, 기존 RenderTargetBitmap 결과 병행 | 사용자 요청 실제 화면 검증과 이전 자동 렌더 검사 구분 |
| 2026-10-12 | 장시간 OBS는 main1939f3c, 화면·배포는 c7d2f0f에서 시험 | diff 확인 결과 plan023의 앱 변경은 QA 전용 helper/guard/fixture뿐이며 정상 renderer/audio/settings 동작은 동일 |
| 2026-10-12 | 장시간 OBS60분 측정도 다시 실행 | 사용자가 모든 테스트 재실행을 명시 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-12 | 제작 | desktop SRCCOPY/CAPTUREBLT와 foreground HWND/색상/PNG CRC/모든 장면 coverage, 일반/서명 실행 화면, 표정·손·의상/머리·입 장면 추가. 기존 기능 검사/개인 자료 경계 유지. 장시간 OBS38252444387 실행 시작 |
| 2026-10-12 | 지도 | main1939f3c clean/origin 동기화, macOS 화면 inventory 확인. 사용자 CI 방식/개인 PNG 외부 전송 금지 답변 반영 |
| 2026-10-12 | 검증 | Windows38255728224 Core177/WPF1346·서명/일반9사례씩·실제 화면225장 전수 시각 검토 통과. 첫 fixture 종료 오류 수정 후 전체 재실행. short OBS38255728219 통과. 장시간 결과는 다음 행 참조 |
| 2026-10-12 | 심사 | long OBS38252444387 60.048분/RSS-2506752bytes/핸들-2/프레임67회 통과. QA 후 자체 리뷰/ZIP32파일 CRC·서명/16PNG byte-identical 확인 |

## Completion Notes

전체 QA/225장 전수 시각 검토/QA 이후 자체 리뷰 통과. 장시간 OBS 60.05분 측정·RSS 증가-2.39MiB·핸들 증가-2·프레임 변화67회. 새 개인 ZIP32파일·CRC/SHA256/서명·기존16PNG 바이트 보존 통과. Windows/short OBS는 c7d2f0f, long OBS는 동일 정상 동작 main1939f3c. 개인 PNG/물리 장치 실기는 별도 범위로 기록했다. main ff-only 통합/push 후 일반 branch-d/worktree 제거 절차를 수행한다.
