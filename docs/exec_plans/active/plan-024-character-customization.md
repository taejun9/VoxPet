# plan-024-character-customization

## Status

active

## Owner

project_lead / plan_keeper

## User Request

모든 캐릭터 표정 테스트·꾸미기 아이템, 그림 늘보군 입 수정, 좌클릭 손동작과 이름에 맞는 도구, 할퀴기를 손올리기로 변경, 옷의 손동작 연결.

## Goal

다섯 캐릭터 각각12종 표정을 선택/연속 테스트하고 캐릭터별 꾸미기 프리셋을 바로 착용한다. 그림 늘보군의 닫힘/반열림/열림 작화를 일관된 입 모양으로 수정한다. 손올리기는 명확한 팔 올림으로, 총/칼/요술봉/하트는 알아볼 수 있는 도구와 손 위치 연결로 표시하고 좌클릭으로 작동한다. 소매와 손에 같은 변형을 적용하며 기존 설정과 원본/이전 ZIP을 보존한다.

## Non-Goals

사용자 승인 없는 개인 PNG 외부 전송, 음성 저장/전송, 물리 마이크·사용자 GPU 검증 주장, 기존 원본 덮어쓰기, 서브에이전트 실행.

## Context Map

CharacterCatalog/CharacterViewModel/ExpressionViewModel, CharacterSheetLoader, CostumeViewModel/CostumeStore/프리셋, HandGesture/CharacterMotion/ConnectedMotion/MotionSurface/HandMotionInputs, 실제 Windows 화면 QA와 로컬 개인 패키지.

## Constraints

codex/plan-024-character-customization와 .worktree/plan-024-character-customization. 개인 자산은 ignored artifacts/characters/plan024에 새 버전으로 저장한다. imagegen 사용은 기존 전송 제한의 범위를 확인한 뒤 진행한다. GitHub CI는 공용/합성 자료만 사용한다. 기존 enum0/저장 설정·표정 슬롯·의상 배치 호환을 유지한다.

## Implementation Plan

- [ ] 캐릭터별 표정 가족 로딩과 전체 캐릭터 표정 테스트/취소·복원 UI.
- [ ] 각 캐릭터에 맞는 꾸미기 프리셋·바로 착용·저장/전환 배치.
- [ ] 개인 작화 승인 범위 확인, 그림 늘보군 입 수정과 표정 자산 제작/검증.
- [ ] 손올리기/좌클릭·도구 앵커/소매·의상 변형 개선과 호환.
- [ ] Core/WPF/실제 화면·OBS/서명/로컬 ZIP QA와 자체 리뷰/완료 Git 절차.

## QA Plan

verify_base.py, verify_app.py, git diff --check, locked restore/format/Release 경고0·오류0/Core 전체. 캐릭터5×12표정·입/눈 frozen 상태와 테스트 취소/전환/종료/원래 캐릭터 복원, 프리셋추가/착용·독립배치/재실행/기존 사용자 아이템보존, enum0 이전 저장 호환, 실제 WM_LBUTTONDOWN/좌클릭 등록·해제·중복, 알아볼 수 있는 도구/팔 끝 앵커·팔/소매 동일 변형·얼굴보존을 검사한다. GitHub Windows 실제 화면 캡처 직접 검토와 OBS WGC/alpha/같은 HWND 회귀. 개인 PNG는 로컬 알파/셀/표정/시각 검토와 ZIP CRC/해시로 확인한다. 정상 동작 변경에 맞는 장시간 검증 필요성을 리뷰에서 결정한다.

## Review Plan

QA 후 단일 에이전트 자체 리뷰. 요구사항5종·프리셋의 실제 사용 경로·동작 가시성·저장 호환·IO/수명·이미지와 전송 경계·기존 슬롯/의상 보존을 확인하고 completed/리뷰 미러→main ff-only/push→branch-d/worktree 제거한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-13 | 내장 image_gen 참고 제공 허용, GitHub 전송 금지 유지 | 사용자 명시 답변으로 작화 제작 진행 |
| 2026-10-13 | 개인 이미지 생성 도구 참고 제공 범위를 확인하며 코드 작업 진행 | 이전 사용자 PNG 외부 전송 금지 선택을 유지하며 이번 작화 수정 요청과 적용 범위를 명확히 함 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-13 | 지도 | main ff82c9c clean/origin 동기화 확인. 신규4캐릭터는 평상 시트만 있으며 그림 늘보군 중간/큰 입 작화 불일치 확인 |

| 2026-10-13 | 제작 | 48개 신규 개인 시트/288상태 imagegen 제작·그림 늘보군 입 단계 통일,9종×5캐릭터 꾸미기 프리셋/범위 저장,전체 캐릭터 테스트 수명,좌클릭 원클릭 연결/손올리기/도구 wrist 앵커·공유 소매 변형 구현. 격자 뒤집힘 발견 후 회전/올림을 분리해 모든 기본 격자 검사를 통과 |

## Completion Notes

진행 중. 기존16PNG/원본/ZIP을 보존하고 새 자산과 검증 배포를 별도로 만든다.
