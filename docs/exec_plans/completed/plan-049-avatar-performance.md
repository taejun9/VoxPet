# plan-049-avatar-performance

## Status

completed

## Owner

project_lead / harness_builder / quality_runner

## User Request

실제 버추얼들이 사용하는 앱처럼 부드럽게 동작하도록 수정한다. 현재 외형과 움직임이 너무 부자연스럽다는 지적을 실제 원본 출력으로 검증한다.

## Goal

늘보군·펠리의 원본 인상을 보존하면서 대기/말하기/눈깜빡임/9동작/전환을 일관된 실시간 연기로 만든다. 각 파츠의 찢어짐·늘어남·겹침·얼굴 변형과 반복적인 튀는 움직임을 실제 연속 원본 화면에서 찾아 고친다. 시작·중단·재입력·무음·표정/캐릭터 변경이 자연스럽게 이어져야 한다. 기존 의상17종/14분류/헤어3종/회전 모자/날개·무기 선택과 파지·원본/설정/단일 사이드바·개인정보 계약을 보존한다. 수치 검사만으로 자연스러움을 판정하지 않으며 전후 원본 애니메이션, 실제 Windows/portable/OBS 전체 검증과 새 Windows 테스트 ZIP을 제공한다.

## Non-Goals

웹캠/얼굴 추적·음성 저장/원격 전송·자동 마이크 시작·원본13PNG 외부 업로드·기존 ZIP/다른 plan024 작업트리 삭제는 하지 않는다. Live2D라는 이름으로 PNG 변형 결과를 과장하지 않는다. 기존 사용자 타임라인의 step/smoothstep/monotoneCubic 의미와 일반 실행 설정을 임의 변경하지 않는다.

## Context Map

- [앱 설계](../../architecture/application.md), [제품](../../product/product.md), [QA](../../quality/rules.md), [개발 환경](../../quality/development.md), [하네스](../../architecture/harness.md), [개인정보](../../privacy/principles.md)
- [이전 계획](../completed/plan-048-emotive-companion-motion.md), [리뷰](../../reviews/plan-048-emotive-companion-motion-review.md), [Studio 샘플](../../guides/studio-samples.md)
- MotionEngine/LegacySpriteSheetRuntime/JointSkinning/CompanionParts/CompanionPartImages/CharacterViewModel/ActionEngine/MotionSurface/MainViewModel

## Constraints

시작 main/origin4005b78·clean·동일. 전용 codex/plan-049-avatar-performance/.worktree/plan-049-avatar-performance을 사용한다. 기존451 Core/3296 WPF/2683 portable 사례와 공개/개인 자산 분리를 유지한다. QA→단일 에이전트 자체 리뷰→완료 이동/리뷰 미러→main ff/push→branch -d→worktree 제거를 수행한다. 이전 goal turn은 검증된 ZIP·main 통합을 만든 progress이며 새 목표는 이전 합격으로 완료 처리하지 않는다.

## Implementation Plan

- [x] 원본 연속 출력과 실시간 파이프라인에서 부자연스러운 외형·움직임의 원인을 재현한다.
- [x] 형태를 보존하는 파츠/관절·대기/말하기/눈·동작 전환과 캐릭터별 연기를 구현한다.
- [x] 두 원본의 기본/복장·9동작·전환/음성 입력 연속 전후 출력을 검토한다.
- [x] lint/Release/Core·Windows 실제 표시/portable/OBS 전체 QA를 수행한다.
- [x] QA 후 자체 리뷰·완료 문서·새 Windows ZIP을 마무리하고 Git 통합용 검증본을 확정한다.

## QA Plan

python3 harness/scripts/verify_base.py, verify_app.py, compileall, git diff --check. SDK10.0.401 locked restore/format/analyzer/Release·Core 전체. 새 모션은 유한값/상한·불규칙 tick/여러 frame rate·입력 경계·전환 위치/속도·시작/중단/연타/캐릭터 변경·정착을 검사한다. 파츠 면적/이음새·머리/눈/부리 비율·파지/활시위·발 기준선을 확인한다. 개인 원본은 로컬 연속 합성에만 사용하며 공개 Windows 실제 desktop/표시 픽셀·서명/시작·ZIP 동봉 도구와 OBS WGC/alpha를 별도로 재실행한다. 검증할 정량 성능 지표는 측정 전에 Decision Log에 적는다. 시험한 공용 bytes+개인13PNG의 CRC/PE/해시로 새 ZIP을 검증한다. 물리 마이크/GPU/DPI 실기는 별도로 기록한다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰로 요구·외형과 연속 연기·호환·스레드/수명·개인정보·파일 보존·배포 일치를 점검한다. 객관적인 수치 합격과 원본 시각 검토를 함께 사용한다. 미확인과 한계를 완료 근거로 대체하지 않는다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-18 | 원본 full mesh로 외형과 실시간 경로를 함께 점검한다. | 키 보간만 바꾼 뒤에도 사용자가 부자연스러운 외형을 지적했다. |
| 2026-10-18 | 어깨 root와 Cartesian 손 목표를 공유하고 source clip/투명 범위/행렬을 캐시한다. | 소매가 고정 cap 아래로 꺾이고 같은 삼각형 clip을 매번 만드는 문제를 확인했다. |
| 2026-10-18 | 220ms 현재 자세/이전 속도 보정, 음성 관성·호흡·목 회전과 양발 고정을 rig resolve 전에 적용한다. | 전체 시트의 즉시 bounce와 중단/재입력의 자세 점프를 없애야 한다. |
| 2026-10-18 | 기본/전역/반복 입력5개를 실제 companion timeline으로 연결한다. | 과거 빈 uppercase 동작이9개 authored 동작을 우회했다. 일반 legacy의1.4초 계약은 보존한다. |
| 2026-10-19 | 원본 leg contour를 유지하고 신발 등록 PNG의 고립 잔여물만 제거한다. | 중앙 leg mask 확대가 원본 데이터 보존 검사를 실패시켰다. bind pose 원본 비교의1%/채널차24 기준은 유지한다. |
| 2026-10-19 | 입은 원본8/3단계·기존 곡선을 유지하고 눈만4단계 frozen atlas로 준비한다. | 개인 원본의 mouth 혼합 시제품에서 이중 윤곽·코/입 흐림이 보였다. 최초 시제품60/20개 대신 출시 bank는32/12개다. |
| 2026-10-19 | 기본 연기를 선택형 motion.companionPerformance로 native 편집/내보내기에 보존한다. | 기존 파일은 none으로 호환하고 두 캐릭터는 편집 후에도 같은 연기를 써야 한다. |
| 2026-10-19 | 준비 완료는 명시적 UI Dispatcher에서 세대/종료를 재검사하고 적용한다. | 호출자의 SynchronizationContext에 의존하지 않고 worker의 UI 변경을 막는다. |
| 2026-10-19 | Studio 종료를 시작한 즉시 Model 종료도 시작한다. | 편집기 종료 await 동안 캐릭터 선택이 유효하던 경합을 실제 Windows에서 확인했다. QA는 실제 Closed 신호와 즉시 입력 차단/대기 선택 취소를 검사한다. |
| 2026-10-19 | 분리 캐릭터 표정 전환은 이전 얼굴 opacity1 위에 새 얼굴을 Blend로 그린다. | 두 얼굴을 함께 fade하면 중간 alpha가.75로 내려간다. 기존 전체 시트 fade는 유지하고 새 실제 픽셀 검사를 추가한다. |
| 2026-10-19 | 개인13PNG는 로컬 원본 검토와 개인 ZIP에만 둔다. | 공개 합성 Windows/OBS와 로컬 SVG proxy를 구분하며 물리 마이크·사용자 GPU/DPI·Live2D/추적을 시험한 것으로 주장하지 않는다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-18 | 설계 | main/원격/작업트리를 재확인하고 새 계획을 작성했다. 원본과 이전 ZIP을 보존한다. |

## Completion Notes

앱1.5.0/pack6.0.0·검증 소스6e3b0d0. 두 원본의 어깨 회전·소매 cap·신발 잔여물을 보정하고220ms 시작/중단/재입력 연결·음성 관성·호흡/목 회전·발 위치/밑창 고정을 적용했다. 기본 입력5경로도 실제9동작을 사용한다. 원본8/3입 단계와 곡선,4단계 눈, 불투명 표정 전환을 보존한다. UI Dispatcher 완료/취소·종료 선택 경합·native 편집/내보내기 호환과 렌더 캐시를 검증했다. 두 pack은 각각114파일/17착용항목/14분류/헤어3/9동작이다.

2026-10-19 [전체 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/39151511538): locked restore/format/analyzer·Release 경고0/오류0·Core465/465/skip0·WPF3385/실패0·desktop549장/90시점 실제 표시 픽셀·unsigned/signed 각9시작·SDK 없는 ZIP 도구2764/실패0이다. 이전451 Core/3296 WPF/2683 portable 이름을 모두 보존했다. [OBS](https://github.com/taejun9/VoxPet/actions/runs/39151511518) WGC/chroma/native alpha·같은 HWND 왕복/각7변화 프레임 통과. [추가 입력100회](https://github.com/taejun9/VoxPet/actions/runs/39151711356)1081검사/실패0이며 probeOnly로 전체 QA와 구분한다. 전체와 portable의20회씩도 통과했다.

최종 head 전환3시점 alpha는 두 캐릭터 모두255다. 렌더128/256/512px은 최적화 전과 byte-exact이며 warm clip/scan/matrix 생성0,256px draw삼각형은 늘보군1440→416/펠리864→152였다. warm bitmap 합성·읽기8회 평균은76.96→33.42ms/46.70→20.53ms다. 실제 표시 합성 입력 실험의1.5초 composition callback 중앙값17.37/16.83ms·p95 31.82/31.92ms·tier2다. 이 구간은 callback마다 숫자 상태를 주입한 시험이며 일반 timer의 FPS나 사용자 GPU의60fps 보장으로 쓰지 않는다.

개인 원본 로컬790연속 프레임을22 overview page(9동작의18page/576프레임+착용68+기본 연기146),512px8상세·전후2page로 직접 검토했다.20GIF도 준비했다. 원본12표정의 mouth 혼합 시제품을 직접 검사해 흐림/이중 윤곽을 채택하지 않았다. 마지막 Core로 다시 내보낸790프레임은 byte-identical, numeric SHA256 `73c91c50fa1d00fcf20eac0202b8c2c897eef604e0e1b7a5b5865e3d74f6ea84`다. 최종 공개 Windows26화면/5page와OBS5캡처를 직접 검토했다. 개인 SVG proxy와 실제 개인 Windows 실행을 구분한다.

새 `artifacts/VoxPet-Windows-test-plan049.zip`:54파일/104157815bytes, SHA256 `92a02080f3063e9e470b022446b60f2434117c3b0ee21ac60f5fed4ca8f74040`, EXE SHA256 `e267cb17269a3e4d72e926e023811ab1296a89dad75fd6482db613e0ede5d61e`. 시험한 공용41파일·기존 개인13PNG의 byte equality와 ZIP CRC/PE x64/세 pack 소스 일치를 확인했다. Windows CMD는 checkout의CRLF를 그대로 보존하고 LF 소스와 줄바꿈 이외 내용이 같은지 검사했다. 이전 ZIP·README/AGENTS·plan024 작업트리를 보존했다. 개인 PNG/음성 외부 전송과 사용자 신뢰 저장소 변경은 없다.

[QA 후 단일 에이전트 자체 리뷰](../../reviews/plan-049-avatar-performance-review.md)에서 미해결 필수 수정 사항은 없었다. 물리 마이크/권한/USB·실제 게임 입력·사용자 GPU/혼합 DPI·개인 그림의 실제 Windows 실행·새60분 실기는 [체크리스트](../../quality/windows-checklist.md)의 별도 항목이다. layered BitBlt 제한은 유지한다. 문서 커밋 후 표준 main ff/push→동일 HEAD detach→branch -d→worktree 제거를 수행한다. 실제 Git 종료/보존 확인은 로컬 `artifacts/test-results/plan049/final/git-lifecycle.json`에 기록한다.

## 조사·실험 이력 (초기 후보와 채택하지 않은 시제품 포함)

full512 원본 합성에서 늘보군 양팔 소매가 수평 shoulder cap 아래로 꺾여 리본처럼 보이고, 펠리는 날개 뿌리/가슴의 겹침이 보인다. 기존256 근사 합성만의 문제로 처리하지 않는다. 일반 시트 경로는 몸 전체를 음량에 바로 따라 움직이고 눈은 열림/닫힘이 즉시 바뀌며, action 중단/재입력은 즉시 idle/새 시작 키로 돌아간다. 이 세 경로도 새 목표에 포함한다.

렌더러는 full512 전체 격자의 각 삼각형마다 새 clip geometry를 만들고 전체 texture를 다시 그린다. 투명 픽셀 밖의 삼각형을 보수적으로 제외하고 고정 source clip을 재사용하는 방식을 적용한다. 측정 전 기준: 최적화/기존 경로의 합성 bitmap 픽셀 일치, 실제 파츠/의상 frame에서 draw 삼각형 수 감소, warm 상태 추가 clip 생성과 alpha scan0. 시간은 동일 Windows runner에서 warmup 뒤 기록하되 측정 전 임의60fps 보장/합격으로 쓰지 않는다.

공식 Live2D의 rotation deformer/warp deformer 역할과 motion fade를 확인했다. 이는 새 SDK 의존을 뜻하지 않으며 기존 리그/합성기의 관절 회전·연속 전환 설계 근거다: https://docs.live2d.com/en/cubism-editor-manual/deformer/ 및 https://docs.live2d.com/en/cubism-sdk-manual/motion/ (확인2026-10-18).

첫 현재 Core459/459·skip0 통과. 기존451개 이름을 보존했고 새8개에서 대기/9동작 발 고정·중단/재타겟/동일 동작 재시작의 위치 연속·음성 초기 점프/극단 지연을 확인했다. 실시간 전체 WPF/화면과 입력 연결 검사는 아직 실행 전이다.

원본 하단의 중앙22px 조각이 몸통에 남아 발 고정 후 신발 사이로 처지는 것을 확인했다. 늘보군 양발 마스크를 중앙에서 맞닿게 해 원본을 양발이 소유하고 신발 대체 때 함께 숨기도록 한다. 원본 재합성/파츠 픽셀 허용치는 유지한다. 어깨 위 팔 파츠를 포함하고 머리를 팔보다 앞에 배치하며 소매 cap은 둥근 SVG 패턴으로 다듬었다.

2026-10-19: 자동 승인 검토의 사용량 제한으로 전체 formatter가 실행 전에 중단됐다. 안내된 재개 시각 뒤 동일 승인 절차로 재실행해 통과했다. 현재 전체 format/analyzer·Release 경고0/오류0·Core461/461/skip0·문서/자산/diff 검사가 통과했다. Windows 전체/실제 화면·새 blink/입 전환·연속 원본 검토·최종 ZIP과 Git 완료는 아직 남아 있다.

ce8241a Windows39145560033: Core461/Release/lint・전체WPF중2개실패. 렌더128/256/512픽셀은기존경로와바이트일치·warm새clip/scan0, draw삼각형은늘보군960→268/펠리864→152, 합성측정평균65.60→29.76ms/44.83→19.31ms다. 이는같은runner bitmap합성측정이며60fps보장으로쓰지않는다. OBS39145560102통과.

실패parts_body_keeps_pixels_between_feet_neulbo는중앙leg마스크확대가임의원본데이터보존계약을침해한것이다. 해당확대를원복한다. 실제신발사이작은조각은등록된shoes-right PNG에남은다른신발의분리잔여물임을직접확인했다. 의상분리쪽의연결요소를보정한다. parts_rest_pixels_preserve_original_neulbo는새대기회전/호흡이적용된프레임을정지원본과비교해6640/262144변경이었다. 원본파츠보존은독립bind pose를그려기존1%/채널차24기준으로검증하고실시간변형은새stance/연속프레임검사로별도검증한다. 허용치를올리지않는다.

얼굴은원본만사용하는background frozen atlas를준비한다. 활성표정1개·최대60개512px(입15단계×눈4단계)로한정하고표정/캐릭터전환시이전준비를취소한다. 원본파일/메모리입력은변경하지않으며캔버스상비율변경없이forehead에맞춘±3px정렬만허용한다. 입/눈중간프레임은premultiplied RGBA를정수보간해머리의반투명겹침을피한다. 깜빡임은45ms닫기/105ms열기다. 편집/재등록에서도모션프로필을보존하며기존파일의기본값은none이다.

새실제표시검사8화면(각idle/voice/blink/transition)을추가한다. composition측정은스크린샷/bitmap렌더없이1.5초실행중callback간격과renderer tier를기록한다. 최소5회는정지하지않는지에대한기능판정이며FPS합격/60fps보장의대체가아니다. 기존정지원본검사와새모션/얼굴/표시검사를구분한다.

얼굴 준비는UI활성화후시작한다. Native/Editor의worker transaction에서는준비를시작하지않으며UI적용뒤에만비동기준비한다. source이미지와생성atlas이미지를분리해미리보기재생성때재정렬이누적되지않게한다. 새profile의최대중간프레임수는원래16입제작모델까지64개,늘보군60/펠리20개다. shader나SDK의존은추가하지않았다.

표정변경은현재표시중인입/눈중간head픽셀을다음전환의snapshot으로사용한다. 캐릭터identity가바뀌면이전캐릭터head전환을해제하여다른몸에이전머리가잠시겹치지않게한다. 얼굴준비의중복Dispose는멱등이며종료중준비를취소한다. 실제90시점의정지캡처는해당표정atlas준비완료뒤에수행해비동기준비와프레임identity를혼동하지않는다.

발 고정은위치뿐아니라밑창의수평축을rig의정지기준으로유지한다. 비균일몸통배율의역변환에서작은shear만생략하고정지foot의첫basis축과origin을독립검사한다. 자연스러운기본연기는알려진body/head/leftFoot/rightFoot를검증하며없으면명시적오류를내고일반리그는없음profile을사용한다.

렌더링의동일Core행렬은frozen MatrixTransform으로재사용한다. weak key여서지난프레임이해제되면캐시도남지않고두창/팔·소매가같은행렬을공유한다. 기존전체격자경로와픽셀동일및warm새행렬할당0을추가검사한다.

개인 펠리의 두 입 사이 half-frame을 직접 검사하니 위/아래 부리가 크게 회전하는 구간에 이중 윤곽이 생겼다. 세로 윤곽 등록 시제품도 부리 끝/hinge에 찌그러짐을 만들어 채택하지 않는다. 펠리는 원본3입 상태를 유지하고 음량 envelope/머리 반응/4단계 blink를 연속화한다. 늘보군은8원본 사이15입단계를 쓴다. 새 atlas는 늘보군60/펠리12개다. 실제 출력의 원본 인상을 합성 프레임 개수보다 우선하며 원본 파일은 변경하지 않는다.

늘보군12표정 전체 contact에서도 일부 입 half-frame의 코/입이 흐려지는 것을 확인해 모든 입 혼합 시제품을 제외한다. 출시 구현은 원본8/3입 상태를 보존하며 눈만4단계로 보간한다(32/12 frozen texture). mouth는 raw voice의 새 선형round 계산 대신 Core가 기존 voice curve로 선택한 sprite 인덱스를 그대로 사용한다. 새 QA는 각 voice 위치에서 runtime.MouthFrame과 출력 asset의 동일성을 직접 검증한다. 이는 신규 미출시 보간의 수정이며 기존 Core/WPF의 입 경계·픽셀 검사 허용치를 바꾸지 않는다.

원본 연속 출력에서 펠리의 reaching wing 보정이 외곽 잉크까지 살색으로 지우는 것을 확인했다. 전체 원본 silhouette의8px 이웃이 투명한 경계는 그대로 두고 내부 crease만 보정하도록 제한한다. 새 WPF 검사는 공개 fixture의 해당 경계 픽셀을 보정 전 파츠와 채널차2 이내로 대조한다. 원본 contour/시트와 선택/파지 계약은 유지한다.

얼굴 atlas 완료 통지는 호출자의 SynchronizationContext에 의존하지 않고 생성 시 UI Dispatcher로 명시적으로 전달한다. 종료/세대 변경은 UI 적용 때 재확인하며 shutdown의 예약 취소는 정상 처리한다. 새 WPF QA는 UI에서 표준 SynchronizationContext로 잠시 바꿔 준비를 시작하고 완료 통지의 owner thread를 검사한다. worker에서 UI를 만지지 않는 기존 계약을 보강한다.

f8dcae2 Windows39149393300: 새 입/눈/곡선/렌더 검사는 모두 통과했으나 기존selection_close_drops_pending_load 1개 실패다. MainWindow가 Editor.CloseAsync를 await한 뒤 Model.DisposeAsync를 시작해 그 사이 캐릭터 선택/준비가 유효했다. Studio 종료를 시작한 즉시 Model 종료도 시작해 대기 선택을 무효화한다. QA는100ms sleep의 완료 추정 대신 실제 Closed 신호(3초 제한)를 기다리며, 종료 요청 즉시 새 선택 차단·진행 선택 false·이전 캐릭터/스프라이트 보존을 추가 확인한다. 기존 case 이름/기능 판정을 유지하고 더 강한 수명 계약을 검사한다.

512px 최종 양손 원본 검토에서 작은 반대편 날개는 주 날개의 바깥 목선까지 축소/반전하면 어깨 안에 어두운 조각이 생기는 것을 확인했다. 원본 외곽 잉크 보존은 실제 오른쪽 날개에만 적용하고, 재구성한 반대편 날개의 숨은 root는 기존 내부 crease 보정 정책을 유지한다. 원래 손/날개 외형과 기존 far-wing 경로를 보존한다.

표정 전환의 이전 head와 새 head를 모두1-Blend/Blend 투명도로 그리면 중간에 opaque 얼굴의 합성 alpha가.75로 내려간다. 분리 companion의 이전 head는1로 유지하고 위에 새 head를Blend로 그려 얼굴 안의 색상 보간과 불투명도를 함께 유지한다. 일반 legacy 전체시트의 기존 fade 계약은 유지한다. 기존 두 Core 사례에3시점의 source-over alpha1을 추가하며 새 WPF2사례는 실제 합성 bitmap의 머리 내부 alpha255를3시점에서 확인한다.
