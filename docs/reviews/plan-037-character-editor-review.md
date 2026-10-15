# plan-037-character-editor Review

## Summary

[완료 계획](../exec_plans/completed/plan-037-character-editor.md)의 Phase 10을 QA 이후 단일 에이전트가 자체 리뷰했다. 와이어프레임10화면의 Dashboard/library/editor와 생성 → Canvas → PNG → Anchor → 얼굴/입/표정 → 동작 → 부착물 → 같은 Runtime preview → 검증/export를 정상 사용자 화면에 연결했다.

## QA

최종 구현 d9c55de. [Windows38668937751](https://github.com/taejun9/VoxPet/actions/runs/38668937751), [OBS38668937607](https://github.com/taejun9/VoxPet/actions/runs/38668937607) 성공. local/Windows Core355/355, WPF1744고유검사·실패0. locked restore/format/analyzer/Release solution build 경고0·오류0, verify_base/verify_app/diff check 통과. unsigned/signed 각각9개 일반 시작·정상 종료·저장 파일 보존과 실제 desktop250 coverage가 통과했다.

artifacts/test-results/plan037/last의 TRX/smoke/coverage/OBS JSON과10개 editor desktop PNG를 직접 확인했다.4창크기×8페이지에서 Canvas 높이·입력 접근성·검정 글자 대비를 검증한다. 실제 mouse Anchor drag,16입 프레임·full-sprite 전체 bank·8종 timeline payload와 export/reimport, 모든 지원 필드/원본 PNG bytes/preview·native broadcast frame exact 비교를 통과했다. OBS32.2.2 WGC/크로마·native alpha·같은 HWND 전환 통과.

## Findings

미해결 Phase 10 gate 결함 없음. 초기 read-only meter TwoWay binding 시작 오류, CI capture bounds, inherited dark theme의 흰 글씨, 무한 목록 측정에 따른 Canvas 밀림, 같은 값 event의 IndexOf 첫 행 수정과 순번 기반 pending 입력 재정렬 오류를 수정하고 전체 재검증했다. event 객체별 weak identity와 trigger/pose key 분리를 사용하며 UI identity를 패키지에 저장하지 않는다.

CharacterDocument의 불변 transaction은 section 외 지원 모델·custom 문서 참조·사용하지 않는 원본 PNG도 보존한다. validation/decode/runtime 준비는 worker에서 완료하고 최신 revision·종료 상태를 확인해 scene을 교체한다. export/검증 worker 종료를 기다리며 UI busy 중 편집을 막는다. PNG 준비는 기존 form/detail 선택을 보존하고 명시 적용으로 연결한다. 미적용 입력은 export를 막고 추가/삭제 시 먼저 정리하도록 안내한다.

Editor preview는 CharacterRuntimeViewModel과 방송의 동일 CharacterRuntime/RenderFrameDrawing을 사용한다. timeline seek도 같은 engine을 새 시간축으로 재생한다. visual drag는 부모 static affine의 inverse로 local 좌표를 계산한다. non-square Canvas 비율을 유지하며 Anchor 라벨 겹침을 줄인다. 기본 정보/미리보기에서 불필요 목록·inspector를 숨기고 상세 편집은 동작/부착물 페이지에 둔다.

StudioShell은 library metadata 검색·선택·편집·export/관리 복사본 삭제와 native action/attachment·입력 상태를 연결한다. 기존 PNG/표정/의상/손동작/마이크 설정은 방송 / 설정에 보존한다. 오디오 처리/원본 자산을 변경하지 않았고 음성 저장·전송·개인 PNG 공개를 추가하지 않았다.

## Residual Risk

macOS에서 WPF를 실행하지 않았으며 Windows 자동 QA는 public/synthetic fixture다. 물리 마이크/권한/장치·혼합 DPI/사용자 GPU·실제 입력 장시간은 기존 체크리스트를 따른다. BitBlt layered capture는 기존 제한이다. 편집 초안은 자동 저장하지 않으므로 내보내야 한다. 참조 중 ID 변경/삭제는 무결성 검증에 실패하며 마지막 유효 문서를 유지한다. Sound는 예약으로 재생하지 않는다. 미지원 JSON 확장 속성은 패키지 보존 계약에 포함하지 않는다.

## Follow-Ups

전체 repository migration review에서 Legacy 각 클래스의 현재 참조·대체·호환 목적·삭제 가능성을 보고하고 Runtime/패키지 문서를 확정한다. 기존 파일·설정·migration 또는 QA oracle에 필요한 Legacy는 삭제하지 않는다. 이 후속 검토 전에는 전체 목표를 완료로 표시하지 않는다.
