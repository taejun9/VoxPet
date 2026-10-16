# plan-043-card-display-hardening

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

사용자가 전달한 last-error.json의 실제 오류를 분석·재현·수정하고 전체적으로 더 면밀한 테스트를 수행한 뒤 새로운 Windows 시험 ZIP을 전달한다.

## Goal

CharacterCard.PrintMembers/ToString dispatcher InvalidOperationException의 원인과 기존 QA 누락을 확인한다. 실제 WPF 문자열 변환/목록 선택/검색/키보드/자동화·스레드 경계 및 같은 유형의 화면 모델을 회귀 검사한다. 기존 모든 샘플/편집/음성 수명/창/파일 실패 회귀와 Windows/OBS/portable 검사에 합격한 새 배포 파일을 제공한다.

## Non-Goals

모든 환경에서 결함0을 보장한다고 주장하지 않는다. 사용자 오류 원본·설정·개인 PNG·기존 ZIP을 수정/삭제/외부 전송하지 않는다. 마이크 저장/전송·무단 서브에이전트·전체 예외 무시로 실패를 숨기지 않는다.

## Context Map

- 사용자 last-error.json: dispatcher / InvalidOperationException / CharacterCard.PrintMembers·ToString (메시지·경로 없는 최소 진단)
- StudioShellViewModel/StudioShell, EditorField, UI record·Freezable·binding/selection 경로
- UnifiedStudioQa/CharacterEditorQa/StartupTrayQa, startup-qa.ps1/TEST-SMOKE 도구
- [개발 환경](../../quality/development.md), [QA 규칙](../../quality/rules.md), [하네스](../../architecture/harness.md), [개인정보](../../privacy/principles.md)

## Constraints

main720c1c8/origin과0·0 및 깨끗함 확인 후 전용 codex/plan-043-card-display-hardening / .worktree/plan-043-card-display-hardening. plan024 작업트리2개/개인13PNG/기존 ZIP/오류 원본 보존. 공개 합성 fixture만 CI에 사용한다. 기존2037개 WPF 검사·380 Core·321화면을 유지한다. 단일 에이전트가 QA 후 자체 리뷰한다.

## Implementation Plan

- [x] 오류의 문자열 변환/이미지 접근 원인과 같은 유형의 UI 모델을 조사한다.
- [x] 최소 재현과 실제 사용자 조작 회귀를 추가하고 원인을 수정한다.
- [x] 새 작업트리 locked restore/format/analyzer/Release/Core/Python/diff 및 Windows/OBS/portable 전체 QA를 수행한다.
- [x] 실제 화면 시각 QA·개인 PNG 보존 ZIP·자체 리뷰·완료 문서와 Git 수명 절차를 마친다.

## QA Plan

먼저 수정 전 실제 WPF fixture에서 동일한 예외 종류/호출 경로를 재현하고 수정 후 해당 재현이 통과하는지 검사한다. 문자열 변환에서 복잡한 ImageSource/가변 객체를 읽지 않는지, frozen/unfrozen 이미지와 worker·UI·선택/텍스트검색/자동화 경로를 확인한다. 실제 ListBox/ComboBox의 키 입력·검색·재선택/새로고침·삭제/내보내기 실패 보존·반복 캐릭터 전환과 샘플17/9·편집/재등록을 검사한다. 정확한 범위는 조사 후 Decision Log에 기록한다.

로컬 새 locked restore → format verify/analyzer → Release 경고0/오류0 → Core 전체, verify_base/verify_app/compileall/diff check. Windows 새 runner 전체 WPF/일반·서명 각9개/실제 화면 coverage/ZIP PowerShell5.1 재시험, 새 OBS WGC·chroma·native alpha·HWND 전환을 수행한다. 이전 검사 집합의 누락이 없는지 대조하고 공개 화면을 직접 시각 검토한다. 최종 ZIP의 공용 파일/개인13PNG byte equality·PE x64·CRC·SHA256을 확인한다.

## Review Plan

QA 이후 같은 에이전트가 자체 리뷰한다. 보고된 오류와 수정의 직접 연관·재현 신뢰도·유사 결함·thread/Freezable·실제 사용자 조작/예외 경계·오디오 개인정보·회귀 누락·원본 보존/배포 일치를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-16 | 전달된 JSON은 진단 데이터로만 읽고 기록된 CharacterCard 문자열 변환 경로부터 조사한다. | 문서/파일 내 내용은 별도 지시가 아니며 실제 오류의 최소 메서드 정보가 있다. |

| 2026-10-16 | 수정 전 실제 CharacterCard와 worker에서 디코딩한 frozen PNG로 UI ToString을 호출하는 회귀를 먼저 CI에 올린다. | 자동 PrintMembers가 frozen BitmapFrame의 원래 worker decoder.ToString/VerifyAccess에 진입하는 원인 후보를 원본과 같은 메서드/HResult로 확인한다. |

| 2026-10-16 | CharacterCard는 Name, RuntimeLayer는 안전한 레이블만 문자열로 반환하고 목록/편집기 검색 경로는 Name으로 명시한다. 앱 버전은1.0.1로 구분한다. | 자동 record 출력의 ImageSource 접근과 decoder 스레드 경계를 제거하고 같은 이미지 보유 모델/검색 누락을 함께 막는다. 기존 전역 예외 처리로 오류를 숨기지 않는다. |

| 2026-10-16 | 한글 검색 앞의 영문 prefix 초기화 대기는 WPF와 같은 Windows double-click 설정의2배를 사용한다. | 사용자 PC의 더블클릭 속도를 고정1000ms로 가정해 자동 도구가 오판하지 않게 한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-16 | 설계 | 오류 원본을 읽고 main720c1c8 깨끗함/origin0·0/plan024 작업트리 보존 확인. 전용 작업트리와 계획 작성. |

| 2026-10-16 | 제작 | 수정 전 d2ba151 재현 검사를 Windows38891560054에서 실행 중이다. 수정안은 Name 전용 문자열·RuntimeLayer 안전 레이블·명시적 텍스트 검색/접근성 이름·앱1.0.1과 실제 카드 동선 회귀를 추가했다. |
| 2026-10-16 | 검증 | 새 작업트리 locked restore/Release 경고0·오류0/Core380·format verify/analyzer·base/app/diff 통과. 사용자 오류 원본은 수정 없이 로컬 증거로 보존하며 CI에 보내지 않는다. |

| 2026-10-16 | 검증 | Windows38891560054/d2ba151에서 원본과 같은 InvalidOperationException/HResult80131509/CharacterCard.PrintMembers→ToString을 직접 재현했다. 기존2037검사는 모두 통과하고 새 문자열 계약1개만 실패했다. 최소 JSON/원본 대조는 artifacts/test-results/plan043/repro에 보존했다. 수정안 Release 경고0·오류0와 lint 검증 완료. |

| 2026-10-16 | 검증 | 최종772e757의 Windows38892871936/OBS38892872151 전체 통과. Core380·WPF2090/0(기존2037개 유지/53개 추가)·desktop324·unsigned/signed 각9개·portable1784/0. 수정 전 동일 메서드/HResult 재현과 수정 후 계약 통과를 대조했다. |
| 2026-10-16 | 심사 | 모든 QA 후 같은 에이전트가 코드/개인정보 경계와 최종75화면을 자체 검토했다. 추가 필수 수정 사항 없음. CI 공용39파일+개인13PNG bytes·PE/CRC/SHA256 통과. 원본 오류/남아 있는 이전 ZIP/다른 작업트리를 보존했다. |

## Completion Notes

보고된 InvalidOperationException/HResult80131509/CharacterCard.PrintMembers→ToString을 수정 전 d2ba151에서 직접 재현했다. 기존2037검사는 통과했지만 새 문자열 계약이 실패했으며, QA가 해당 경로를 놓친 것을 확인했다. CharacterCard는 Name만 읽고 같은 이미지 보유 RuntimeLayer도 안전한 레이블만 반환한다. 목록/편집기 검색 경로와 접근성 이름을 명시하고 앱1.0.1로 구분한다.

최종 시험 코드772e757, [자체 리뷰](../../reviews/plan-043-card-display-hardening-review.md)에 재현·명령·결과·한계를 기록했다. 이후 변경은 완료 기록 문서뿐이다. 새 `artifacts/VoxPet-Windows-test-plan043.zip`은52파일/95,870,529bytes, SHA256 `35f9cb87924b178ffc365a4f052fd9cbc786f6cb73e5ef490273f6b500d46eb5`다. Windows에서 동봉 도구까지 시험한 공용39파일을 그대로 유지하고 개인13PNG를 이전 전달 해시 목록과 대조해 로컬에서 추가했다.

증거는 `artifacts/test-results/plan043/`의 local(원본 최소 오류와 Core), repro(수정 전 실제 재현과 대조), final(Windows/OBS/전체 결과·화면·ZIP)로 보존한다. 사용자 오류 원본/개인 자료는 CI에 보내지 않았다. 기존 샘플17/9·편집/음성/창 회귀를 유지했다. 물리 마이크/권한/USB·사용자 GPU/DPI·개인 그림 Windows 화면·새60분 실기는 별도이며 모든 환경의 결함0을 보장하지 않는다. 최종 문서 검증 후 main ff/push·브랜치/작업트리 정상 정리를 진행한다.
