# plan-045-character-samples Review

## Summary

별도 샘플 인형 대신 선택한 늘보군/펠리의 원래 그림·표정·입/눈 반응에 의상17종/14분류와 동작9종을 직접 연결했다. 모자8프레임·날개6프레임과 단발/장발/폭탄머리는 수채화/과슈 그림으로 개선하고 두 체형에 따로 맞췄다. 앱1.1.0, 검증한 앱/자산/하네스 기준은 a8cdcbade67ea7dd3e3568508d4d2548698f7203다. 전체 QA 후 동일 에이전트가 별도 단계로 자체 리뷰했다.

## QA

- 로컬/Windows SDK10.0.401 locked restore·format/analyzer·Release 경고0/오류0·Core405/405/skip0·verify_base/verify_app/compileall/diff 통과. 로컬 formatter의 sandbox IPC 거부는 승인된 실행 환경에서 재시도해 통과했다. 이전 임시 SDK 파일 누락은 공식 배포본/SHA512 대조로 복구했다.
- 최종 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38938790352): WPF2370고유검사/실패0·실제 desktop396화면/PNG CRC·일반/서명 각9개 시작10초/정상 종료/저장 파일 보존·서명 Valid/변조 HashMismatch/아이콘/임시 신뢰·키 정리 통과. 실제 CI ZIP을 새 폴더에 풀고 Windows PowerShell5.1 동봉 도구로1992고유검사/실패0 확인.
- 이전 plan044의2090개 중2087개 이름을 그대로 유지했다. `card_library_automation_name_voxpet.samples.studio`/`studio_sample_card_bundled`는 새 일반 목록에서 별도 인형을 제공하지 않으므로 `companion_catalog_has_only_original_two_characters`/`studio_no_standalone_sample_card`와 두 캐릭터 직접 사용 검사로 대응한다. `card_library_legacy_sample_native_labels_and_images`는 `card_library_legacy_and_native_labels_and_images`로 이름을 바꿨다. native/import/editor·문자열 오류 회귀는 유지했다. 새283개 검사 추가.
- 두 캐릭터 각각 원래 선택/표정/입3·8단계/눈/원본 SHA 유지·17착용/해제/썸네일·9고유 자세/idle복귀·기존 손동작 엔진 공유·머리3+모자+날개·얼굴 안전 영역·4반복 프레임·미리보기/방송 공유·편집의 현재 시트/120ms 변경/내보내기/재읽기·자기 의상 복원·중지·이전 인형 파일을 삭제하지 않는 업그레이드/재시작을 검사했다. 입력 factory0. 기존 의상과 같은 분류/한벌 충돌은 저장된 Enabled를 바꾸지 않고 샘플 해제 후 복원하는 Core 회귀로 확인했다.
- 최종 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38938790502): OBS32.2.2 WGC 초록/크로마/native alpha·동일 HWND/소스 배경 왕복·각7프레임 변화 통과. 녹화/송출/마이크 없음. BitBlt 실패는 기존 layered 창 제한이다.
- 실제 공개 Windows76화면(새72+기존 카드/편집4), 개인 로컬 정적64합성/11contact page, OBS4캡처를 직접 검토했다. Windows3화면은 원래 크기도 확인했다. 검토 중 장발의 얼굴 침범/펠리 날개의 각진 경계/수평 헤어 절단/날개 이웃 조각을 발견해 새 장발 시안·곡선 앞날개/얼굴 마스크·프레임별 자르기로 고쳤고 최종 빌드 전체를 재시험했다. 처음 펠리 모자를 찍은 화면의 겹침은 선택100ms 뒤240ms 전환 중인 이전 캐릭터 snapshot이며 후속 화면에서 정착했다. 편집기 좁은 목록의 긴 이름은 상세 이름 필드로 확인하며 작은 창은 스크롤한다. 추가 필수 수정 사항 없음.
- `artifacts/VoxPet-Windows-test-plan045.zip`:54파일/103,671,141bytes, SHA256 `a225fc1de3365a4e798bb81d57bf47f715b349f5ede58d72afc4ccba874490db`, EXE SHA256 `2fbf5a80e5f1f985443ff441461fd69b9cde821f87bb2d158362c8f77b3a91a2`. 공용41파일은 시험한 최종 CI ZIP과 동일하며 개인13PNG는 이전 plan044 ZIP에서 이전 해시와 대조해 byte-identical로 추가했다. 새2팩/기존 fixture 소스 일치·PE x64/CRC 통과. 이전 ZIP 덮어쓰기 없음.
- 증거는 `artifacts/test-results/plan045/`의 local/final Windows·OBS 결과/로그/화면과 검증 JSON에 보존했다. 원본 생성물15개·프롬프트/SHA·내장 imagegen 사용 경로는 [그림 기록](../art/companion-samples.md)에 둔다. 개인 그림은 생성 도구/Git/CI에 보내지 않았다.

## Findings

하나의 중앙 ActionEngine을 유지하며 기존 scheduler가 새 문자열 action ID를 enum으로 오해하지 않게 했다. 의상/소품은 최종 RenderFrame에 합쳐 방송창과 같은 결과를 공유하고 worker에서 해독·동결한 공개 그림을 두 팩만 캐시한다. 파일 읽기/그림 해독을 매 프레임 실행하지 않는다. 캐릭터 선택의 최신 요청/종료 가드, 기존 실패 복구와 원본 보존을 유지했다. 샘플 없는 경우 재시도할 수 있고 현재 캐릭터를 지우지 않는다. plan043의 Name 전용 ToString/명시적 검색·접근성 이름 회귀가 계속 통과해 기존 decoder thread 오류 수정을 유지한다. 오디오/마이크 정책과 README/AGENTS, 두 plan024 작업트리는 변경하지 않았다.

## Residual Risk

Windows 자동 실행은 공개 교정 도형/가짜 입력을 사용했다. 개인 그림은 로컬 정적 합성과 파일 해시로 검토했으며 Windows에서 실행한 것처럼 주장하지 않는다. 물리 마이크/권한/USB·사용자 GPU/OBS/DPI·새60분 실기는 별도다. 모든 환경의 결함0을 보장하지 않는다. EXE는 unsigned이며 일회용 runner의 서명 QA가 사용자 신뢰 저장소를 바꾸지 않는다. BitBlt 대신 WGC를 사용한다.

캐릭터별 샘플 착용 선택은 현재 실행 중에 기억하고 새 실행에는 해제한다. 마지막 캐릭터/기존 설정·표정은 기존 저장 규칙을 따른다. 의상 편집의 명시적 내보내기는 현재 개인 표정 그림을 포함한다. 원래 시트의 팔/날개를 안전 범위에서 변형하며 새 손가락 그림이나 자동 관절 추정을 제공하지 않는다.

## Follow-Ups

기존 앱을 종료하고 새 폴더에 ZIP 전체를 풀어 START.cmd를 실행한다. 캐릭터에서 늘보군/펠리 선택 → 의상 착용 → 동작 재생. [Studio 안내](../guides/studio-samples.md), [Windows 체크리스트](../quality/windows-checklist.md)를 따른다. TEST-SMOKE.cmd는 실제 합성 키/클릭을 사용하므로 전용 Windows/VM에서 실행한다.
