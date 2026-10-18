# plan-048-emotive-companion-motion

## Status

active

## Owner

project_lead / harness_builder / quality_runner

## User Request

동작을 이모티콘처럼 부드럽고 자연스럽게 개선하고 늘보군과 펠리 각각의 체형과 성격에 맞춰 작업한다.

## Goal

두 원본 캐릭터의9종 동작에 준비·주동작·여운·복귀가 이어지는 타이밍과 몸통/머리/팔의 겹치는 움직임을 부여한다. 모든 키에서 속도가0이 되는 기계적인 멈춤과 시작/끝 튐을 줄이고, 늘보군은 둥글고 느긋한 탄성, 펠리는 측면 부리/날개에 맞는 가벼운 리듬으로 구별한다. 실제 원본 연속 프레임과 Windows 표시 화면으로 검토하고 전체 QA 및 새 Windows 테스트 ZIP을 전달한다.

## Non-Goals

개인 원본13PNG·이전 ZIP·README/AGENTS·다른 plan024 작업트리를 덮어쓰지 않는다. 개인 자료는 Git/CI/생성 도구로 보내지 않는다. 음성 저장/전송·자동 마이크·추적·서브에이전트를 추가하지 않는다. 기존 사용자 타임라인의 step/보간 의미를 임의 변경하지 않는다.

## Context Map

- [앱 설계](../../architecture/application.md), [QA 규칙](../../quality/rules.md), [개발 환경](../../quality/development.md), [개인정보](../../privacy/principles.md)
- [이전 완료 계획](../completed/plan-047-natural-companion-studio.md), [샘플 안내](../../guides/studio-samples.md), [패키지 규격](../../product/voxpet-package-format.md)
- ActionTimeline/ActionDefinition/ActionEngine·PackageJson·CharacterDocument·companion_actions.py·LegacySpriteSheetRuntime·JointSkinning

## Constraints

시작 main/origin04c23e8·0/0·깨끗함. 전용 codex/plan-048-emotive-companion-motion 및 .worktree/plan-048-emotive-companion-motion을 사용한다. 이전446 Core/3096 WPF/2483 portable 사례를 보존하고 변경 요구로 달라지는 기대값은 이유를 기록한다. QA→자체 리뷰→완료 이동/미러→main ff/push→branch -d→worktree 제거를 따른다.

## Implementation Plan

- [x] 현재9동작의 속도·시작/종료·파지와 두 체형의 연속 원본 프레임을 조사한다.
- [x] 기존 호환을 보존하는 연속 보간과 캐릭터별 타이밍/겹침/복귀를 구현한다.
- [ ] 머리/몸통과 팔·소매·양손/활 연결을 실제 원본9동작 연속 프레임에서 검토한다.
- [ ] lint/Release/Core·Windows 실제 표시/portable/OBS 전체 QA를 수행한다.
- [ ] QA 후 자체 리뷰·완료 문서·원본 포함 새 Windows ZIP·Git 수명을 완료한다.

## QA Plan

python3 harness/scripts/verify_base.py, verify_app.py, compileall, git diff --check. SDK10.0.401 locked restore/format verify/Release 경고0·오류0/Core 전체. 보간의 키값/단조성/유한값/중간 속도 연속/끝점0·거울반전·pose reset·지연tick과 기존 기본동작을 수치 검사한다. 두 캐릭터9동작의 고밀도 시점에서 손 접점·양손 지지·부리/목/발 경계·시작/끝 자세와 본동작 구별을 확인한다. 개인 원본은 로컬에만 사용하며 연속 contact sheet와 재생 가능한 로컬 애니메이션으로 자연스러움을 검토한다. Windows는 공개 교정 그림으로 기존 실제 GDI90시점·표정/의상/시작/스레드 복구·portable·OBS를 재실행한다. 실제 시험한 CI 공용 bytes와 개인13PNG를 합친 새 ZIP의 source/CRC/PE/해시를 대조한다. 물리 마이크/GPU/DPI 실기와 구분한다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰를 별도 단계로 수행한다. 부드러움의 수치만으로 자연스러움을 주장하지 않고 두 원본의9종 실제 연속 출력을 함께 대조한다. 호환/수명/성능/개인정보·ZIP 일치와 남은 실기 제한을 기록한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-18 | 기존 smoothstep과 step은 유지하고 새 샘플에 연속 속도 보간을 명시적으로 적용한다. | 이전 구현은 매 키에서 속도가0이 되어 동작 중간도 여러 번 멈추는 인상이 남는다. |

| 2026-10-18 | 날개 정렬은 shoulder의 로컬 좌표에서 비교한다. | 첫 전체 Core에서 squash의 비균일 배율이 월드 각도 판정에 들어가9동작의 깃털 면이 뒤집혔다. 기존 양의 면적 검사를 유지하고 좌표계 원인을 고친다. |

| 2026-10-18 | 고개 여운은 머리 중심 대신 실제 목 연결점을 중심으로 회전한다. | 실제 원본 합성에서 얼굴 중심의 회전이 어깨/목 접점을 밀어, 부리/얼굴의 여운과 목 연결을 함께 유지한다. |

| 2026-10-18 | 같은 시각의 미래 transform 키를 선택하는 개선은 새 monotoneCubic에만 적용한다. | 기존 smoothstep은 다음 키가 중복이면 첫 키를 향하고 그 시각에 마지막 키를 적용하는 기존 의미를 보존한다. 새 경로와 각각 회귀 검사한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-18 | 설계 | 현재 main/원격의 깨끗한 상태와 이전 결과를 확인하고 별도 계획을 작성했다. 새 목표의 시각 검증은 이전 합격과 구분한다. |

## Completion Notes

구현과 실제 QA/시각 검토 결과를 확인한 뒤 기록한다. 현재는 완료 상태가 아니다.

## 중간 QA 기록

첫 Release 경고0/오류0, Core451중439통과/12실패.9건은 비균일 body scale의 월드 정렬 판정 오류,2건은 보이지 않는 arrow 효과 앵커를 몸의 idle 복귀에 포함한 새 검사 오류,1건은 새 수치 검사1ms 꼬리 예상값(실제0.001249px)이다. 기존 깃털 면적 판정은 보존하고 어깨 로컬 좌표를 사용하며, 몸 복귀는 신체 앵커/머리/발 트랙으로 검사한다. 원본 실패 TRX를 보존한다.

두 번째 전체 Core451/451·skip0 통과, 전체 format verify 및 Release 경고0/오류0 통과. recoil 회복은 몸통의 작은 표현 움직임과 분리해 같은 torso-local 손 위치로 비교하며 발사/반동 기존 판정은 유지한다. 개인 자료 없는558개 Core 프레임을 로컬 내보냈고 원본 픽셀 합성을 진행한다.
