# plan-020-cape-hand-motion

## Status

active

## Owner

project_lead / plan_keeper

## User Request

의상에 망토 분류 추가. 손동작 옵션: 항상, 사용자 설정 초 간격, 사용자 설정 핫키. 기존 늘보군 개인 기본/12표정·입8단계/의상·같은 방송창·단일 실행·아이콘·개인 서명 패키지 유지.

## Goal

기존 의상 JSON enum 숫자를 유지하면서 망토 추가 및 저장/재시작/조정. 마이크와 독립적인 손동작 시간축과 항상/간격/핫키·수동 시험/설정 저장. 두 캐릭터 화면에 같은 손 모션을 표시하고 좌클릭 기본 입력/선택형 전역 키의 등록/충돌/재설정/해제를 구현한다. 기존PNG 원본/12표정·8입 상태는 유지하고 별도4프레임 동작 PNG의 몸·손만 렌더한다. 할퀴기/장난감 총/손하트/장난감 칼/요술봉5종을 각각 설정·실행하며 개인 늘보군 동작 이미지를 별도 제공한다.

## Non-Goals

음성 저장/전송·추적, 원본 PNG 덮어쓰기·개인PNG 공개 업로드, 자동 팔 인식/관절 리깅, 기존 의상·슬롯·설정/ZIP 삭제, 인증기관 게시자 신원 확인 사칭.

## Context Map

CostumeProfile/CostumeViewModel 분류, CharacterViewModel/CharacterView 렌더링, Core 손 모션 모델/시간축·설정 저장, configurable native hotkey/창 수명, 손 모션 탭 UI, Core/WPF/hotkey/OBS/일반 실행 QA 및 개인 배포.

## Constraints

codex/plan-020-cape-hand-motion / .worktree/plan-020-cape-hand-motion. QA→동일 에이전트 자체 리뷰→계획 completed/리뷰 미러→main 병합/push→branch-d/worktree 정리. 사용자 보고 한국어 별칭. 서브에이전트 없음. VoiceLevel/마이크 엔진 수정 불필요. PNG 동작4프레임/frozen/vector head mask로 원본 보존. finite 좌표/시간, UI IO/디코딩 분리. 기존 김태중 자체 서명·수동 사용자 신뢰 절차 유지.

## Implementation Plan

- [ ] 망토 append enum/9분류·기존 저장 호환/UI/시험.
- [ ] Core 5종 손동작 스케줄러 always/interval/hotkey, 유한 시간/간격/지연·수동 입력과 저장 모델.
- [ ] 원본 머리·표정 보존/별도4프레임 몸·손 렌더러와 공유 미리보기/방송, 손모션 설정 탭·초/핫키/영역 조정.
- [ ] 전역 핫키 충돌/기존예약키·repeat/재등록/트레이·종료 수명.
- [ ] Core/WPF/일반 시작/OBS/개인 서명 검증·원본 보존 ZIP·자체 리뷰·Git 수명.

## QA Plan

verify_base.py/verify_app.py/diff/locked restore/format/analyzer/Release/Core. 망토 enum 기존숫자/저장·착용·겉옷 공존. 손 시간 경계·finite·반복/interval 변경/핫키 trigger·중복입력·중지·설정 손상/구버전 호환. 실제 WPF 양손만 이동/얼굴·입·의상 보존/공유 렌더/영역 편집과 모드·간격·핫키 설정, 실제 native 등록충돌/WM_HOTKEY·재설정·해제/트레이.7탭×4창크기 검정 선택·팝업 글씨와 접근성. 일반 EXE 5사례/중복 실행, OBS32.2.2 같은 HWND/WGC·배경/모션, 최종 개인 서명/아이콘·ZIP CRC·이전12PNG/원본 해시.

## Review Plan

QA 후 같은 에이전트 자체 리뷰. 기존 enum·저장 호환, 손 mask/영역·renderer 메모리/표정 전환, 시간축·키 충돌/해제·UI 재진입, 개인정보·원본/서명 신뢰 구분 및 문서 최신성.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-12 | 손흔들기 대신 할퀴기/장난감 총/손하트/장난감 칼/요술봉5종, 동작별 실행 방식·간격·핫키 설정 | 사용자 구체적 답변 반영 |
| 2026-10-12 | 별도 동작 몸·손 PNG를 기존 머리/입/눈과 합성, 한 번에 한 동작·중복 예약 없음 | 원본 및12표정·8입 보존과 독립 시간축 |
| 2026-10-12 | 늘보군5동작4프레임은 imagegen으로 개인 자산만 생성, 공개 앱은 기존 code-native 고양이 작화 방식으로 생성 | 개인 배포 경계와 스타일 일치 |
| 2026-10-12 | 모든 동작의 기본 입력은 좌클릭, 처음 할퀴기만 활성화. 활성 좌클릭 동작은 하나로 전환 | 사용자 기본 핫키 좌클릭·실제 클릭과 함께 실행 지시 |
| 2026-10-12 | 좌클릭 hook은 버튼 down 신호만 처리하고 원래 클릭을 항상 전달 | 사용자 실제 클릭 보존. 좌표/창 내용/다른 버튼·로그 수집 없음 |
| 2026-10-12 | 망토는 enum 마지막에 추가해 기존8분류 숫자 유지·겉옷과 공존 | 이전 의상 JSON 호환 |
| 2026-10-12 | 손 동작은 마이크와 독립적인 시간축으로 제어하고 원본 PNG는 유지 | 사용자 요구와 기존 움직임/개인 자산 경계 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-12 | 지도 | main clean/origin 동기화 확인. 현재 손 독립 모션 없음. 기존 PNG 한 장 구조/캐릭터 공유 렌더와 native hotkey 확인 |

## Completion Notes

진행 중. 손동작 모습 선호 질문은 비동기로 제시했고 망토와 설정·시간축은 독립적으로 진행한다.
