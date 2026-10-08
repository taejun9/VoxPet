# plan-015-expressions-mouth

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

늘보군 표정을 다양하게 추가하고 데시벨에 따른 입 크기를 더 세밀하게 나눈다. 이전 개인 실행용 선택과 원본 보존 요청은 유지한다.

## Goal

늘보군 여섯 실제 얼굴 표정(평상/기쁨/슬픔/화남/놀람/졸림), 각 표정 입8단계×눈2상태를 제공한다. 정규화·Gate·attack/release를 거친 음량으로 세밀한 입 프레임을 고르며 기존3단계 개인 시트/저장 슬롯을 보존한다. 개인 실행 ZIP에 새 자산을 적용한다.

## Non-Goals

음성 감정 인식/음소 전사, 녹음·원격 음성 전송, 개인 이미지의 공개 Git/CI 배포, 기존 캐릭터 원본/저장 슬롯 삭제.

## Context Map

- src/VoxPet.Core/Models/SpriteSheetLayout.cs, CharacterParameters.cs, Services/CharacterAnimator.cs: 기존 3단계 계약.
- src/VoxPet.App/Services/CharacterSheetLoader.cs, ViewModels/CharacterViewModel.cs: frozen PNG·표정·기본 가족.
- ViewModels/ExpressionViewModel.cs: 슬롯 저장과 복원, 호환 EncodeSheet.
- tests/VoxPet.Core.Tests/CharacterTests.cs, UsabilityTests.cs, Views/CharacterQa.cs: 수치·이미지·Windows QA.
- artifacts/characters/neulbo/늘보군.png: 보존할 원본.
- imagegen 스킬: 실제 표정/입/눈 픽셀 제작. 새 자산은 root artifacts/characters/neulbo-plan015/에만 보존한다.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-015-expressions-mouth` / `.worktree/plan-015-expressions-mouth` 사용.
QA → 자체 리뷰 → completed/리뷰 미러 → main 병합/push → branch -d → worktree 제거.
개인 실행 자산을 공개 저장소/CI에 포함하지 않는다. 이미지 생성만 사용자 요청대로 외부 도구를 사용하며 마이크 데이터는 도구로 전달하지 않는다. UI 스레드에서 PNG IO/디코딩을 수행하지 않는다.

## Implementation Plan

- [x] imagegen으로 원본 모습을 유지한 여섯 표정·여덟 입·깜빡임 제작, 투명/정렬/프레임 시각 검사.
- [x] 3열/8열×2행 로더·인코더 호환과 유한0~1 입 선택 수치 계약.
- [x] 선택한 기본 표정에 맞는 개인 시트 사용, 기존 슬롯 보존과 실패 복구.
- [x] Core와 합성 Windows fixture, 안내/검증 문서 갱신.
- [x] QA/리뷰/Git 수명/새 개인 실행 ZIP.

## QA Plan

macOS: verify_base.py/verify_app.py/diff --check, SDK10.0.401 locked restore/format/analyzer/Release/Core tests. 8단계 임계값·유한값·dBFS sweep의 순서/작은 입력과 Gate/무음 복귀를 검증한다.
Windows: qa.ps1 -Publish -Smoke의 합성8열PNG로16frozen상태·선택/행열/저장 재시작/기본 표정 가족/손상 복구/3열회귀/탭/방송 수명 확인. OBS green/alpha 회귀. 실제 개인 생성 PNG는 로컬 시각/알파/프레임/해시/ZIP 검사로 확인하고 CI에 전송하지 않는다.

## Review Plan

QA 이후 동일 에이전트 자체 리뷰. 이미지 정체성·표정 차이·입 단계, 이전 포맷과 저장 슬롯, UI 비동기/오디오 경계, 문서와 실제 검증 범위를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-08 | 입8단계·눈2상태를 추가하고 기존3열은 유지 | 이전 개인 파일·슬롯을 깨뜨리지 않고 더 세밀한 반응 제공 |
| 2026-10-08 | 기존6개 표정 종류에 서로 다른 늘보군 얼굴 자산 적용 | 기존 단축키/슬롯 UI와 바로 연결. 기존 개인 기본은 모든 종류에 같은 시트를 사용했음 |
| 2026-10-08 | 생성 결과는 프레임 추출/시트 배치만 수행하며 얼굴 픽셀은 imagegen으로 제작 | 스타일을 유지하고 반복 가능한 런타임 포맷으로 통합. 원본 비파괴 보존 |

| 2026-10-08 | 일부 생성본의3열 배치·화남 닫힌 입 반복·투명 경계를 imagegen으로 재수정 | 실제16프레임과8입 크기를 확보하고 개인 원본을 보존 |
| 2026-10-08 | 프레임 추출 시 독립 캐릭터 연결 영역을 사용, 얼굴은 그리지 않고 패딩/정렬만 수행 | 좁은 행 간격에서 다른 프레임 조각이 섞이지 않게 한다. 별도 초안은 버전명으로 보존 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-08 | 지도 | main clean/origin 동기화 확인, plan015 worktree 생성. 기존 MouthOpen 연속값과 3열 열 인덱스 의존 위치 확인 |

| 2026-10-08 | 검증 | macOS locked restore/format/analyzer/Release 경고0·오류0/Core132개 통과. 상세 합성 fixture와 개인 기본 가족/호환 로더 구현. 작업 브랜치 push로 Windows QA 실행 예정 |

| 2026-10-08 | 검증 | Windows37776428922: Core132개/format/analyzer/Release/publish 경고0·오류0, smoke 실패0/415고유 검사. 상세8열16상태/6표정/인코딩/저장 재시작/손상 복구와 기존3열/UI 회귀 통과 |
| 2026-10-08 | 검증 | OBS37776428945: WGC/Chroma/native alpha/각7프레임 통과. 개인 자산6표정×8입×2눈/96상태/352px셀/RGBA/투명·면적 순서·해시 확인. ZIP18파일/CRC/원본 SHA256 보존 |
| 2026-10-08 | 심사 | QA 후 자체 리뷰. 개인 PNG 공개 미포함/기존 저장 슬롯/오디오 경계/문서/합성 검증 한계 확인. completed 이동과 리뷰 미러 작성 |

## Completion Notes

늘보군6실제 얼굴·입8단계·눈2상태(96프레임)를 개인 실행 패키지에 추가했다.3열 포맷과 기존 저장 슬롯을 보존하며 기본 PNG의 형제 표정 파일을 비동기로 읽는다.8열은 연속 MouthOpen을 세밀하게 고르고3열은 기존0.2/0.6 MouthState 계약을 유지한다. 음량은 기존 dBFS→Gate/정규화/감도/Attack·Release 흐름을 따른다.

실행 코드9e89a07의 Windows37776428922 및 OBS37776428945 통과, Core132/132·Windows415고유 검사 실패0·빌드 경고/오류0. [자체 리뷰](../../reviews/plan-015-expressions-mouth-review.md) 완료. 개인 ZIP root artifacts/VoxPet-win-x64-neulbo-plan015.zip은18파일/CRC 통과, 기존 원본과 이전 ZIP은 보존했다. 이미지 생성은 내장 imagegen을 사용했고 프롬프트/선택 원본/정렬 스크립트/manifest를 root artifacts/characters/neulbo-plan015/에 보존한다.

개인 PNG는 공개 Git/CI에 보내지 않았다. 이번 개인 PNG의 Windows 실제 시작 화면과 물리 마이크/DPI/사용자 OBS 실기는 수행하지 않았다. 합성8열 런타임 QA 및 로컬 개인 이미지 계약·시각 검사로 구분한다. 이전 슬롯의 사용자 PNG가 우선이면 내장 표정 사용/슬롯 저장으로 새 얼굴을 명시적으로 선택한다.
