# plan-036-app-runtime Review

## Summary

[완료 계획](../exec_plans/completed/plan-036-app-runtime.md)의 중앙 Runtime/App 연결을 QA 이후 단일 에이전트가 자체 리뷰했다. [아키텍처](../architecture/voxpet-2-runtime.md).

## QA

최종 구현1e1b57f. [Windows38603070570](https://github.com/taejun9/VoxPet/actions/runs/38603070570), [OBS38603070843](https://github.com/taejun9/VoxPet/actions/runs/38603070843) 성공. local/Windows Core351/351, WPF1535/0, skipped0. locked restore/format verify/Release solution build 경고0·오류0, verify_base/verify_app/diff check 통과. 실제 desktop225 coverage와 unsigned/signed startup 검증도 통과했다.

TRX와 smoke/pixel/OBS JSON을 artifacts/test-results/plan036/final에서 직접 확인하고 native garment PNG를 시각 검토했다. 24 pixel 비교의 최대 평균 byte오차0.00002861, 변경비율0.00001907이다. 기존 독립 wardrobe centroid/얼굴 보존/표정/설정 재시작 검사도 유지한다. OBS32.2.2/WGC에서 green/chroma/native alpha·같은 HWND 전환이 통과했다. BitBlt 실패는 기존 layered window 호환 제한이다.

## Findings

미해결 Phase 9 gate 결함 없음. Main의 animation 조정은 CharacterRuntimeCoordinator로 옮겼다. CharacterRuntimeViewModel은 동일 Core Runtime과 frozen image map을 제공한다. MotionSurface/RenderFrameDrawing에는 입/표정/동작/착용/상속 선택이 없다. LegacySheet의240ms flatten snapshot과 opaque 그림 준비만 App에 남긴다.

LegacySpriteSheetSource는 full-sprite bank의 모든 mouth/eye asset을 정의/codec에 담는다. processor 없이도 Runtime으로 재생하고 패키지 왕복을 시험했다. 기존 gesture scheduler는 중앙 Actions 인스턴스를 공유한다. frozen QA가 runtime 시간을 앞당긴 뒤 이전 clock sample을 받을 때 발생한 예외를 engine.LastTime coalesce로 수정했다.

LegacyRenderGeometryProvider의 pivot/연결 mesh와 native RigRenderGeometryProvider의 source placement/Follow/상속을 Core에서 계산한다. native 정적 가중치와 Runtime 준비는 decode와 함께 worker에서 수행한다. root uniform affine의 중복 이동을 방지하고 이전 snapshot을 유지한다.

native library는 ZIP를 추출하지 않고 검증/실제 WPF decode 성공 뒤 관리 bytes를 저장한다. 실패/종료/최신 revision 검사, 원본 삭제 후 restart와 legacy 기본이 있는 startup의 native 선택을 확인했다. 목록 읽기 실패는 원본을 유지한다. 입력은 reserved key를 거부하고 mouse default off/명시 활성화, WM_HOTKEY/pulse/중복 scene 메시지 폐기와 dispose를 시험했다. tick에 IO/JSON을 추가하지 않았다. 음성 저장·전송·개인 PNG 공개를 추가하지 않았다.

## Residual Risk

macOS에서는 WPF를 실행하지 않았다. Windows 자동 QA는 public/synthetic fixture이며 물리 마이크/권한/혼합 DPI/장시간 실기를 대체하지 않는다. Native garment는3개 가까운 Anchor를 사용하는128triangle policy이며 기존 garment의 정확한 호환은 Legacy provider를 유지한다. native 입력은 지원/예약·충돌 등록 실패 시 manual 재생으로 복구하며 HWND 수명 중 registration ID를 재사용하지 않는다.

## Follow-Ups

Phase 10에서 와이어프레임10화면의 Dashboard/library/editor, 실제 native 액션·부착 조작과 input 상태, 시각 Anchor/타임라인/같은 Runtime preview/검증/export·편집 왕복을 완성한다. 전체 완료 후 Legacy 참조·대체·호환 목적/삭제 가능성 리뷰를 수행한다. 사용자 기존 파일/설정/원본은 유지한다.
