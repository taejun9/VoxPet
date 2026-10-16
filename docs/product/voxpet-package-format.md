# .voxpet 패키지 규격 v2

`.voxpet`은 ZIP 안의 UTF-8 JSON과 PNG다. Core loader는 추출하지 않고 검증한 bytes/불변 정의를 메모리에 보관한다. [마이그레이션 계약](voxpet-2-migration.md)을 따른다.

## 문서와 asset

`manifest.json`은 필수다. `format=voxpet`, `formatVersion=2`, `id/name/version`, 선택 author/description/thumbnail, 기본 `character=character.json`, 선택 actions/attachments 문서 경로 배열을 담는다. 캐릭터 version은 문자열이며 formatVersion과 독립이다. character 참조 문서와 최소1PNG 파츠가 필요하며 action/attachment/thumbnail은 생략할 수 있다.

character 문서는 schemaVersion2, canvas.width/height(기본512×512), parts(id/type/file/anchor/side/zIndex/transform/visible), anchors 이름별 x/y/parent/scaleX/scaleY/rotation, 선택 mouth/expressions/poses/motion/face를 담는다. parent가 null/생략이면 Canvas 기준이고 지정하면 부모 기준 local 좌표다. 표준 Anchor와 custom ID를 지원한다. PNG 실제 해상도와 logical Canvas를 분리한다. visible 생략 시 true이며 false 파츠와 의존 얼굴은 숨긴다. Action visibility가 명시되면 그 값으로 재정의한다.

mouth는 frames mode,1~16 상대 PNG 경로, anchor, curve(smooth/linear/legacy3/legacy8), 선택 frameTransforms/zIndex다. expressions는 id/name/eyesOpen/eyesClosed/선택 brows다. poses는 id/name과 target별 sprites(null=숨김)/transforms snapshot이다. face는 눈/눈썹 Anchor/z-order 및 선택 requiredVisiblePart다.

motion은 blink(enabled/intervalMin/intervalMax/duration), idle(enabled/breathing/sway), voice(mouth/bodyBounce/headBounce/headRotation/mouthTravel), bodyAnchor/headAnchor/viewportMotion을 담는다. viewportMotion은 기존 UI 배율 밖 body 이동의 호환 정책이며 일반 제작은 Canvas 모션을 사용한다.

## Action과 attachment

action 문서는 schemaVersion2/id/name/priority/duration(ms), playback(mode=once/loop,interruptible,returnToIdle), triggers, timeline, 선택 compatibilityTag다. duration 생략 시 마지막 이벤트 시각/효과 길이로 유도하며 단일0시각 이벤트는1000ms다. 최소1ms, 종료 시각 pose를 허용한다.

각 timeline 항목은 time(ms)과 payload 하나만 담는다: pose ID, transform(target/x/y/rotation/scale 또는 scaleX/Y), visibility(target/visible), sprite(target/file), effect(file/anchor/duration/선택 transform), expression ID, attachment(id/enabled), sound(file 예약). Sound는 실행/파일 읽기를 하지 않으며 WAV 파일을 패키지에 넣지 않는다. 정의되지 않은 pose ID는 상태 label용 빈 pose로 표현하며 실제 그림/이동은 sprite/transform 또는 character poses에 작성한다. Action의 pose 값은 PoseDefinition.Id를 참조하고, 부착물 poses map은 평가된 PoseDefinition.Name(생략 시 ID)을 키로 사용한다.

trigger는 id(생략 시 자동ID), type(hotkey/mouse/voiceThreshold/interval/manual), key/button, modifiers(ctrl/alt/shift), threshold, interval(seconds),startImmediately/enabled다. ActionId는 부모 action에서 지정한다. 전역 감지 API는 Core에 포함하지 않는다.

attachment는 schemaVersion2/id/name/category/layers와 선택 compatibilityTag/exclusiveGroup이다. 분류는 [14종](voxpet-2-migration.md)을 사용하며 v1 다중 의상에는 legacyOutfit 호환 분류를 보존한다. layer는 id/file/anchor/zIndex/transform/inherit(position/rotation/scale/mouthMotion), 선택 poses/requiredVisibleParts/onlyDuringAction/hideOnMissingPose, space(canvas/anchor)/follow/width+height/deformWithRig다. width/height는 함께 지정하는 logical 크기다. garment 연결 격자가 필요한 레이어는 준비된 pure geometry provider가 있어야 동작 중 표시할 수 있다.

레이어의 선택 `animation`은 `{ "frames": ["hat-0.png", "hat-1.png"], "frameDurationMs": 80 }` 형식이다. 상대 PNG2~64개, 유한한 간격16~10000ms를 검증하고 일반 asset과 같은 크기·누락·decode 상한을 적용한다. 생략/null이면 기존 정적 `file`을 사용한다. 지정하면 Runtime의 초 단위 시간으로 반복하고 액션이나 음성과 독립적으로 진행한다. 매칭하는 `poses` PNG가 있으면 그 그림이 우선하며 미지정 포즈는 기존 hide/fallback 규칙을 따른다. 각 프레임과 간격은 native attachment index·편집·내보내기에도 보존된다.

## 안전성과 자원 상한

기본 상한은 archive64MiB, 실제 해제 합계64MiB, PNG16MiB/JSON512KiB, directory 포함 entry512개, PNG4096px/8M pixels, 전체 decoded RGBA128MiB다. logical Canvas의 너비/높이는 각각1~16384다. 극소 값의 표시 배율 overflow와 과대 WPF geometry를 막기 위해 로드·편집·내보내기에서 같은 범위를 검사한다. ZIP64/multi-volume/encrypted/지원하지 않는 압축 방식은 거부한다. central directory 개수/구조를 ZipArchive 객체 확장 전에 검사하며 실제 payload 길이·CRC도 확인한다.

경로는 package 안의 PNG/JSON 상대 경로만 허용한다. 절대/drive/UNC/상위 이동·빈 segment·역슬래시·제어/예약 문자를 거부한다. Unicode NFC와 case-insensitive 중복, symlink, Windows device 이름 및 끝 공백/점을 검사한다. 이 규칙은 [Microsoft 파일명 자료](https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file)와 [ZIP entry 경로 자료](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchiveentry.fullname?view=net-10.0)를 참고했다(확인2026-10-14).

JSON은 depth32와 중복 속성을 검사한다. PNG는 signature/IHDR/8bit 형식/크기/chunk CRC/IEND 및 zlib 실제 scanline 길이·filter를 검사하고 APNG는 거부한다. 작은 PNG 선언으로 내부 해제량을 숨기는 경우도 거부한다. WPF 실제 decode는 App worker에서 추가로 수행한다.

## 로드·편집·내보내기

VoxPetPackageLoader/Validator는 준비된 package abstraction을 만든다. VoxPetPackageSource는 원본 형식을 Runtime에 드러내지 않고 CharacterDefinition만 제공한다. PNG는 원본 bytes를 보존하며 읽기 API는 복사본을 반환한다. Exporter는 공통 JSON codec으로 문서와 모든 보유 PNG를 ZIP으로 만들고 같은 loader로 다시 검증한 뒤 파일을 원자적으로 교체한다. 지원 데이터/PNG/Runtime 프레임의 왕복을 시험한다. 알 수 없는 JSON 확장 속성은 지원 계약에 포함하지 않는다.

AttachmentStore의 native index는 attachments-v2.json이며 공통 attachment codec을 사용한다. 기존 wardrobe.json과 PNG/JPG/BMP/GIF 관리 원본은 계속 읽고 수정하지 않는다. 잘못된 index는 recovered 상태로 읽으며 원본 파일을 덮지 않는다.

## full-sprite 시트 호환 표현

분리 mouth와 별개로 character의 선택 `spriteSheet`는 `{part, curve, expressions:[{id, open:[PNG…], closed:[PNG…], blink}]}`를 지원한다. part는 기존 파츠ID, expression id는 기존 표정ID이며 모든 bank는 동일1~16입 단계다. curve는 일반 mouth와 같고 기존3/8열은 legacy3/legacy8이다. 분리 mouth와 동시에 지정하지 않는다. Runtime은 같은 mouth/blink 시간축으로 해당 파츠의 PNG를 선택하고 분리 eyes/mouth를 중복으로 덧그리지 않는다. 모든 bank PNG의 검증·원본 bytes·export/load 왕복을 동일하게 적용한다.


## 편집 보존과 표준 경계

CharacterDocument는 변경하지 않은 지원 모델·manifest의 character/action/attachment 참조·사용하지 않는 보유 PNG도 보존한다. metadata만 바꾸거나 no-op edit한 파일의 지원 데이터/원본 bytes를 시험한다. 사용자 변경으로 참조 무결성이 깨지면 적용을 거부하고 이전 문서를 유지한다. 새 asset은 managed 상대 PNG 경로로 추가하며 원본 파일을 수정하지 않는다. Editor와 native 방송의 동일 입력 frame 왕복도 검사한다. unknown JSON 확장 속성을 무손실 편집하는 기능은 제공하지 않는다.

필수 최소 예제/공개 제작 틀과 지원 필드의 실제 codec은 VoxPetPackageTests, CharacterDocumentTests, CharacterEditorQa가 검증한다. Runtime thread/renderer 계약과 Legacy 보존은 [아키텍처](../architecture/voxpet-2-runtime.md), [최종 완료 근거](migration-completion-audit.md)를 따른다.
