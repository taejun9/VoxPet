# plan-019-wardrobe Review

## Summary

8분류 의상/단일 실행/같은 방송창 배경 전환/아이콘·김태중 개인 서명 요청을 QA 이후 동일 에이전트가 자체 리뷰했다. 서브에이전트 없이 수행했다. 최종 코드73c4b34, OBS 시험 이후 src 변경 없음. 개인 늘보군/사용자 개인키를 공개 Git/CI로 보내지 않았다.

## QA

- macOS SDK10.0.401 locked restore/format/analyzer/Release 경고0·오류0/Core151, verify_base.py/verify_app.py/diff 통과.
- [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38200870611): Core151/151, WPF844고유검사 실패0,6탭×4창크기 선택값/팝업 검정 글씨·실제 의상 항목/60자 이름 접근·페이지 스크롤 없음. 의상8분류/복수 얼굴·귀걸이/한벌과 상하의 배타·겉옷 공존/비율 크기·미세 XY/겹침 순서/관리 원본·저장·재시작·손상 이미지/잘못된 추가의 이전 상태 보존/메모리 fixture 비저장. 공유 renderer의 미리보기/방송 합성 픽셀과 실제 UI PNG를 확인했다.
- unsigned/signed 일반 EXE 각각5사례×10초, 초기화/저장 슬롯·순서 불변/정상 종료. 실제 추가 EXE0종료·기존 tray HWND 복원. Start/트레이/방송창 수명·입 모션 회귀 유지.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38200259371): OBS32.2.2/Server2025 build26100/Basic Render Driver. WGC 초록+크로마·7프레임 변화, 투명 native alpha65.08%, 같은 소스/같은 HWND의 초록→투명→초록 캡처. BitBlt 유효 프레임 없음 제한을 별도로 기록했다. 캡처 PNG 직접 확인.
- EXE Company=김태중, 실제 Authenticode signer=김태중. 신뢰 전 UnknownError, 임시 LocalMachine Root 신뢰 후 Valid, 변조 HashMismatch. signed EXE 정상 시작/종료 검사. 공개 CER SHA256/지문·CN/3072bit 확인, 개인키/PFX 미포함·QA finally 정리. PE 리소스의 아이콘7해상도는 원본 ICO payload와 정확히 일치한다.
- 개인 ZIP27파일/93,271,126bytes/CRC 통과. SHA256 a08844c8aa7c317d6fb1a7b90b46864eb2738baa178b77159cad31b7ce9ce291. 이전12PNG/원본/ZIP 모두 보존. 증거 artifacts/qa/plan019/.

## Findings

- 저장/경계: 이미지 파일은 GUID 이름으로만 관리하고 원본은 수정하지 않는다.100개 보관/24개 착용,16MB/4096px/800만 화소 검증·최대1024px frozen 디코딩. 비착용·비선택 이미지는 해제한다. JSON 유한 좌표/크기·enum/중복ID/null 항목을 검사하고 원자적 교체한다. 실패 시 이번에 생성한 복사본/임시 파일만 정리하며 기존 목록과 사용자 파일은 유지한다. 손상 파일을 자동 삭제하지 않는다.
- 착용/렌더: 같은 분류 단일 선택과 장식 복수 선택, 한벌과 상하의 배타·겉옷 공존이 양방향으로 적용된다. 레이어는 하나의 UI 컬렉션이고512좌표/균일 비율·대기 움직임을 공유한다. 조정값은 명시 저장하며 추가/삭제는 현재 전체 목록을 저장한다. 문서에 차이를 명시했다. UI/파일 작업의 gate/close를 확인했고 오디오 엔진에 의상 계산을 넣지 않았다.
- 창/단일 실행: 사용자·Windows 세션 Local mutex는 첫 일반 실행만 모델/UI를 만들게 한다. 두 번째는 등록 native 메시지로 숨겨진 기존 설정창을 복원하고 종료한다. Loaded 재진입은 초기화 guard로 막는다. native broadcast 생성과 배경 변경을 분리해 기존 HWND/geometry/model을 유지한다. 투명 layered 창으로 WGC를 요구하는 tradeoff는 사용자 지시 및 Decision Log에 기록했다.
- 색상: 첫38199786043의 신규 ComboBox 글씨16실패를 실제 화면에서 확인했다. 선택/팝업 항목 DataTemplate에 검정 글씨를 명시했고 전체4크기 재검증했다. 긴 목록 이름은 잘림 표시/툴팁, 긴 한 줄 입력의 내부 caret 가로 이동만 예외로 두어 페이지 스크롤과 구분했다.
- 서명:38200259395의 CurrentUser Root 대기는 보호 저장소 확인 UI로 추정한다. 해당 QA를 취소하고 CI 전용 LocalMachine Root/단계 로그/5분 상한으로 수정했으며 최종 전체 통과했다. 앱에는 인증서 설치 기능이 없고, 개인PC에서 직접 확인·신뢰하는 공개 인증서만 제공한다. 인증기관 신원 검증/SmartScreen 평판과 실제 서명 무결성을 구분했다.
- 문서/자산: 사용자 원본·개인 기본 범위·이전 저장 슬롯을 보존했다. 기존 늘보군12종은 수정하지 않고 로컬 개인 ZIP에만 복사했다. README 사용자 내용 유지. 미해결 기능 결함은 발견하지 않았다.

## Residual Risk

자동 Windows는 Server2025/합성 이미지이며 사용자 Windows11 GPU/개인PNG/실제 마이크·물리Shell과 구분한다. 고정 HWND를 위해 layered 창을 사용해 BitBlt는 보이지 않을 수 있다. WGC를 사용한다. 자체 서명은 인증기관 신원 확인이 아니며 대상PC에서 수동 신뢰 설치해야 한다. 실제 경고 화면/SmartScreen 제거를 보장하지 않는다. CI마다 인증서가 달라 새 패키지 지문을 확인해야 한다.

## Follow-Ups

개인 ZIP을 새 폴더에 풀어 의상 추가→클릭 착용→조정→의상 저장, 기존 창 복원/OBS WGC를 사용한다. 게시자 표시를 위해 동봉 CER/JSON 지문과 수동 신뢰 안내를 따른다. 대상PC 실기는 검증 체크리스트에 추가 기록한다.
