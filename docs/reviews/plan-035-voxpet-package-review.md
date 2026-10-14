# plan-035-voxpet-package Review

## Summary

[완료 계획](../exec_plans/completed/plan-035-voxpet-package.md)의 package loader/validator/codec/exporter와 native inventory 저장을 QA 후 단일 에이전트가 자체 리뷰했다. [규격](../product/voxpet-package-format.md).

## QA

macOS arm64/SDK10.0.401. locked restore, format verify, 솔루션 Release build 경고0·오류0, Core338/338(기존317+신규21), skipped0, verify_base/verify_app와 stage diff check. TRX는 artifacts/test-results/plan035/core.trx에 보존한다. Phase 7 Windows/OBS CI 성공 확인. 최초 nullable effect transform 왕복 차이를 수정한 뒤 전체 재검증했다.

## Findings

미해결 Phase 8 결함 없음. ZIP를 추출하거나 프로그램을 실행하지 않는다. central directory는 count/구조/압축 방식/ZIP64·disk 정보를 preflight하고 loader는 실제 읽은 payload 길이·CRC·합계 상한을 검증한다. 경로 traversal/절대/drive/역슬래시/빈 segment/예약 이름/끝 점·공백, NFC/case 중복, symlink, unsupported extension과 directory marker 처리를 시험했다.

압축 archive, entry 수, PNG/JSON 크기, 총 해제/decoded·PNGdimension 상한을 각각 시험했다. PNG chunk CRC/signature/IHDR/IEND와 zlib scanline 길이를 검사하고 선언된1×1보다 실제 data가 큰 PNG를 거부한다. malformed JSON·중복 속성/버전/자산 참조 누락도 거부한다.

최소 character는 action/attachment/thumbnail 없이 로드한다. formatVersion과 character version은 독립이다. v1 제공 제작 틀을 export/load하고 PNG bytes, motion/face/pose/timing 및 Runtime layer/affine/크기가 보존되는지 비교했다. generic 이벤트/trigger/attachment inheritance·placement도 왕복한다. PNG 읽기 복사본을 수정해도 package 원본이 바뀌지 않는다.

native attachment index는 같은 codec과 atomic save를 사용하며 손상 재읽기가 원본을 덮지 않는다. 기존 wardrobe/managed 원본 bytes와 App/오디오 경로는 유지한다. animation tick에 IO/JSON을 추가하지 않았다.

## Residual Risk

PNG 검사는 Core 수준이며 실제 WPF decode 오류 처리는 App worker에서 추가 검증해야 한다. unknown JSON extension fields는 지원 데이터가 아니며 codec에서 보존하지 않는다. ZIP64/암호화/APNG/8bit 외 PNG 형식은 지원 범위 밖이다. Legacy garment provider와 sprite/pixel compatibility 및 실제 editor import→edit→export는 Phase 9/10의 gate다.

## Follow-Ups

Phase 9 CharacterRuntimeViewModel/renderer 및 package 보관함을 연결하고 WPF/OBS/기존 설정·시트/mesh 회귀를 검증한다. Phase 10은 같은 Runtime/codec을 쓰는10화면 editor와 시각 Anchor/timeline preview를 구현한다.
