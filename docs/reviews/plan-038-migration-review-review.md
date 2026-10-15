# plan-038-migration-review Review

## Summary

[완료 계획](../exec_plans/completed/plan-038-migration-review.md)의 최종 repository migration review를 전체 QA 이후 단일 에이전트가 자체 리뷰했다. [전체 요구별 근거](../product/migration-completion-audit.md), [타입별 Legacy 판단](../architecture/legacy-migration-review.md), [Runtime](../architecture/voxpet-2-runtime.md), [.voxpet 규격](../product/voxpet-package-format.md)을 첨부 Master/Phase1~10/최종 prompt·10화면과 대조했다.

## QA

검토 기준96bfbcc의 [Windows38670581601](https://github.com/taejun9/VoxPet/actions/runs/38670581601) 성공. fresh local locked restore/format/analyzer/Release build 경고0·오류0, Core355/355, verify_base/verify_app/diff check 통과. Windows TRX Core355/355·skipped0, WPF1744/0, 실제 desktop250 coverage를 artifacts/test-results/plan038/final에서 직접 확인했다. unsigned/signed 각9개 일반 시작·정상 종료·기존 파일 보존과 signer/icon 검증이 통과했다.

이 계획은 docs만 변경했다. src tree `b248ea27bbf1f17de28dfcec458af46dc03c2f84`, harness tree `faf692bbe4e8b1b233c7a536fbf193cdc5ba8fed`는 마지막 구현d9c55de와 main b883426의 tree와 같다. [OBS38668937607](https://github.com/taejun9/VoxPet/actions/runs/38668937607)의 JSON source commit과 green/transparent 결과를 재확인했다. OBS32.2.2 WGC/크로마/native alpha·같은 HWND/소스 배경 왕복·각7개 프레임 변화 통과. BitBlt는 기존 always-layered 제한이다. 같은 코드의 Phase 10 전체10화면은 직접 시각 검토했으며 최종 Windows 전체 회귀에서도250화면 커버리지가 통과했다.

## Findings

미해결 migration gate 결함 없음. 기반 named models와 finite/immutable/cycle/string ID, Legacy adapter/모션·동작·트리거·상속/콧수염 mapped point·중앙 Runtime·bounded package·App/10화면 편집 workflow를 각각 코드와 관련 Core/WPF tests에 대응시켰다. 새 editor preview와 native 방송의 모든 지원 모델/PNG bytes/Runtime frame exact 왕복을 확인했다.

Core net10.0 source와 engine/Runtime/composer/geometry 경로에는 WPF/NAudio/Win32·FS/JSON/wall-clock animation 판단이 없다. App audio callback은 숫자 snapshot만 발행하고 UI timer는 monotonic time/voice/input을 전달한다. frozen PNG/패키지 IO·JSON·export는 worker이며 종료/실패/최신 revision 보호를 유지한다. renderer는 준비된 final frame과 image map만 그린다.

기존67개 public 타입의 참조 파일·선언 파일 내부/직렬화·간접 binding 목적을 검토하고 namespace가 다른 두 CharacterDefinition을 구분했다. ImageSetPlayer/ComposedImage는 정상 App에서 대체됐지만 독립 test oracle이다. CharacterAnimator/HandMotionScheduler는 새 engine wrapper다. CharacterRig/ConnectedMotion/표정·의상·손동작/PNG preparation·store/input/UI는 기존 파일·설정·정확한 legacy pixel·API 때문에 유지한다. LegacyMotionReference도 QA 전용 비교 구현이다. 새 Legacy bridge는 현재 architecture이며 삭제 후보가 아니다. 삭제를 제안하거나 수행하지 않았다.

Runtime pipeline과 editor/native instance·두 창 scene 공유/viewport·thread 수명을 문서화했고, package format/version·선택 문서·상한/경로·full-sprite·supported-data 보존 계약을 확정했다. pose event는 ID, attachment pose map은 평가 Name이라는 규격 의미도 명확히 했다. 음성 저장·전송·webcam/추적·제품 네트워크/플러그인 기능을 추가하지 않았다.

## Residual Risk

Windows CI는 public/synthetic·가짜 장치·합성 입력이다. 실제 개인 PNG/물리 마이크·권한/USB·혼합 DPI/사용자 GPU·실제 입력 장시간은 기존 실기 체크리스트를 따른다. macOS의 cross-build는 WPF 실행 검증이 아니다. 편집 초안은 내보내기로 보존한다. Sound는 예약 무동작이며 미지원 JSON 확장 속성은 무손실 편집 계약에 포함하지 않는다.

## Follow-Ups

첨부의 Phase1~10과 최종 migration review에 필요한 구현·문서·자동 QA는 완료했다. 기존 사용자 환경의 실기 체크리스트는 별도 release 검증으로 유지한다. 향후 Legacy 삭제는 기존 파일/설정 이관·independent baseline 대체와 전체 compatibility/Windows/OBS QA를 갖춘 별도 계획에서만 검토한다. 완료 기록 뒤 main 병합/push와 일반 branch/worktree 정리를 수행한다.
