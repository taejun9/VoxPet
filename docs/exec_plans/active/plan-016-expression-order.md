# plan-016-expression-order

## Status

active

## Owner

project_lead / plan_keeper

## User Request

방송창 우클릭 메뉴 글씨를 검정색으로 변경. 사용자가 표정 순서를 변경. 늘보군12개 표정을 모두 다르게 만들고12종 전체를 테스트할 수 있도록 제공.

## Goal

검정 우클릭 메뉴/저장되는 위아래 표정 이동/변경 순서에 맞는F1~F12/서로 다른12표정×8입×2눈=192상태와 개별·전체 테스트 화면. 기존 저장 슬롯과 PNG를 자동 덮어쓰지 않는다.

## Non-Goals

음성 감정 인식, 녹음·원격 음성 전송, 개인 PNG 공개 Git/CI 배포, 기존 캐릭터 원본/이전 ZIP 삭제.

## Context Map

- Core ExpressionProfile/ExpressionSlotStore와 새 순서 저장소:12표정 정의/원자적 순서 파일.
- App ExpressionViewModel/MainViewModel: 순서/단축키/테스트/데모 수명.
- Views MainWindow/CharacterWindow 및 QA: 편집 UI/메뉴 색/합성 검증.
- artifacts/characters/neulbo-plan015/: 기존6표정 보존·재사용.
- imagegen: 부끄러움/뿌듯함/갸우뚱/신남/애정/장난6종 추가. 개인 자산은 root artifacts/characters/neulbo-plan016/.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-016-expression-order` / `.worktree/plan-016-expression-order` 사용.
QA→자체 리뷰→completed/리뷰 미러→main 병합/push→branch-d→worktree 제거.
개인 실행용 범위와 원본 보존 유지. UI 밖에서 PNG IO. 기존3열/8열·표정 숫자0~5 호환. 순서는 저장 ID를 이동하는 별도 매핑으로 저장하여 PNG/슬롯 파일을 바꾸지 않는다.

## Implementation Plan

- [x] 방송창 메뉴의 실제 헤더 템플릿 검정 글씨.
- [x] 순서 매핑 저장/복구·위아래 이동·F키 대응·편집 초안 유지.
- [x]12표정 종류와 개인12가족, 새6개 얼굴 자산 제작/통합.
- [x] 개별12종 미리보기·전체 순회/취소·원래 화면 복원·마이크 없는 데모.
- [ ] Core/Windows/OBS QA, 문서, 리뷰, 개인 ZIP 및 Git 수명.

## QA Plan

macOS verify_base.py/verify_app.py/diff --check/SDK10.0.401 locked restore/format/analyzer/Release/Core tests.
Core:12기본값과 기존Kind숫자, 순서 순열·원자적 저장·손상/실패 복구. Windows: 메뉴 팝업 실제 검정 글씨/PNG, 이동·재시작·단축키 대응·저장 파일/초안 보존,12개 고유 기본 미리보기/전체 순회·취소·종료·데모 수명,192합성 상태와 탭 가시성. OBS 캡처 회귀.
개인 PNG는 CI에 보내지 않고 로컬 얼굴 차이/투명/192프레임·해시/ZIP 검사로 구분한다.

## Review Plan

QA 이후 동일 에이전트 자체 리뷰. 세 요구사항/기존 슬롯·PNG/단축키 의미/원자성/테스트 수명·오디오 경계/자산과 문서 확인.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-09 | 위치와 저장ID를 분리한12항목 순열, 위아래 이동을 즉시 저장 | 파일 교체 없이 안전하게 순서/F키를 변경 |
| 2026-10-09 | 기존6종에 부끄러움/뿌듯함/갸우뚱/신남/애정/장난 추가 |12개 실제 다른 얼굴. 기존Enum0~5 유지 |
| 2026-10-09 | 테스트 탭에서 저장 PNG와 별개로 기본12종을 시험, 전체 순회는 취소/복원 가능 | 기존 사용자 저장값을 바꾸지 않고 새 얼굴을 전부 확인 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-09 | 지도 | main clean/origin 동기화 확인. plan016 worktree 생성. 기존 메뉴·슬롯 저장·단축키·개인6자산 확인 |

| 2026-10-09 | 제작 | 메뉴 헤더 검정/저장ID 기반 이동/12종 개별·전체·취소/복원/숫자 데모 구현. 새6종 imagegen과 기존6종 보존,352px셀192고유상태 검증 |
| 2026-10-09 | 검증 | locked restore/format 검증/Release 빌드0경고0오류/Core140통과. Windows/OBS 실행 준비 |

## Completion Notes

Windows 첫 실행37888923645:507통과, 기존 F12 기본 이름을 "표정12"로 가정한1개 assertion 실패. 실제 슬롯 저장 이름으로 검사하도록 fixture를 수정한다. OBS37888923685 통과.192상태 전체 매핑 및 기존 데모 보존도 보강하여 최종 재실행한다.

구현·생성·QA 진행 중. 개인 원본·이전 ZIP과 사용자 저장값을 보존한다.
