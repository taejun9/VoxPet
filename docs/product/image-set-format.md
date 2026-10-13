# 이미지 조합 세트 v1

VoxPet은 외부에서 준비한 그림을 입력에 따라 조합한다. 앱 안에서는 파츠 좌표·회전·그림을 편집하지 않는다. **캐릭터 → 이미지 세트 → 제작 틀 저장**으로 실행 가능한 합성 예제 JSON과25PNG를 받는다. 예제는 실제 늘보군·펠리 그림이 아니라 제작 규격을 시험하는 도형이며, 원하는 그림으로 교체한다. 캐릭터 선택은 늘보군과 펠리 두 종이다.

## 등록과 보관

선택한 캐릭터의 `imageset.json`을 등록한다. 버전1, `CharacterId`는 `neulbo` 또는 `pelly`, CanvasSize는256·512·1024(512권장)이다. 모든 PNG는 이 크기의 정사각형 RGBA이고 제자리에 그린다. PNG마다 빈 영역까지 동일하게 유지한다. 잘라낸 이미지를 등록하면 위치가 맞지 않는다. 좌표는 화면 기준이며 좌우도 화면 기준이다.

경로는 세트 폴더 안의 상대 PNG 경로만 허용한다. 절대 경로·상위 폴더·URL·링크를 거부한다. JSON256KiB, PNG128개, 압축 합계32MiB, 디코딩 합계128MiB 이내다. 등록 전에 전체 세트를 검증하고 `%LocalAppData%/VoxPet/ImageSets/<무작위ID>/`로 복사한다. 원본을 수정하거나 전송하지 않는다. `image-runtime.json`에는 내부 세트 ID, 선택 인덱스, 입력 설정만 저장한다. 재시작 때 선택한 캐릭터의 마지막 세트를 복원하며 손상되면 기존 PNG 경로로 복구한다.

## 파츠와 레이어

기본 `Parts`는 Head·Body·ArmL·ArmR·LegL·LegR 각1장, 정확히6개다. 의상과 얼굴장식은 별도 Outfits 레이어에 넣어 숨김·Anchor 규칙을 따른다. 머리 기본 PNG에 눈/입을 그려 넣지 않는다. 눈·눈썹·입이 따로 조합되므로 완성 얼굴을 사용하면 중복된다. 펠리도 같은 이름을 쓰되 ArmL/R에는 날개 그림을 배치한다. 필요 없는 측면 파츠는 같은 Canvas의 완전 투명 PNG로 등록한다.

고정 순서:

`CapeBack → ArmL → LegL → LegR → Body → Lower → Upper → Shoes → Head → Eyes → Brows → Mouth → FaceDecoration → ArmR → Weapon → Effect`

같은 슬롯에서 캐릭터 파츠 위에 해당 의상 레이어를 그린다. ArmL은 뒤팔, ArmR은 앞팔이다. 동작 중 뒤로 보내야 하는 팔은 그 프레임에서 앞팔을 숨기고 ArmL 교체 그림에 그린다. 새 이미지 엔진은 기존 PNG 리깅·손동작 소품·의상 렌더를 겹쳐 그리지 않는다. 기존 세트는 **기존 PNG 사용**으로 복귀한다.

## 표정·말하기·Anchor

`Mouths`는 닫힘부터 크게 열림까지3장 또는8장이다. 음량이 입 단계를 선택한다. `Expressions`는 Neutral 필수, 전체12종 이내이며 `Kind`는 기존 ExpressionKind 이름이다. `EyesOpen`, `EyesClosed`, 선택적 `Brows`를 제공한다. 없는 종류는 Neutral로 복구한다. 기존 표정 버튼·단축키도 적용된다. 새 조합 모드의 표정은 등록 눈/눈썹으로 표시하며 기존 PNG 슬롯의 완성 얼굴과 눈물 이펙트를 합성하지 않는다.

의상 `PoseLayer.Anchor`는 Canvas·Head·Eye·Face·Mouth 중 하나다. 안경은 Head/Eye/Face, 콧수염은 Mouth를 사용한다. 모든 그림은 원래 위치에 배치하며 Anchor 값은 추가 이동량이다. 액션 프레임의 `Anchors.Head`가 머리와 얼굴/장식 모두에 적용된다. Eye/Face/Mouth는 여기에 각 Anchor 이동량을 더한다. `MouthOffsets`는 입 단계와 같은 수의 X/Y 배열이며 입과 Mouth 장식에 추가 적용된다. 이동량은 각 값±128px 이내이고 Canvas는 이동하지 않는다. Head를 숨기는 프레임에서는 눈·입과 머리에 붙은 의상도 숨긴다.

## 액션·무기·소매 규칙

`Actions`의 `Name`, `Template`, `Frames`를 등록한다. Template은 Gesture·OneHand·TwoHand·Firearm·Bow이다. 액션은16개, 각1~64프레임,16~2000ms/프레임, 합계30초 이내다. 프레임은 `DurationMs`, `Pose`, `Replace`, 선택적 `Anchors`를 가진다.

`Replace: {"ArmR":"arm-fire.png"}`는 기본 오른팔을 숨기고 지정 PNG 한 장으로 대체한다. `{"ArmR":null}`은 팔과 해당 소매를 숨긴다. 각 프레임은 기본 상태에서 독립적으로 조합하며 이전 프레임의 Replace를 상속하지 않는다. 액션이 끝나면 기본 상태로 복귀한다. 새 입력은 진행 중인 액션을 취소하고 첫 프레임부터 즉시 재시작한다. 대기열은 없다.

무기와 의상은 `Layers` 배열이며 각 레이어에 `Part`, `Anchor`, `Poses`가 있다. 모든 레이어에 `idle`이 필요하다. 예: `Poses: {"idle":"gun-idle.png","ready":"gun-ready.png","fire":"gun-fire.png","return":"gun-return.png"}`. 무기 본체는 Weapon, 총구 화염·탄/화살 등은 Effect이다. Effect의 없는 포즈는 표시하지 않으며 idle에서는 표시하지 않는다. 무기는 템플릿이 일치하는 액션과만 결합한다. 같은 템플릿의 모든 프레임 포즈에 무기 본체가 있어야 등록된다. 관리 미리보기와 idle 출력은 동일 PNG를 사용한다. 관리 썸네일은 투명 여백만 잘라 크게 보여 주고, 실제 출력은 원래 Canvas와 방향을 유지하며 앱이 임의로 회전하지 않는다.

방향은 오른쪽을 기준으로 준비한다. Firearm은 총구 오른쪽, Bow는 오른쪽 발사, TwoHand는 오른쪽, OneHand의 idle은 오른쪽15°다. 휘두르기/발사 각도는 포즈별 PNG로 준비한다. 이 방향은 제작 규격이며 픽셀의 의미를 자동 판별하지는 않는다.

상의는 Upper(몸통) + ArmL(왼 소매) + ArmR(오른 소매)로 나눈다. 하의 Lower도 Body 포즈를, Shoes는 양다리 포즈를 따른다. 교체되는 해당 부위의 모든 포즈에 의상 이미지가 있어야 등록된다. 숨겨진 부위의 의상은 표시하지 않는다. 액션 팔에 소매를 미리 그려 넣었다면 별도 소매 레이어를 등록하지 않는다. 한 의상 프리셋만 선택하며 다음/이전 프리셋에 '없음'도 포함한다.

## 키와 동시 입력

**액션 → 프레임 액션**에서 실행/다음·이전 액션/다음·이전 표정/다음·이전 의상/기본 복귀를 선택하고 원하는 키를 직접 누른다. 문자·숫자·F1~F12·Space 및 Ctrl/Alt/Shift 조합을 지원한다. Esc는 취소, Delete/Backspace는 해제다. Windows 키, 중복 키, 기존 Ctrl+Shift+M/F키 예약 조합을 거부한다. 선택적 좌클릭은 클릭마다 액션을 재시작한다.

전역 입력은 지정된 가상 키·좌클릭 down 신호만 감지하며 원래 입력을 게임에 전달한다. 키 반복 down은 키를 놓을 때까지1회만 실행한다. 입력 설정 중에는 훅을 일시 해제하고 종료·세트 전환 때 대기 입력과 훅을 해제한다. 등록 실패는 화면에 안내하고 재생 버튼으로 시험한다. 문자 내용·좌표·게임 창 정보는 저장하지 않는다. 보안 데스크톱·권한이 다른 게임·게임 자체 입력 차단은 실기 확인이 필요하다.

## 업로드 이미지 예제 묶음

`harness/scripts/export_upload_examples.py`는 기존 코드 기반 합성 예제를 확장한 교육용 묶음을 지정한 새 폴더와 ZIP으로 만든다. 개발용 Pillow가 필요하며 기존 폴더/ZIP이 있으면 거부한다. 예: `python3 harness/scripts/export_upload_examples.py ~/Downloads/VoxPet-upload-examples`. 실제 개인 캐릭터 그림을 읽지 않는다.

묶음에는512px RGBA PNG76개(필수6파츠·입8단계·표정4종·3의상/장식 프리셋·4무기·5템플릿과 팔 숨김 액션), 늘보군/펠리 ID별 JSON2개, 기존3×2/8×2 완성 시트8개, 조합 미리보기27장과 HTML/한국어 안내가 있다. 두 JSON은 같은 교육용 그림을 각각의 ID에 연결하며 실제 늘보군·펠리 개인 원본과 구분한다. 미리보기는 업로드하지 않는다. 새 방식은 JSON, 기존 방식은 완성 시트를 선택한다.
