# plan-049-avatar-performance

## Status

active

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
- [ ] 형태를 보존하는 파츠/관절·대기/말하기/눈·동작 전환과 캐릭터별 연기를 구현한다.
- [ ] 두 원본의 기본/복장·9동작·전환/음성 입력 연속 전후 출력을 검토한다.
- [ ] lint/Release/Core·Windows 실제 표시/portable/OBS 전체 QA를 수행한다.
- [ ] QA 후 자체 리뷰·완료 문서·새 Windows ZIP·Git 수명을 마무리한다.

## QA Plan

python3 harness/scripts/verify_base.py, verify_app.py, compileall, git diff --check. SDK10.0.401 locked restore/format/analyzer/Release·Core 전체. 새 모션은 유한값/상한·불규칙 tick/여러 frame rate·입력 경계·전환 위치/속도·시작/중단/연타/캐릭터 변경·정착을 검사한다. 파츠 면적/이음새·머리/눈/부리 비율·파지/활시위·발 기준선을 확인한다. 개인 원본은 로컬 연속 합성에만 사용하며 공개 Windows 실제 desktop/표시 픽셀·서명/시작·ZIP 동봉 도구와 OBS WGC/alpha를 별도로 재실행한다. 검증할 정량 성능 지표는 측정 전에 Decision Log에 적는다. 시험한 공용 bytes+개인13PNG의 CRC/PE/해시로 새 ZIP을 검증한다. 물리 마이크/GPU/DPI 실기는 별도로 기록한다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰로 요구·외형과 연속 연기·호환·스레드/수명·개인정보·파일 보존·배포 일치를 점검한다. 객관적인 수치 합격과 원본 시각 검토를 함께 사용한다. 미확인과 한계를 완료 근거로 대체하지 않는다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-18 | 동작 키 보간에 국한하지 않고 기본 연기와 원본 파츠 변형/전환까지 조사한다. | 이전9동작 개선 뒤에도 사용자가 외형과 움직임의 부자연스러움을 지적했다. |

| 2026-10-18 | 팔/소매의 root를 각 어깨 회전 bone에 연결하고 손의 Cartesian 목표로 회전을 푼다. | 기존 늘보군은 root가 body에 고정되어 손을 몸 앞에 올릴 때 소매 위쪽이 수평으로 남고 중간이 각지게 꺾인다. 원본 full mesh 출력에서도 확인했다. |

| 2026-10-18 | 원본 시트 경로의 실시간 연기를 rig resolve 전에 적용하고 발을 고정한다. | 몸 전체의 즉시 음량 bounce 대신 호흡/머리의 지연 반응과 중단/재입력의220ms 연속 복귀를 파츠·의상·무기 모두에 공유해야 한다. |
| 2026-10-18 | 기본 입력과 반복 scheduler의5개 연결을 실제 companion action 정의로 교체한다. | 같은 ActionEngine에 과거 빈 Claw/ToyGun 동작이 입력되어9종 개선을 우회하고 간이 geometry/props가 나오는 경로를 확인했다. 설정/호출 API와 옛 시트의1.4초 규칙은 보존한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-18 | 설계 | main/원격/작업트리를 재확인하고 새 계획을 작성했다. 원본과 이전 ZIP을 보존한다. |

## Completion Notes

현재 조사/구현 단계이며 새 목표 완료를 주장하지 않는다.

## 추가 조사와 검증 기준

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
