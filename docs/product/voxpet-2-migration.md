# VoxPet 2.0 개발 계약과 화면 기준

2026-10-14 사용자 제공 와이어프레임10화면과 마이그레이션 텍스트를 기준으로 작성했다. 이 문서는 목표와 단계별 완료 조건이며 현재 앱의 지원 기능 안내는 아니다. [기존 이미지 세트 v1](image-set-format.md)과 저장된 설정은 계속 지원한다.

## 진행 순서와 보존 계약

기존 구현 옆에 신규 Core 모델/엔진을 만들고 Adapter → 병행 검증 → App 전환 → 삭제 가능성 검토 순서로 진행한다. 매 단계 별도 실행 계획, 구현, 테스트, QA 후 리뷰와 Git 수명 절차를 완료한다. Phase 1~8은 기존 UI를 보존한다. Legacy는 호환성 테스트가 증명한 이후에도 파일/설정 이관에 필요한지 확인해야 한다.

| 단계 | 목표 | 검증 기준 |
|---|---|---|
| 1 Foundation | CharacterDefinition/Part/State/Pose, AnchorDefinition/Transform2D, RenderFrame/Layer, VoxPetManifest | 불변 snapshot, 유한 수치, 캔버스, 문자열ID, 부모 cycle·참조 검증 |
| 2 Legacy Adapter | ImageSet → CharacterDefinition | 제공 제작 틀의 파츠/입/표정/Anchor/의상/무기/프레임 시간을 무손실 표현 |
| 3 Motion Engine | Voice/Idle/Blink 합성, CharacterAnimator 호환 wrapper | 호출자가 제공한 시간/Random으로 재현 가능, 기존 동작 보존 |
| 4 Action Engine | pose/transform/sprite/visibility/effect/expression/attachment timeline | 우선순위, 중단, idle 복귀, 지연 프레임, HandMotionScheduler 호환 |
| 5 Trigger Engine | hotkey/mouse/voice threshold/interval/manual | 플랫폼 독립 이벤트, 기존 표정/음소거 키 보존 |
| 6 Attachment System | Anchor 상속과 분류 충돌, Costume adapter | 기존 wardrobe 읽기, 한벌과 상·하의 충돌, 입에 붙은 콧수염 이동 |
| 7 CharacterRuntime | Trigger/Action/Motion/Attachment 조정 → RenderFrame | idle → voice → action → idle 통합, 렌더러에 판단 로직 없음 |
| 8 Package | ZIP+JSON+PNG `.voxpet` loader/validator | traversal/절대 경로/중복/확장자·크기·파일수·JSON·PNG 제한과 악성 패키지 시험 |
| 9 App Integration | Runtime VM/service 뒤에 연결, MainViewModel 책임 축소 | UI 시각 개편 없이 기존 마이크/방송/OBS 동작 보존 |
| 10 Editor | 생성 → 파츠 → Anchor → 얼굴/입/표정 → 동작 → 부착물 → 미리보기 → 검증 → 내보내기 | 방송과 동일 Runtime, 시각 Anchor 배치, timeline/부착 미리보기, 편집 왕복 보존 |

마지막 전체 리뷰에서 Legacy의 참조/대체/호환 목적/삭제 가능성을 보고하고 runtime·패키지 아키텍처 문서를 확정한다. 기존 sprite-sheet/ImageSet, 표정, 의상, 손동작, 마이크, OBS, 사용자 설정을 단계 내내 보존한다. 음성 저장·원격 전송·웹캠·추적·플러그인은 이번 계약에 포함하지 않는다. SoundEvent는 규격 예약만 하며 재생하지 않는다.

## 와이어프레임10화면 해석

| 화면 | 화면 구성과 동작 | 구현 단계 |
|---|---|---|
| 1 메인 대시보드 | 왼쪽 홈/캐릭터/의상·부착물/동작/방송·설정 탐색, 중앙 캐릭터 미리보기·실행 상태, 오른쪽 마이크 미터/표정·입 프레임·동작·우선순위/빠른 동작, 하단 투명 방송 출력·해상도/FPS/방송창 열기 | 10 시각 개편; 9는 내부 연결만 |
| 2 캐릭터 선택/관리 | 검색·카드 목록·추가, 선택한 캐릭터 정보와 선택/편집/내보내기/삭제 | 10 |
| 3 편집 기본 정보 | 이름/ID/캐릭터 버전/제작자/설명/썸네일, 캔버스 너비·높이, 편집 하위 탐색 | 1 모델, 10 화면 |
| 4 부위/이미지 | 파츠 목록·추가/표시, ID/유형/Anchor/Z-index, 이미지 교체·삭제 | 1 모델, 10 화면 |
| 5 Anchor | 그리드와 캐릭터 위 시각 포인트·라벨, Anchor 목록·추가, 이름/X/Y/부모/시각화 설정 | 1 계층 검증, 7 합성, 10 배치 UI |
| 6 얼굴(입/눈) | 입/눈 표정/눈 깜빡임 탭, 프레임 모드·1~16 입 이미지·추가/삭제, 음량 슬라이더 미리보기 | 2/3/7 모델·엔진, 10 화면 |
| 7 표정 | 표정 목록·추가, ID/이름, 눈 뜸/감음 PNG 교체 | 2/7 정의·상태, 10 화면 |
| 8 동작 | 동작 목록·추가, ID/이름/우선순위/재생 설정/중단·idle복귀, 트리거 추가 | 4/5 엔진, 10 설정·timeline 미리보기 |
| 9 부착물 | 목록·추가, ID/이름/분류, PNG 레이어·Anchor·위치·크기·회전과 상속 옵션 | 6 엔진, 10 화면 |
| 10 미리보기/내보내기 | 투명 배경 Runtime 출력, 마이크 시뮬레이션·표정/동작 선택·재생, 검증·`.voxpet` 내보내기 | 7 Runtime, 8 패키지 기반, 10 편집·export |

와이어프레임의 늘보군/펠리 그림, 동작 이름과 FPS·출력 크기는 시각 예시다. 개인 캐릭터 그림을 신규 공개 자산으로 넣지 않는다. 새 그림 생성이나 별도 미리보기 애니메이션 엔진 없이 기존 자산·공용/합성 시험 자료와 동일 CharacterRuntime을 사용한다.

## Phase 1 모델 계약

- `VoxPet.Core.Character`: 생성 시 검증하는 get-only record, 외부 컬렉션은 복사한 읽기 전용 snapshot이다. JSON DTO와 변환은 패키지 단계에서 정의한다.
- `CharacterDefinition`: schemaVersion2, 기본512×512이며 유한한 양수의 비정사각 캔버스도 허용. 최소1파츠, 파츠/Anchor ID는 각각 유일하며 case-sensitive이다. 신체 유형과 ID는 고정 enum 대신 문자열이다.
- `AnchorDefinition`: parent 생략 시 캔버스 좌표, 지정 시 부모 기준 local 변환. 누락 부모와 순환을 거부한다. custom Anchor와 임의 깊이의 계층을 허용한다. 표준 이름은 root/body/head/face/eyes/mouth/leftShoulder/rightShoulder/leftHand/rightHand/hips/leftFoot/rightFoot/back이며 강제로 모두 생성하지 않는다.
- `Transform2D`: X/Y는 논리 좌표, rotation은 degree, scaleX/Y는0이 아닌 유한 배율이다. 음수 배율은 반전이다. 최종 계층 합성은 후속 엔진에서 수행한다.
- `CharacterState`: 유한0~1 VoiceLevel, 표정/동작/부착물 선택 snapshot. `CharacterPose`: 유한0~1 입/눈 열림, ID별 local transform snapshot. 실제 선택/모션 계산은 후속 단계다.
- `RenderLayer`: 안정ID, 상대 PNG asset 참조, X/Y/scaleX/scaleY/rotation/opacity/zIndex. `RenderFrame`은 레이어ID 중복을 거부하고 zIndex 순으로 정렬한다. 같은 zIndex에서는 입력 순서를 유지한다.
- `VoxPetManifest`: format=`voxpet`, formatVersion=2와 character version을 분리한다. character.json 기본 참조, thumbnail/action/attachment 문서는 선택사항이다. 모델은 상대 경로 형식만 검증하며 실제 파일/ZIP 안전성은 아직 제공하지 않는다.

## 후속 데이터/엔진 결정

`.voxpet`은 manifest.json + character.json + PNG가 최소 구성이다. actions/*.json, attachments/*.json, thumbnail.png는 선택사항이다. 포맷 버전과 캐릭터 버전을 분리하고 기본 논리 캔버스512×512와 PNG 해상도를 분리한다. 입은1~16프레임과 `round(MouthCurve(VoiceLevel) × (frameCount-1))`로 일반화하며 반올림 정책은 구현 단계에서 시험으로 확정한다.

동작 우선순위는 Idle0, Voice10, Expression20, Automatic30, User50, Weapon80, System100이다. 부착물 분류는 hat/glasses/faceDecoration/earrings/top/bottom/onePiece/shoes/outerwear/cape/headAccessory/backAccessory/handAccessory/effect이며 onePiece는 top/bottom과 충돌한다. 상속은 position/rotation/scale/mouthMotion을 독립적으로 설정한다.

Runtime은 원본 형식을 알지 않고 정의를 받는다. LegacyImageSet/LegacySpriteSheet/VoxPetPackage source가 같은 정의로 변환한다. Core에는 WPF/NAudio/Win32를 넣지 않는다. tick에는 파일/JSON 처리, 오디오 callback에는 동기 UI dispatcher 호출을 넣지 않는다. 애니메이션은 호출자가 제공한 monotonic time을 사용한다.

Phase 1 실행/검증은 [완료 plan-028](../exec_plans/completed/plan-028-runtime-foundation.md)과 [자체 리뷰](../reviews/plan-028-runtime-foundation-review.md)에 기록한다. 사용자는 이후 모든 단계의 연속 진행을 승인했다. 단계마다 별도 계획/QA/리뷰/Git 수명을 완료하며 전체 목표는 Phase 10과 최종 마이그레이션 리뷰다.

Phase 2는 mouth/expression/pose/action/attachment 불변 데이터와 LegacyImageSetAdapter를 추가한다. 원본 v1 객체를 신규 정의 내부에 보관하지 않는다. full-canvas PNG는 원래 논리 크기/좌표를 유지하며 Legacy3/Legacy8 입 threshold를 명시적 곡선으로 표현한다. 프레임별 고유 pose ID와 원래 포즈 이름을 함께 보존해 같은 fire 이름의 여러 프레임도 소실하지 않는다. Layer는 소매/부위 숨김 의존성과 무기 템플릿 호환/효과 표시 정책을 담는다. 부모 Anchor를 이용한 실제 합성과 엔진 실행은 후속 단계에서 한다.

Phase 3은 MotionEngine의 Voice/Idle/Blink source와 기존 CharacterAnimator wrapper를 구현했다. wrapper는 기존3단계 입/6DIP body/2~6초 blink를 유지한다. caller time/delta만 사용하고 source의 local 이동/회전은 합산, 배율은 곱, 입은 max, 눈은 min으로 합성한다. 변환 계층과 Action 우선순위는 이후 Runtime 단계에서 결합한다.
