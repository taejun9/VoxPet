# plan-027-upload-examples

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

캐릭터·표정·액션 등을 업로드할 때 필요한 이미지 예시를 만들어 다운로드 폴더에 저장한다.

## Goal

실제로 등록할 수 있는512px RGBA 예제 세트와 완성 PNG 시트, 역할별 미리보기·한국어 사용 안내·ZIP을 생성하고 Downloads에 전달한다.

## Non-Goals

실제 개인 캐릭터 원본 변경·그림 편집기·앱 동작 변경·새 캐릭터 추가. 개인 PNG는 읽거나 전송하지 않는다.

## Context Map

- `harness/scripts/generate_imageset_template.py`: 기존 코드 기반 합성 예제.
- `src/VoxPet.Core/Models/ImageSet.cs`: 실제 JSON/파츠/소매/무기 계약.
- `harness/scripts/export_upload_examples.py`: 재현 가능한 교육용 예제 출력.
- `docs/product/image-set-format.md`: 이미지 규격.

## Constraints

`codex/plan-027-upload-examples` / `.worktree/plan-027-upload-examples`. 단일 에이전트. 기존 코드 기반 도형 예제를 확장해 파츠 정렬과 투명 Canvas를 정확히 보여준다. Downloads의 기존 파일은 덮어쓰지 않는다.

## Implementation Plan

- [x] 파츠6개·입8단계·표정·의상/Anchor·5템플릿 액션 예제 생성.
- [x] 늘보군/펠리 ID용 JSON과 기존 완성 시트·미리보기·사용 설명서 생성.
- [x] 실제 Core 모델·PNG/크기/경로/용량·ZIP 검증과 시각 확인.
- [x] 다운로드 폴더 전달, 자체 리뷰·계획 완료·Git 정리.

## QA Plan

verify_base.py, verify_app.py, git diff --check. 출력 JSON은 실제 ImageSet.Validate와 ImageSetPlayer로 검증한다. PNG RGBA/Canvas/파일 수/용량·ZIP CRC와 조합 미리보기를 확인한다. 앱 코드 변경이 없으므로 기존 Windows 실행을 반복하지 않는다.

## Review Plan

QA 후 업로드 경로, 예제와 실제 캐릭터의 구분, 투명 Canvas·팔 대체/소매/무기 포즈·개인 원본 경계와 다운로드 전달을 자체 리뷰한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-13 | 기존 코드 기반 합성 도형을 교육용 예제로 확장 | 정해진 Canvas와 파츠 교체를 정확히 재현하고 개인 그림을 사용하지 않음 |
| 2026-10-13 | 새 조합 JSON와 기존 완성 시트를 모두 제공 | 두 실제 업로드 방식에 필요한 파일을 구분해 사용 가능하게 함 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-13 | 검증 | 입 도형의 잘못된 좌표를 생성 단계에서 발견·수정. 최종 Core492검사/액션20프레임 픽셀 일치/다운로드 전달116파일 일치 통과 |
| 2026-10-13 | 심사 | QA 후 단일 에이전트 자체 리뷰, 업로드 방식·투명 Canvas·원본 경계 확인 |
| 2026-10-13 | 설계 | 현재 ImageSet v1 규격과 등록 제한 확인 후 worktree 격리 |

## Completion Notes

등록 PNG76장(512px RGBA), JSON2개, 기존 시트8장, 미리보기27장, HTML/한국어 안내를 생성했다. 실제 ImageSet.Validate/ImageSetPlayer/SpriteSheetLayout에서 각ID246검사(총492)를 통과했다. 액션20프레임의 미리보기는 실제 Core 조합 결과와 픽셀 일치했다. PNG CRC·투명도·크기·경로·압축/디코딩 용량·ZIP CRC, HTML 로컬 링크·Python 구문과 문서/자산/diff 검사를 통과했다. 실제 조합 그림을 시각 검토하고 QA 후 자체 리뷰 완료, 미해결 P1/P2 없음.

사용자가 지정한 Downloads/VoxPet-업로드-이미지-예시-20261013 폴더와 같은 이름 ZIP에116파일, ZIP3,020,959바이트를 저장했다. 검증 원본과 전달 파일의 byte equality를 확인했으며 기존 다운로드 파일을 덮어쓰지 않았다. 코드 기반 교육용 그림이며 실제 개인 늘보군/펠리 PNG는 읽거나 전송하지 않았다. 앱 코드 변경이 없어 Windows GUI 검사를 반복하지 않았다. 완료 기록 커밋 뒤 main 병합/push와 브랜치/worktree 정리는 정해진 Git 절차로 수행한다.
