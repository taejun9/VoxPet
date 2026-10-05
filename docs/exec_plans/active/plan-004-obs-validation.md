# plan-004-obs-validation

## Status

active

## Owner

project_lead / harness_builder / quality_runner

## User Request

전체 기능이 실사용 가능하도록 버그 검증을 계속한다. OBS 캡처의 실행 근거를 확보한다.

## Goal

실제 OBS Windows 프로세스가 VoxPet 합성 데모의 방송창을 캡처하고 초록 배경 제거/캐릭터 변화/설정창 최소화 중 캡처를 검증하는 하네스를 만든다.
가능한 경우 장시간 합성 데모의 메모리/핸들 유지도 측정한다. 실제 마이크와 실제 사용자 GPU/OBS 환경의 미검증 조건은 유지한다.

## Non-Goals

실제 음성 저장/전송, OBS 영상 녹화/방송, 카메라/실제 마이크 자동 시작, 사용자 컴퓨터의 OBS 설치/설정 변경.
GitHub runner의 그래픽 환경이 불충분하면 그 제한을 기록하며 실제 OBS 성공으로 대체하지 않는다.

## Context Map

OBS 공식 portable/launch/remote-control 설명과 obs-websocket 프로토콜, OBS 공식 release asset.
App 합성 데모, CharacterWindow, Windows QA/배포 명령.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-004-obs-validation` / `.worktree/plan-004-obs-validation` 사용.
QA → 자체 리뷰 → 완료 이동/리뷰 미러 → main ff-only 병합/push → branch -d → worktree 제거.
OBS는 disposable GitHub runner에서만 다운로드/실행하고 loopback RPC만 사용한다. 오디오 source를 비활성화한다.
실패 Git 단계 후 강제 우회 금지. 전체 목표의 실제 마이크/사용자 OBS/DPI/장시간 범위를 축소하지 않는다.

## Implementation Plan

- [ ] 공식 OBS 버전/asset/API 및 runner 환경 확인
- [ ] 마이크 없는 QA 데모 모드와 loopback OBS 테스트 하네스
- [ ] 실제 Window Capture/Chroma Key 스크린샷 및 환경 증거
- [ ] 합성 장시간 자원 검증 가능성 확인 및 실행
- [ ] QA 후 자체 리뷰/문서/완료 또는 제한 기록과 Git 절차

## QA Plan

합성 데모는 숫자 신호만 만들며 마이크를 열지 않는다. OBS에 오디오 input/output, recording, streaming source를 만들지 않는다.
실제 창 식별자는 OBS property API에서 얻으며 VoxPet Character만 선택한다.
캡처 PNG가 빈/검은 프레임이 아니고 초록 배경 및 보라색 캐릭터가 보이며 시간에 따라 캐릭터 픽셀이 변해야 한다.
Chroma Key 적용 후 초록 배경 alpha 제거와 캐릭터 픽셀 유지를 확인한다.
설정창을 최소화해도 OBS의 방송창 픽셀이 유지되어야 한다. OBS/OS/캡처 방식은 결과 JSON, renderer는 선별한 로그, build는 CI commit/배포 SHA256으로 기록한다.
장시간 합성 UI를 수행할 경우 10분 warmup 후 60분 RSS 증가 50MiB 이하, handle 증가 50 이하를 기준으로 한다. 실제 마이크 장시간 합격으로 해석하지 않는다.
기존 Core 70개/WPF smoke, verify_base.py/verify_app.py/diff 검증을 유지한다.

## Review Plan

단일 에이전트 자체 리뷰. OBS 설치/제어는 테스트 runner에 한정되고 생산 앱에 네트워크/오디오 저장 기능을 넣지 않는지 확인한다.
캡처 실패를 app bug/runner 환경 제한으로 구분하고 미검증 항목은 그대로 남긴다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-05 | 실제 OBS를 remote Windows에서 합성 UI와 시험 | 마이크 환경은 없지만 방송 캡처 요구의 직접 실행 근거는 추가 확보 가능 |
| 2026-10-05 | test-only branch push로 prototype 검증 | 새 workflow의 main 통합은 실제 QA/리뷰 이후에 수행 |
| 2026-10-05 | 캡처에서 보인 resize grip을 제거하고 모서리 alpha 검사 추가 | 방송 이미지에 UI 표시가 남지 않도록 실제 OBS 증거에 따라 수정 |
| 2026-10-05 | 검증 브랜치에서 10분 warmup + 60분 측정 실행, 완료 후 기본값 0 복구 | default branch 통합 전 장시간 증거 확보; 일반 CI는 짧은 검증 유지 |
| 2026-10-05 | 투명 창은 원래 resize grip 영역 유지, Opacity=0으로 표시만 제거 | 공식 WPF Window/ResizeGrip 소스의 native hit 처리 확인. Windows smoke에서 실제 WM_NCHITTEST=HTBOTTOMRIGHT를 검사하고 OBS에서 corner 픽셀 검사 |
| 2026-10-05 | 장시간 실행을 유지하며 후속 짧은 QA에 투명 배경 독립 프로세스 추가 | 기존 green 프로세스를 정리한 뒤 WGC 캐릭터/변화를 검사하고 native alpha 보존 여부는 별도 Boolean으로 기록. 투명 alpha를 green/chroma 성공으로 대체하지 않음 |
| 2026-10-05 | 기존 d35f707 장시간 실행은 그대로 두고 다음 push의 기본값을 0으로 복구 | 실행 중인 job의 checkout/인자를 바꾸지 않으며 별도 짧은 green/transparent QA를 먼저 실행. 모든 결과 이후 main 통합 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 지도 | main/원격 동기화 및 plan-004 worktree 생성 |
| 2026-10-05 | 검증 | e41194f: Core/WPF Windows QA 통과. OBS 32.2.2/WS 5.7.4, Windows Server 2025 build26100, Microsoft Basic Render Driver/D3D11에서 WGC 통과, BitBlt 유효 프레임 없음. 7개 서로 다른 프레임, keyed alpha66.0%, green0%, 캐릭터 유지. 실제 PNG 검사 후 resize grip 발견 |
| 2026-10-05 | 제작 | e7fe742의 장시간 실행은 resize 영역 보존 검증을 보완하기 위해 취소. 최종 방식은 CanResizeWithGrip + Opacity=0이며 영역은 유지 |
| 2026-10-05 | 검증 | d35f707 Windows QA [37269646190](https://github.com/taejun9/VoxPet/actions/runs/37269646190) 통과: Core70/빌드/publish/WPF smoke, native resize hit 포함. OBS 장시간 [37269646209](https://github.com/taejun9/VoxPet/actions/runs/37269646209)는 진행 중이며 아직 합격 기록 없음 |
| 2026-10-05 | 검증 | Windows artifact TRX의 total/passed=70, failed=0과 smoke JSON failures=0을 직접 확인. AMD64 PE/배포 필수 파일/ZIP CRC 확인 후 root artifacts/에 plan004-preview 배포본 및 SHA256 요약 보존. 실제 마이크와 장시간 결과는 미검증 상태 유지 |

## Completion Notes

진행 중. 실제 마이크 환경 질의는 미응답이며 전체 실사용 목표는 미완료다.
