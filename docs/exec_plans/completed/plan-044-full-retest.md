# plan-044-full-retest

## Status

completed

## Owner

project_lead / quality_runner

## User Request

보고된 버그 수정 작업을 이어서 진행하고 전체 테스트를 다시 수행한다. 검증된 Windows 시험 파일을 전달한다.

## Goal

plan043에서 재현·수정한 CharacterCard dispatcher 오류를 포함해 최신 main22215a6의 전체 검증을 새 작업트리와 새 Windows/OBS runner에서 반복한다. 실패가 있으면 원인을 고친 후 관련 검증을 반복한다.

## Non-Goals

사용자 오류 JSON·설정·개인 PNG를 외부로 전송하거나 기존 ZIP을 덮어쓰지 않는다. 실제 물리 마이크/GPU/DPI·장시간 검증을 자동 합성 시험과 혼동하지 않는다. 모든 환경의 무결함을 보장하지 않는다.

## Context Map

- [이전 수정 계획](../completed/plan-043-card-display-hardening.md)
- [이전 자체 리뷰](../../reviews/plan-043-card-display-hardening-review.md)
- [개발 명령](../../quality/development.md), [QA 규칙](../../quality/rules.md)

## Constraints

main/origin22215a6 깨끗함 확인. plan043 정상 병합/push/정리를 완료하고 전용 codex/plan-044-full-retest와 .worktree/plan-044-full-retest를 생성했다. 기존 plan024 작업트리2개와 모든 기존 사용자 자료를 보존한다. 서브에이전트 없이 실행한다.

## Implementation Plan

- [x] 새 작업트리 locked restore/format/analyzer/Release/Core/Python/diff 전체 실행.
- [x] 최신 main으로 새 Windows/OBS 전체 workflow를 실행하고 증거·이전 검사 집합을 대조.
- [x] 샘플·실제 화면·공용 파일과 개인 PNG를 확인하고 새 ZIP 생성.
- [x] QA 후 자체 리뷰/완료 기록/main 병합·push·안전한 정리.

## QA Plan

로컬 verify_base/verify_app/compileall/diff, locked restore → format verify/analyzer → Release 경고0/오류0 → Core380. Windows WPF2090, desktop324, 일반/서명 각9개 시작·종료·원본 보존, ZIP에서 PowerShell5.1 portable1784를 확인한다. 이전 검사 집합 전체를 유지하고 오류 재현 계약·목록/편집기 키 검색·의상17/동작9·헬리콥터/날개/헤어를 포함한다. 새 OBS WGC/chroma/native alpha/같은 HWND 전환을 확인한다. 공개 화면 직접 검토 후 공용39파일과 개인13PNG의 byte equality/PE x64/CRC/SHA256을 검증한다.

## Review Plan

QA 이후 동일 에이전트가 별도 단계로 자체 리뷰한다. 원본 오류와 수정의 연결, 회귀 검사 누락, 개인정보와 기존 파일 보존, 실제 전달 ZIP의 CI 빌드 일치 및 남은 실기 한계를 검토한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-16 | 앞선 main CI 성공과 별도로 최신 main22215a6의 workflow를 수동 재실행한다. | 사용자가 전체 재시험을 다시 요청했으므로 새 runner 결과와 새 ZIP을 확보한다. |
| 2026-10-16 | 날개는6프레임/4자세의 왕복0·1·2·3·2·1로 판정한다. | 임시 자산 점검의 모든 프레임 고유 가정이 실패해 생성기/PNG를 조사했다. 되돌아오는 펄럭임의 의도된 동일 그림이며 모자는8프레임/8자세다. 앱 실패를 숨기는 변경은 없다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-16 | 설계 | main 동기화와 plan043 정상 정리 후 새 검증 전용 작업트리/계획을 작성했다. |
| 2026-10-16 | 검증 | 새 작업트리 locked restore·format/analyzer·Release 경고0/오류0·Core380/380/skip0·base/app/compileall/diff 통과. 샘플78파일/17의상/14분류/9동작/헬리콥터8프레임/날개6프레임/헤어3종과 사용자 오류 원본 SHA256 불변을 확인했다. |
| 2026-10-16 | 검증 | 새 main22215a6 OBS38913348630 통과. WGC/chroma/native alpha·같은 HWND 배경 전환·각7프레임 확인. 공개 캡처4장 직접 검토. 기존 BitBlt layered 제한 유지. |

| 2026-10-16 | 검증 | 새 Windows38913337799 전체 성공. Core380/WPF2090/desktop324/startup 각9/portable1784, 이전2090개 전부 유지. 최종 ZIP 공용39·개인13 bytes/PE/CRC/SHA256 통과. |
| 2026-10-16 | 심사 | 모든 QA 후 동일 에이전트 자체 리뷰와75화면·20원본·4OBS 시각 검토. 추가 필수 수정 없음. |

## Completion Notes

최신 main22215a6을 새 작업트리와 새 Windows/OBS runner에서 다시 시험했다. [Windows38913337799](https://github.com/taejun9/VoxPet/actions/runs/38913337799), [OBS38913348630](https://github.com/taejun9/VoxPet/actions/runs/38913348630) 모두 통과했다. Core380/380·lint/analyzer·Release 경고0/오류0·WPF2090/0·desktop324·일반/서명 각9개·portable1784/0. 이전2090개 WPF 검사 집합이 정확히 유지됐다. 샘플17의상/14분류/9동작과 움직이는 모자/날개·헤어3종을 확인했다.

모든 QA 후 같은 에이전트가 자체 리뷰하고 공개 실제 화면75장/샘플 원본20장/OBS4장을 직접 검토했다. 추가 필수 수정 사항은 없다. 앱/하네스/시험/자산 변경 없이 plan043의 Name 전용 카드 출력 수정을 유지한다. [자체 리뷰](../../reviews/plan-044-full-retest-review.md)에 범위와 한계를 기록했다.

새 ZIP `artifacts/VoxPet-Windows-test-plan044.zip`:52파일/95,870,506bytes, SHA256 `11e192512b8e575897a31fc3a3f829f0c3435b4eec5c7f620e3161ce10233d18`. 공용39파일은 새 CI ZIP과 byte-identical, 개인13PNG는 이전 검증 해시와 일치한다. PE x64/CRC/샘플 동일성 통과. 사용자 오류 원본/기존 ZIP/개인 자료/다른 작업트리는 보존했고 개인 자료를 전송하지 않았다. 증거는 `artifacts/test-results/plan044/`에 저장했다. 물리 마이크·권한/USB·사용자 GPU/DPI·개인 그림 Windows 화면·새60분 실기는 대상PC에서 별도 확인한다. 최종 문서 검증 후 정상 ff/push/브랜치·작업트리 정리를 수행한다.
