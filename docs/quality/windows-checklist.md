# Windows 실사용 검증 기록

## plan045 늘보군·펠리 직접 샘플과 그림 개선

2026-10-17 앱1.1.0/최종 앱 a8cdcba의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38938790352), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38938790502) 통과. QA 후 단일 에이전트 자체 리뷰/실제 Windows76화면·개인 로컬64정적합성/OBS4캡처를 검토했다.

- 별도 인형을 자동 추가하지 않고 늘보군/펠리의 원래 선택·그림·표정/입3·8단계 위에17의상/14분류/9고유 동작을 적용한다. 새 수채화 그림·8프레임 헬리콥터 모자/6프레임 날개·3헤어·편집/왕복·방송 공유·전환/재시작·원본/저장 의상 보존 검사 통과.
- Core405/405·lint/analyzer/Release 경고0·오류0·WPF2370/실패0·desktop396·일반/서명 각9개·ZIP portable1992/실패0. 이전2090개 중2087개 유지, 새 UX에 따른 별도 인형2검사 대응/카드 검사1개 이름 변경과 새283개를 [자체 리뷰](../reviews/plan-045-character-samples-review.md)에 기록했다. 이전 카드 decoder thread 오류 회귀 유지.
- OBS32.2.2 WGC/chroma/native alpha·동일 HWND 배경 왕복·각7프레임 통과, 기존 BitBlt layered 제한 유지.
- 새 `artifacts/VoxPet-Windows-test-plan045.zip`:54파일/103,671,141bytes·PE x64/CRC/SHA256 확인. 공용41파일은 시험한 CI ZIP, 개인13PNG는 이전 해시와 byte-identical. 이전 ZIP/원본/설정·다른 작업트리 보존, 개인 그림 외부 전송 없음. 상세 증거 `artifacts/test-results/plan045/`.
- 착용 선택은 현재 실행 중 캐릭터별 유지/재실행 시 해제. 개인 그림 Windows 실행·물리 마이크/GPU/DPI·새60분 실기는 별도이며 개인 시각 증거는 로컬 정적 합성이다.

## plan044 최신 main 전체 재시험

2026-10-16 main22215a6/앱1.0.1을 새 작업트리와 새 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38913337799), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38913348630)로 다시 시험했다. Core380/380·lint/analyzer·Release 경고0/오류0·WPF2090/0(이전2090개 정확히 유지)·desktop324·일반/서명 각9사례·ZIP PowerShell5.1 portable1784/0 통과.

- 카드 오류 재현 회귀·단일 사이드바·17의상/14분류/9동작·모자/날개/헤어·편집/내보내기/재시작을 확인했다. 실제 화면75장/원본 샘플20장/OBS4장을 QA 이후 자체 시각 검토했고 추가 필수 수정 없음. 앱/하네스/시험/자산 변경 없음.
- OBS32.2.2 WGC/chroma/native alpha·같은 HWND 배경 왕복·각7프레임 변화 통과. 기존 BitBlt layered 제한 유지.
- 새 `artifacts/VoxPet-Windows-test-plan044.zip`:52파일/95,870,506bytes·PE/CRC/SHA256 확인. 공용39파일은 새 CI 시험본, 개인13PNG는 이전 해시와 byte-identical. 오류 원본/기존 ZIP/개인 자료 보존·외부 전송 없음. [자체 리뷰](../reviews/plan-044-full-retest-review.md)와 `artifacts/test-results/plan044/`에 상세를 보존했다. 물리 마이크/GPU/DPI·개인 그림 Windows 화면·새60분은 별도 실기다.

## plan043 실제 카드 문자열 오류 재현·수정

2026-10-16 앱1.0.1/최종772e757의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38892871936), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38892872151) 통과. QA 이후 단일 에이전트 자체 리뷰/75화면 시각 검토를 수행했다.

- 수정 전 d2ba151에서 사용자 기록과 동일한 InvalidOperationException/80131509/CharacterCard.PrintMembers→ToString을 재현했다. 기존2037개는 통과했지만 새 문자열 계약이 실패해 이전 QA의 누락을 확인했다. Name 전용 출력·유사 이미지 모델·명시적 텍스트 검색/접근성 이름을 수정했다.
- Core380/380·locked restore/format/analyzer/Release 경고0·오류0·WPF2090/0(기존2037개 유지/53개 추가)·desktop324·unsigned/signed 각9개 시작/종료/저장 보존·portable1784/0 통과.
- UI/worker/frozen·가변·null 이미지의 문자열/암시적 바인딩/병렬 이름 읽기, 실제 Home/End·영문/한글 검색·UI Automation·필터/빈 결과·반복 선택/새로고침·손상/삭제 원본 보존·편집/재등록·재시작과 기존17/9 샘플/모자·날개·헤어 회귀 확인. 실제 마이크 factory0. 최종75화면에서 추가 문제 없음.
- OBS32.2.2 WGC/크로마/native alpha·동일 HWND/소스 배경 왕복·각7프레임 변화 통과. BitBlt는 기존 제한이다.
- 새 `artifacts/VoxPet-Windows-test-plan043.zip`:52파일/95,870,529bytes·PE/CRC/SHA256 확인. CI 공용39파일+이전 전달 해시와 일치하는 개인13PNG를 그대로 유지했다. 사용자 오류 원본/개인 자료는 CI로 보내지 않았다. [자체 리뷰](../reviews/plan-043-card-display-hardening-review.md)와 `artifacts/test-results/plan043/`에 재현·전체 판정·화면·해시를 보존했다. 물리 마이크/GPU/DPI·개인 그림 화면·새60분 실기와 구분한다.

## plan042 움직이는 모자·날개와 헤어 샘플

2026-10-16 최종 dad72a1의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38881016955)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38881016833) 통과. 전체 QA 후 단일 에이전트 자체 리뷰와72화면 시각 검토를 수행했다.

- Core380/380·format/analyzer·Release 경고0/오류0·WPF2037/0(기존1981개 유지/56개 추가)·desktop321장·unsigned/signed 각9개 시작/종료·portable1734/0 통과.
- 의상14분류/17종·동작9종을 모두 포함한다. 헬리콥터 모자8프레임/80ms·날개6프레임/140ms는 idle/동작 중 반복하고 단발/장발/폭탄머리는 모자와 공존한다. 착용/충돌/해제·preview/방송 공유·편집/불량 입력 복구/전체 PNG bytes·내보내기/재읽기·이전 샘플 보존/1.1.0 재사용을 검사했다.
- 실제 Windows에서 발견한 편집기 선택값 흰 글씨와 시각 QA에서 발견한 편집 후 메뉴 전환 스크롤 잔류를 수정하고 전체를 재시험했다. 최종 화면에서 검정 선택/팝업·프레임 편집 필드·제목/미리보기 상단 복원을 확인했다. 작은 창은 스크롤한다.
- OBS32.2.2 WGC/크로마/native alpha·같은 HWND 배경 왕복·각7프레임 변화 통과. BitBlt는 기존 layered 제한이다.
- 최종 `artifacts/VoxPet-Windows-test-plan042-final.zip`:52파일/95,863,839bytes·PE/CRC/SHA256 통과. CI 공용39파일과 개인13PNG byte-identical, 이전 파일 보존·개인 자료 전송 없음. 해시와 상세는 [자체 리뷰](../reviews/plan-042-animated-accessories-review.md), 증거는 `artifacts/test-results/plan042/final/`를 따른다. 실제 마이크/사용자 GPU/DPI/개인 그림 Windows 화면·새60분 실기는 별도다.

## plan041 처음부터 전체 재시험

2026-10-16 새 기준 b44685e의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38874585603)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38874637732) 통과. bin/obj 없는 새 작업트리에서 locked restore/format/analyzer/Release/Core를 다시 실행했고 Windows/OBS도 새 runner를 사용했다. 앱 소스는 이전 완료본과 동일하다.

- Core369/369·경고0/오류0·WPF1981/0(이전 검사 집합과 정확히 동일)·실화면305장·unsigned/signed 각9개 일반 시작/정상 종료/파일 보존·Windows ZIP 재압축 해제/PowerShell5.1 동봉 도구1694/0 통과.
- 단일 사이드바·의상14분류/동작9종·착용/충돌/관절/효과/idle·방송창 공유·관리 복사본 재사용·선택값/팝업·눈물 패널 이름 회귀 통과. 새56화면을 직접 시각 검토했다. 가장 큰1280×820 요청은 CI 작업 영역에 맞춘 실제992×688이며 작은 창은 스크롤한다.
- OBS32.2.2 WGC 초록/크로마/투명·같은 HWND 배경 전환·각7프레임 변화 통과. BitBlt의 기존 layered 제한 유지. 실제 마이크/GPU/DPI·개인 그림 화면·새60분 측정은 별도 실기다.
- 새 `artifacts/VoxPet-Windows-test-plan041.zip`:52파일/95,605,003bytes·PE x64/CRC/SHA256 확인. CI 공용39파일과 기존 개인13PNG byte-identical, 이전 ZIP/원본 보존·개인 자료 외부 전송 없음. 상세 해시·결과는 [자체 리뷰](../reviews/plan-041-full-retest-review.md), 증거는 `artifacts/test-results/plan041/`를 따른다.

## plan040 통합 Studio와 전체 의상·동작 샘플

2026-10-15 최종 c8bcf46의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38733607211), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38733607371) 통과. 단일 에이전트가 QA 후 자체 리뷰하고 발견한 화면 대비/설정 이름 문제를 보완 후 재검증했다.

- Core369/369·locked restore/format/analyzer/Release 경고0·오류0·WPF1981/0·실제 desktop305장·unsigned/signed 각9개 일반 시작/정상 종료/설정 보존·공용 ZIP 재압축 해제/PowerShell5.1 동봉 도구1694/0 통과.
- 일반 화면은 새 사이드바6페이지와 공통 미리보기로 통합했다. PNG 설정7항목도 같은 화면에서 제공한다. 의상14분류·동작9종/모든 템플릿의 실제 착용/해제·충돌·관절 연결·효과·idle·별도 방송창 공유·관리 복사본 재사용을 확인했다. 샘플은 공개 절차 도형/공개 제작 틀이며 입력을 자동 시작하지 않는다.
- 변경56화면을 직접 시각 검토했다. 가장 큰1280×820 요청은 CI 작업 영역에 맞춘 실제992×688이며 나머지는960×540/640×480/480×320이다. 작은 창은 스크롤한다. 카드/선택값/팝업 글씨와 눈 깜빡임/눈물 이름 대응 검사를 추가했다.
- OBS32.2.2 WGC/초록+크로마/native alpha/같은 HWND 배경 왕복·각7프레임 변화 통과. 기존 BitBlt layered 제한 유지.
- 로컬 `artifacts/VoxPet-Windows-test-plan040-final.zip`:52파일/95,605,036bytes·PE x64/CRC 통과. CI 공용39파일은 줄바꿈까지 동일하며 개인13PNG만 이전 ZIP에서 그대로 추가했다. 개인 자료 외부 전송/원본 덮어쓰기 없음. SHA256과 상세는 [자체 리뷰](../reviews/plan-040-unified-test-studio-review.md), 증거는 `artifacts/test-results/plan040/last/`, 사용법은 [Studio 샘플](../guides/studio-samples.md)을 따른다.

실제 물리 마이크/권한/USB·사용자 GPU/OBS·혼합 DPI·60분 실기와 개인 그림의 Windows 시각 검증은 미실행이다. 자동 도구는 이전 회귀용 화면과 합성 입력을 사용하며 직접 조작은 START.cmd의 통합 화면에서 수행한다.

## plan039 실제 앱 오류 복구와 Windows 시험 파일

2026-10-15 코드bdde9ae의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38714843197)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38714843135) 통과. 단일 에이전트가 QA 후 자체 리뷰했으며 리뷰에서 발견한 Canvas 범위도 보완 후 전체 재검증했다.

- locked restore/format/analyzer/Release 경고0·오류0/Core366/366. 실제 WPF1768고유검사·실패0, 신규 safety24종 전부 실행, desktop250화면·unsigned/signed 각9개 일반 시작/정상 종료/파일 보존 통과.
- 첫 frame 실패의 scene/document/저장 선택 보존, 보관함 import/thumbnail 실패 격리, 지연 변환 overflow의 마지막 frame/입력 해제/재선택 복구, 삭제된 preview 참조, 미적용 입력의 삭제/드래그 보호, 앵커 없는 캐릭터 편집,10000입력의128상한을 검사했다. Canvas1~16384 경계·범위 밖11개 Core 회귀와 WPF 문서 보존도 확인했다.
- 실제 공용 ZIP을 다시 풀고 동봉 PowerShell5.1 시험 도구1536/0 통과. 일반 사용자용 도구는 CI 전용 desktop232검사를 활성화하지 않는다. 합성 키·좌클릭이 있으므로 전용 테스트 Windows/VM에서 실행하고 작업 중인 데스크톱에서는 수동 체크리스트를 따른다.
- OBS32.2.2 WGC/초록+크로마/native alpha/같은 HWND 배경 전환과 각7프레임 변화 통과. BitBlt의 기존 layered 제한은 유지한다.
- 로컬 `artifacts/VoxPet-Windows-test-plan039-final.zip`은50파일/95,456,545bytes·CRC/SHA256 통과. 최종 CI unsigned EXE와 동일하며 이전 늘보군·펠리13PNG byte-identical. 개인 PNG 외부 전송/기존 원본 덮어쓰기 없음. 증거는 `artifacts/test-results/plan039/final/`, 상세는 [리뷰](../reviews/plan-039-runtime-hardening-review.md)와 [직접 시험 안내](windows-test-package.md)를 따른다.

사용자 Windows11 물리 마이크/권한/USB·GPU/OBS·혼합 DPI·60분 실기는 별도다. 개인 PNG의 Windows 시각 검증은 이번 합성 CI에 포함하지 않았다. unsigned 배포의 실행 경고 제거를 주장하지 않는다.

## plan013 사용자 기능 개선과 전체 회귀

2026-10-08, 코드7297d02의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37743242592)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37743242633) 통과. macOS/Windows Core110/110, locked restore/format/analyzer/Release build(경고0/오류0)/자체 포함 publish를 통과했다. 실제 WPF smoke 실패0/명명 검사239개, 4창크기×49컨트롤/3startup bounds/작은 창 스크롤과 첫 화면 Start/Stop을 확인했다. CI annotation0이며 새 전역 키/측정 버튼과480×320 화면 PNG를 직접 확인했다.

- 입 음소거: Ctrl+Shift+M 실제 RegisterHotKey/WM_HOTKEY + 합성 키 입력으로 등록·충돌·설정창 최소화 중 토글/해제·재등록을 확인. 로컬 키 대체/반복 무시/전역 키 중복 처리 방지, RAW/blink 유지도 통과했다.
- 주변 소음: 가짜20ms 숫자 입력으로 실제 UI 타이머의3초 측정/명시 적용/Gate 외 설정 보존/측정 취소/대기 추천 취소/Stop/프리셋/무신호/장치 오류/측정 도중 종료를 확인했다. 실제 무음 snapshot과 무신호를 구분하며 중복 표본·비유한 값·표본 수/기간·과도한 소음·일시적 이상치·메모리 상한은 Core 테스트로 확인했다.
- 권한 설정: 버튼 배치와 명령 이벤트 전달 통과. 자동 smoke에서 실제 Windows Settings를 열거나 권한을 변경하지 않았다.
- 기존 시트/표정 저장·손상 복구·전환·눈물·방송창 수명 및 OBS32.2.2 WGC green+Chroma Key/native alpha·각7프레임 변화 회귀 통과. BitBlt 유효 프레임 없음은 기존 runner 제한이다. 새60분 장시간 시험은 실행하지 않았다.
- 첫 Windows37742692699는480×320 초기 Start/Stop이 새 권한 버튼에 밀리는1건을 발견했다. 버튼을 Start/Stop 뒤로 이동했고 최종 실행에서 모든 검사 통과했다. 시험을 제외하거나 합격 기준을 낮추지 않았다.
- 증거: root artifacts/qa/plan013/windows-final/, obs-final/, summary.json. Windows 자체 포함 배포본 artifacts/VoxPet-win-x64-plan013.zip은9파일/ZIP CRC를 확인했고 개인 PNG를 포함하지 않는다. 이전 늘보군 자료를 보존했다.

| 추가 실기 시나리오 | 합격 기준 | 결과 |
|---|---|---|
| 권한 버튼과 거부/허용 복구 | 올바른 Windows 마이크 설정 페이지, 자동 권한/Start 없음, 허용 후 새로고침/Start 복구 | 미실행 |
| 실제 주변 소음3초 측정 | 무음/팬/키보드 조건의 추천 후 작은 목소리와 일반 발화 확인, 말하면서 측정 시 재시도, Stop 캡처 해제 | 미실행 |
| 게임/OBS 중 물리 Ctrl+Shift+M | 포커스/최소화/충돌/키 반복에서 입 토글1회, RAW/blink 유지, Stop으로 마이크 해제 | 미실행 |

물리 마이크/권한/제거·키보드·DPI, 사용자 GPU/OBS, 실제 입력 장시간은 아래 기존 실기 매트릭스와 함께 미실행으로 남는다. 조사와 수식은 [사용자 기능 목록](../product/user-feature-research.md), QA 이후 [자체 리뷰](../reviews/plan-013-user-feature-improvements-review.md)를 따른다.

## plan012 전체 재점검과 늘보군 화면 검증

2026-10-07, 최종 코드8fdfd36의 [기본 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37637240867), [늘보군 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37637826804), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37637240816) 통과. macOS/Windows Core97/97, locked restore/format/analyzer/Release build/publish, WPF smoke failures0을 확인했다. 늘보군 실행은 명명 검사210개,4창크기×45컨트롤/3startup bounds/작은 창 스크롤을 통과했다. workflow/의존성 deprecation/빌드 경고·오류0이며 C#35파일의XML doc100블록, XAML4개/Python4개 구문도 통과했다.

늘보군3×2 시트의 입3×눈2 상태, 합성 반응, 슬롯 관리 복사본 저장/재시작/원본 제거/손상 복구, 전환 시작·완료, 무음 blink·눈물 및 두 창 공유를 확인했다. 실제 WPF 데모 미리보기(Voice Level38%), 눈물 미리보기, 초록/투명 방송창과 작은 창 PNG를 직접 확인했다. 눈물 시작점을 눈 아래로 조정했으며 원본 PNG의 SHA256은 기존 manifest와 동일하다. UI의 실제 모션 시간축은 자동 합성 입력으로 시험했고 물리 마이크·키보드 실기와 구분한다.

첫 개인 실행37636070658은 추가 개인 시험 때문에 기존60초 상한에 걸려 마지막 작은 창 검사 중 종료됐다. 기본60초를 유지하고 개인 시험만90초로 조정했으며 시험 항목을 줄이지 않았다. 두 번째37637307666은 단일 자산 URL의 jwt:expired로 입력 단계에서 중단되어 새 만료 URL로 동일 코드37637826804를 재실행했다. 최종 실행은 전체 통과했다.

OBS32.2.2의 기본 CC0 캐릭터 회귀는 WGC green+Chroma Key와 native alpha 및 각7프레임 변화 통과다. BitBlt 유효 프레임 없음은 기존 runner 제한이다. 로컬 증거는 root `artifacts/qa/plan012/summary.json`, `windows-neulbo/`, 기본 Windows/OBS 폴더에 보관한다.

사용자의 명시적 전송 승인과 캐릭터 삭제 금지 요청에 따라 **늘보군 원본·private draft asset·개인 화면 증거·직접 시험 패키지를 보존**했다. 임시 URL secret만 제거했다. 개인 패키지는 root `artifacts/VoxPet-win-x64-neulbo-plan012.zip` 및 `artifacts/VoxPet-neulbo-plan012/`이다. Windows에서 압축을 모두 풀고 VoxPet.exe → PNG 시트 불러오기 → 개인 캐릭터/늘보군.png → 마이크 없이 데모 순서로 사용한다. 다음 실행에도 유지하려면 F1 표정 슬롯에 저장한다. 기본 공개 배포는 CC0 고양이를 유지한다.

실제 물리 마이크/권한/장치 제거/키보드/DPI와 사용자 OBS/GPU/실제 입력 장시간은 별도 미실행 항목이다. 이번 자동 화면 시험을 그 실기 통과로 해석하지 않는다.

## 환경과 현재 증거

2026-10-05, 구현 호스트 macOS arm64, SDK 10.0.401.
Core 자동 테스트와 WPF 교차 컴파일은 실행했다. Windows GitHub Actions에서도 70개 테스트와 WPF UI smoke를 통과했다.
초기 검증 commit ad1d266, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37265531490).
방송창 수명 회귀: ddbaee7, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37266740645).
OBS 합성 캡처: e41194f, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37268938486).
최종 green/transparent 캡처: 86374ad, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37276105483). WGC의 투명 배경 alpha65.1%/캐릭터 유지/7프레임 변화 확인.
OBS 32.2.2/obs-websocket 5.7.4, Windows Server 2025 build26100, Microsoft Basic Render Driver/D3D11.
장시간 합성 데모: d35f707, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37269646209). 10분 warmup 후 실제60.17분 측정, RSS 증가1.58MiB/핸들 증가0/68회 프레임 변화 검사 통과.
실제 Windows 마이크와 사용자 GPU/OBS 조합은 아직 통과로 기록하지 않는다.

| 항목 | 현재 상태 | 확인 방법/합격 기준 |
|---|---|---|
| Core 수치·수명·캐릭터 자동 테스트 | 71/71 통과 (macOS/Windows, 4ad3180) | 합성 입력과 fake input; 모든 테스트 통과 |
| WPF Release 교차 컴파일 | 통과, 경고/오류 0 | `dotnet build VoxPet.sln -c Release` |
| win-x64 자체 포함 배포 | 통과 | 잠금 복원 후 publish, EXE/라이선스 확인 |
| Windows WPF smoke | 통과 | UI 바인딩 오류 0, 이미지/별도 창/데모 반응/blink/입력 범위/reset/Stop/일반 창 종료 통과 |
| 방송창 최소화/복구/재열기/종료 | Windows CI 통과 | native 창 가시성/최소화 상태와 shared model, 메인 종료 후 창 닫힘 확인 |
| 실제 마이크 | 미실행 | 아래 실기 매트릭스 수행 |
| OBS 합성 데모 | WGC 통과, BitBlt 유효 프레임 없음 | 설정창 최소화 중 Window Capture, 7개 서로 다른 프레임, Chroma Key 후 green0%/alpha66.0%/캐릭터 유지; 실제 PNG 확인 |
| OBS 합성 장시간 UI | 통과 (d35f707) | 60.17분 RSS +1.58MiB/핸들 +0, 68회 변화 검사 및 최종 유효 프레임. 실제 마이크 장시간 시험과 구분 |
| OBS 사용자 환경 | 미실행 | 아래 OBS 매트릭스 수행 |

## 최종 통합 자동 QA와 실행 패키지

plan004/005/006 통합4ad3180: [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37278367856) 및 [OBS CI](https://github.com/taejun9/VoxPet/actions/runs/37278367918) 통과.
Core71/71, smoke 실패0, 4창크기×15컨트롤/3startup bounds/휠과 native 방송창 수명·resize를 확인했다.
OBS32.2.2 WGC green+Chroma Key 및 native alpha65.1%/7프레임 변화 통과. BitBlt는 유효 프레임 없음.
오디오 Stop 중 native Ended 오류의 누락을 재현·수정했고, 무음/자원정리/새 Start 복구 자동 회귀를 추가했다.
Windows runner의 배포본은 로컬 `artifacts/VoxPet-win-x64-plan006.zip`, 증거와 SHA256은 `artifacts/qa/plan006/integrated/summary.json`에 보존한다.
이 패키지의 실제 마이크/실제 DPI/실제 입력 장시간 검증은 아래 미실행 항목으로 남는다.

## 마이크/창 실기 매트릭스

plan007의312916f에서 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37294330257)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37294330480)를 재실행했다.
새 필수 format/analyzer 검사 통과, Core71/71, Release 경고/오류0, publish 및 실제 WPF smoke failures0을 확인했다.
4창크기×15컨트롤/3startup bounds/작은창 wheel JSON과 실제 WPF PNG를 직접 확인했다.
OBS WGC green+Chroma Key green0%/alpha66.04%, native alpha65.08% 및 각7프레임 변화와 캡처PNG를 확인했다.
BitBlt 유효 프레임 없음과 아래 실제 마이크 미실행 상태는 유지한다. 증거: root artifacts/qa/plan007/summary.json.

시험자는 OS edition/build, 앱 commit, 장치 종류/채널/샘플 형식, 화면 DPI, 결과/실패 증상을 기록한다.
녹음·개인 장치 ID는 기록하지 않는다.

| 시나리오 | 합격 기준 | 결과 |
|---|---|---|
| 마이크 없는 PC 시작 | 앱 유지, 목록 없음/연결 안내, Start 비활성 | 미실행 |
| Windows 데스크톱 마이크 권한 거부 | 오류 안내, 앱 유지, 권한 허용 후 Start 복구 | 미실행 |
| USB/내장 마이크 Start→발화→Stop | 입 반응/값 표시, 중지 후 마이크 점유 해제와 레벨 0 | 미실행 |
| 100회 Start/Stop | 중복 캡처/멈춤/핸들 증가 없음 | 미실행 |
| 시작/종료 중 창 닫기 | UI 멈춤 없음, 프로세스/마이크 점유 종료 | 미실행 |
| USB 장치 캡처 중 제거 | 오류 안내와 레벨 0, 재연결/새로고침/Start 복구 | 미실행 |
| 장치 Stop→교체→Start | 새 장치만 캡처, 이전 세션 값 없음 | 미실행 |
| 작은 소음/말/침묵 | gate 조정, attack/release 반응, 침묵 후 입 닫힘 | 미실행 |
| 100/150/200% DPI, 최소 창 크기 | 잘림 없이 조정/Start/Stop 접근, PNG 정렬 | 미실행 |
| 설정창 최소화 중 방송창/복구/재열기 | 방송창 가시성 유지, 열기 명령 복구, 재열기와 메인 종료 정리 | Windows CI 통과 |
| 실제 입력 중 창 최소화와 UI 조작 | 입력 callback 대기열 증가/화면 정지 없음 | 미실행 |
| 설정 저장/손상/읽기 전용 | 정상 보존/기본값 복구/앱 종료 가능 | 미실행 |
| 60분 캡처 | 메모리/핸들 누적 증가 없음, 마이크 오류 없이 응답 | 미실행 |

## OBS 실기 매트릭스

OS/OBS version, GPU/driver, 캡처 방식(자동/Windows Graphics Capture/BitBlt), DPI, 창 가림/최소화, 녹화 시간과 결과를 기록한다.
실제 방송 영상·음성은 저장소에 넣지 않는다.

| 조합 | 합격 기준 | 결과 |
|---|---|---|
| 초록 배경 + Window Capture + Chroma Key | 배경 제거, 입/눈/몸 반응, UI가 송출되지 않음 | 위 CI 환경에서 WGC 합성 데모 통과. 실제 마이크/사용자 GPU 조합은 미실행 |
| 투명 배경 + Window Capture | alpha 유지 여부를 측정, 실패 시 초록 배경 경로 사용 | 위 CI의 WGC에서 alpha65.1%/캐릭터 유지/7프레임 변화 통과. 사용자 GPU 조합은 미실행 |
| 방송창 가림/항상 위 해제/최소화 | 캡처 지속 조건과 제한 기록 | 미실행 |
| 크기 조절/화면 DPI 변경 | OBS 화면에서 anchor/정렬 유지 | 미실행 |
| 60분 캡처 | 프레임 정지/핸들·메모리 누적 증가 없음 | 위 CI의 합성 데모60.17분/68회 변화 검사 통과. 실제 마이크/사용자 GPU 조합은 미실행 |

## 작은 창 자동 검사 범위

plan005의 LayoutQa는 실제 WPF 창을 1000×730, 960×540, 640×480, 480×320 논리 크기로 조절한다.
3dcded2, [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37272421104)에서 Core70/기존 smoke와 함께 통과했다.
plan004 통합 후87e14a6의 [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37277539708) 및 [OBS CI](https://github.com/taejun9/VoxPet/actions/runs/37277539743)에서도 native resize/배치/green·transparent 캡처가 통과했다.
마이크 선택/Start/Stop/데모/6개 슬라이더/기본값/방송창 열기/방송 옵션 등 15개 컨트롤이 scroll viewport 안에 들어오는지 확인한다.
작은 창의 휠 routed event, 초기 Start/Stop 가시성, 가상 작업 영역을 주입한 실제 창의 초기 bounds/종료도 검사한다.
100/150/200% PNG는 raster 출력 검사다. 실제 모니터 배율 변경/다중 모니터 이동/사용자 GPU 결과로 기록하지 않는다.

## 성능 측정 범위

UI 60Hz는 타이머 목표이며 성능 측정 결과가 아니다.
실기에서 발화→표시 지연, CPU, 메모리, 핸들을 측정하고 목표 기준을 먼저 결정한다.
지연은 녹음 파일 대신 테스트 신호와 화면 시각을 사용해 측정한다.

## plan008 캐릭터와 편의 기능 검증

2026-10-06 코드 f5f1757의 [기본 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37338899300),
[늘보군 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37338981751),
[OBS 회귀 QA](https://github.com/taejun9/VoxPet/actions/runs/37338899198)가 모두 통과했다.
Core84/84, Release 경고/오류0, format/analyzer, 자체 포함 publish, 실제 WPF smoke failures0을 확인했다.
로컬 증거는 `artifacts/qa/plan008/summary.json`과 `final-personal/`에 보존했다. 개인 이미지가 있는 원격 QA artifact와 임시 입력/secret은 수집 후 삭제했다.

- PNG 시트: 6상태 매핑, 작업 스레드 로드와 Freeze, 자동 중앙/바닥 정렬, 파일 점유 해제, 빈/불투명/잘못된/과대/없는 파일 복구 및 기존 캐릭터 보존 통과.
- 편의 기능: 조용한 목소리/빠른 반응/일반 대화 프리셋, 음소거 중 입 0 및 입력/blink 유지, 해제 후 반응, Stop reset, 마이크 상태 표시 보존 통과.
- 레이아웃: 4창크기×21컨트롤 모두 접근 가능, 3startup bounds, 작은 창 휠, 1/1.5/2배 합성 렌더, 넓은 창 미리보기 전체 가시성 통과.
- 늘보군: 사용자 참고 이미지에서 만든 개인 RGBA 시트 적용, 여섯 상태 PNG와 실제 합성 반응/미리보기 PNG 확인. 마이크를 캡처하지 않았다.
- OBS32.2.2: 기본 캐릭터 WGC의 7프레임 변화, green Chroma Key 및 native alpha65.08% 유지 통과. BitBlt는 기존처럼 유효한 캐릭터 프레임 없음.

배포본과 개인 시트를 묶은 로컬 파일은 `artifacts/VoxPet-win-x64-neulbo-plan008.zip`이다.
기본 공개 배포는 CC0 고양이를 유지하며 늘보군은 이용 권리 미확인 개인 시험 팩이다.
실제 마이크/권한/제거, 사용자 GPU/OBS, 물리 DPI 변경, 실제 입력 장시간 시험은 위 미실행 상태를 유지한다.

## plan009 문서·주석 회귀 검증

2026-10-06 checkpoint246a61e의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37413935262) 및
[OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37413935334)가 통과했다.
README·사용 설명서·배포 안내와 한국어 코드 주석만 보강했으며 실행 로직·자산·의존성·workflow는 변경하지 않았다.

- Windows: locked restore/format/analyzer/Release/publish, Core84/84, 경고·오류0, WPF smoke failures0.
- 배치: 4창크기×21컨트롤, 3startup bounds, 작은 창 스크롤 통과.
- OBS WGC: green+Chroma Key와 native alpha65.08%, 각7프레임 변화 통과. BitBlt 유효 프레임 없음 유지.
- 증거: root artifacts/qa/plan009/windows/ 및 obs/. 로컬 주석 제외 실행 내용 동등성 및 XML summary 검사 통과.
- 실제 마이크·사용자 GPU/OBS·물리 DPI·실제 입력 장시간 시험의 기존 미실행 상태는 유지한다.


## plan011 표정·단축키·모션 검증

2026-10-07 c18a8c2의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37576998278) 및
[OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37576998263) 통과.
Core97/97, format/analyzer, Release 경고/오류0, 자체 포함 publish와 실제 WPF smoke 통과.

- 표정: 내장6종×6상태, 12슬롯 로컬 저장/재시작/원본 PNG 제거 후 관리 복사본 적용, 손상 PNG의 기존 표시 보존과 내장 복구 확인.
- 전환/모션: 240ms 중간·완료·중도 전환 snapshot, 연속 요청의 최신 우선, 마이크 없이 blink/tears 이동, blink 끄기, 눈물 위치 조정 확인. 미리보기/방송창 눈물 PNG를 직접 확인했다.
- 단축키: 실제 RegisterHotKey/WM_HOTKEY + 합성 키 입력으로 F1~F11 등록, 충돌 안내, 메인 최소화 중 F3 전환, F12 앱 내부, 잘못된 modifier 무시, 해제 후 재등록 확인. 물리 키보드 시험과 구분한다.
- 배치: 4창크기, 3startup bounds, 작은 창 스크롤, 40자 슬롯 이름/이름 TextBox 포함. 1/1.5/2배 PNG는 합성 렌더이며 물리 DPI 시험이 아니다.
- OBS32.2.2 WGC green+Chroma Key 및 native alpha, 각7프레임 변화 통과. BitBlt는 기존처럼 유효 프레임 없음.
- 첫 Windows QA의 저장 실패 테스트 예외 종류 차이는 수정 후 재검증했다. 앱 저장 오류 처리의 결함은 없었다.
- 증거: root artifacts/qa/plan011/windows/ 및 final-obs/, 배포 ZIP artifacts/VoxPet-win-x64-plan011.zip.

물리 키보드/다른 프로그램과의 실사용 충돌, 실제 마이크/권한/제거, 사용자 GPU/OBS, 물리 DPI, 실제 입력 장시간은 기존 미실행 조건으로 남는다.


## plan014 셀렉트 가독성·탭·개인 기본·방송창 크기

2026-10-08 a81bc4e의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37768105239)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37768105246) 통과.

- locked restore/format/analyzer/Release/publish, Core110/110, 경고·오류0. Windows smoke 실패0, 고유 명명 검사385개.
- 4창크기×5상단 탭과 모든 하위 탭에서 조작과 머리글 가시성, 스크롤 없음, 선택값/펼친 목록 검정 글씨, 편집값 유지 확인. 탭별 실제 WPF PNG를 직접 확인했다. 작은 창에서 글씨 크기가 줄어드는 점은 유지하며 하위 탭으로 과도한 축소를 줄였다.
- 개인 기본: 합성 PNG의 기본 불러오기/표정 유지/복원/손상·누락 복구 검증. 개인 실행 패키지에만 기존 늘보군 PNG를 Characters/늘보군.png로 동봉했다. 원본 SHA256 일치와 ZIP CRC 통과. 실제 늘보군 시작 화면은 이번 CI에 전송하지 않았으며 원본 캐릭터의 기존 plan012 Windows 검증과 구분한다.
- 방송창: 너비·높이 제어, native 변경의 모델 반영, 유한한200~1200 DIP 범위, 재열기 크기 유지, 기존 우하단 resize hit/독립 수명 회귀 확인. OBS에 grip 픽셀이 찍히지 않도록 기존 투명 grip은 유지했다.
- OBS32.2.2: WGC green/Chroma Key/native alpha65.08%, 각각7프레임 변화 통과. 기존 BitBlt 유효 프레임 없음은 유지.
- 첫 Windows 실행의24개 선택 텍스트 실패는 명시적 ItemTemplate의 검정 TextBlock으로 수정하고 재검증했다.
- 증거: artifacts/qa/plan014/windows/ 및 obs/. 개인 패키지: artifacts/VoxPet-win-x64-neulbo-plan014.zip.

실제 마이크·물리 DPI/입력·사용자 GPU/OBS 실기는 기존 미실행 상태를 유지한다.


## plan015 상세 입8단계·늘보군 실제 표정 가족

2026-10-08 9e89a07의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37776428922), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37776428945) 통과.

- Core132/132, locked restore/format/analyzer/Release/publish 경고·오류0, Windows smoke 실패0/고유 명명 검사415개.
- 합성8열PNG의16frozen상태/행열과 음량 프레임 선택, 기본6표정 가족/8열 인코딩/슬롯 저장 재시작/손상 표정의 평상 복구/기본 누락 시 기존 표시 보존. 기존3열·창 수명·입 음소거·탭·hotkey 회귀 통과.
- Core는8단계 정확한 경계/비유한값/실제 dBFS sweep의8프레임 방문/작은 목소리/Gate와3열·8열 메모리 크기 제한을 검사했다.
- 개인 늘보군은 내장 imagegen으로6실제 얼굴(평상/기쁨/슬픔/화남/놀람/졸림)×입8×눈2=96상태를 제작했다. 얼굴·가방·몸의 정체성, 깜빡임, 표정 차이를 직접 확인했다. 생성기의 배치/화남 입 반복/투명 경계는 도구로 수정하고 프레임은 독립 캐릭터로 추출·패딩·순서 정렬했다. 얼굴 픽셀을 수작업으로 그리지 않았다.
- 로컬 시트는352px셀/2816×704 RGBA, 파일당16MB 이하,96개 고유 프레임과 표정별8개 입 면적 순서를 검사했다. 새 개인 ZIP18파일/CRC/시트 해시/기존 원본 SHA256 일치를 확인했다. 원본과 이전 ZIP은 보존했다.
- 개인 PNG는 Git/공개 CI/공용 배포에 포함하지 않았다. 이번 개인 PNG를 Windows에서 실행한 화면은 수집하지 않았으며 합성 fixture와 로컬 자산 검사로 구분한다.
- OBS32.2.2 WGC green/Chroma Key/native alpha 유지, 각각7프레임 변화 통과. BitBlt의 기존 유효 프레임 없음 유지.
- 증거: artifacts/qa/plan015/windows/ 및 obs/, 개인 자산 artifacts/characters/neulbo-plan015/, 실행 ZIP artifacts/VoxPet-win-x64-neulbo-plan015.zip.

실제 마이크/권한/장치 제거·물리 DPI/키보드·사용자 OBS/GPU의 기존 미실행 상태는 유지한다.


## plan016 방송창 메뉴·표정 순서·개인12종 테스트

2026-10-09 코드6caff4b의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37889406934), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37889406892) 통과.

- Core140/140·locked restore/format/analyzer/Release/publish 경고·오류0. WPF690고유검사·실패0.
- 방송창 팝업 메뉴의 검정 글씨/실제 PNG, 순서 저장·재시작·경계·F키 위치 의미·초안/슬롯 JSON 유지·메모리 PNG 저장ID 검증. 테스트 탭12버튼/전체·취소·복원·키/종료와 기존 데모 보존/마이크 없는 숫자 데모 수명 확인.
- 합성12종×16frozen상태의192매핑, 기존3열·저장 슬롯·입 음소거·창 크기/수명·hotkey 회귀.4창크기 모든 탭에서 컨트롤/머리글 가시성과 스크롤 없음 통과. 실제 메뉴/편집/테스트 PNG 확인.
- 개인12얼굴: 기존6PNG 보존 및 새6종 imagegen 편집.352px셀8×2 RGBA12파일/192고유프레임·투명 여백·8입 면적순서·원본/기존6해시 일치. 개인 ZIP24파일·CRC 통과. 개인 PNG는 공개 Git/CI에 보내지 않았다. 실제 새 개인PNG의 Windows 화면은 이번 합성 fixture와 구분한다.
- OBS32.2.2 WGC green+Chroma Key/native alpha·각7프레임 변화 통과. BitBlt의 기존 유효 프레임 없음 유지.
- 첫 Windows 실행의 오래된 F12 이름 assertion1건은 실제 저장 이름으로 수정해 재검증했다.
- 증거 artifacts/qa/plan016/windows/ 및 obs/, 개인 자산 artifacts/characters/neulbo-plan016/, 개인 ZIP artifacts/VoxPet-win-x64-neulbo-plan016.zip.

실제 마이크/권한/장치 제거·물리 DPI/키보드·사용자 OBS/GPU 실기는 기존 미실행 상태를 유지한다.

## plan018 조용한 시작 종료 재현·OBS 캡처 호환·트레이

사용자는 Windows11/OBS32.2.2/창 캡처, plan016 실행 직후 오류 문구 없이 종료를 보고했다. 기존 persistSettings=false smoke는 일반 저장 설정/기본 가족 시작을 시험하지 못했다.

- 코드 수정 전 [재현 실행](https://github.com/taejun9/VoxPet/actions/runs/37935023184): 같은352px셀12가족을 동봉하고 일반 인수 없는 EXE가 종료 코드-532462766으로 종료했다. stderr의 NullReferenceException은 이동 버튼 CanExecute → InitializeAsync/SetBusy → MainWindow.Loaded 경로였다. WPF 초기화 중 Selected=null을 허용한 회귀다.
- 코드4403f8d [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37937258075): Core140/140·Release 경고/오류0·format/analyzer/publish, WPF725고유검사·실패0.
- 일반 프로세스5사례(fresh12/reversed12/이전3열 저장PNG/손상 설정·순서·슬롯/public6)를 각10초 유지하고 초기화 완료/가시성/선택/편집 가능/정상 종료와 슬롯·순서 파일 불변을 확인했다. synthetic12가족/임시 QA 데이터이며 개인 이미지 전송 없음.
- 실제 WPF Selector 선택 해제/순서 변경, 트레이 hide/메뉴 복원/기존 최소화 복구/방송창·데모 유지/숨겨진 main 종료 및 자원 정리 통과. Shell 물리 클릭과 구분한다.
- 초록 배경 방송창은 불투명/non-layered로 변경, 투명 모드는 별도 native 창으로 재생성해 geometry/model을 유지한다. [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37937258114)에서 이전 실패하던 BitBlt와 WGC 모두 green/Chroma Key/각7프레임 변화 통과, transparent WGC native alpha65.08% 유지. main이 트레이에 숨겨진 상태의 실제 OBS32.2.2 캡처다. 캡처 PNG와 트레이/OBS 버튼이 있는 WPF 화면을 직접 확인했다.
- 마지막 오류 로컬 로그는 종류/코드/앱 메서드/시간·버전만 기록하며 예외 메시지·개인 경로·음성 제외를 검증했다. 사용자의 보안 정책/인증서 신뢰를 변경하지 않았다.
- 증거 artifacts/qa/plan018/repro/, windows/, obs/, summary.json. 개인 ZIP artifacts/VoxPet-win-x64-neulbo-plan018.zip은 기존12PNG/원본 해시를 유지하며 기존ZIP을 보존한다.

실제 사용자 Windows11 GPU/OBS 설정/개인PNG 화면/물리Shell·마이크는 별도 환경이다. 이번 일반 시작 재현의 원인은 PNG 그림 내용과 무관한 Selector null 처리임을 확인했다. 게시자 회사 정보는 김태중이며 기본 EXE는 NotSigned이고 개인PC 서명 안내를 동봉한다.


## plan019 의상·단일 실행·고정 HWND·개인 게시자

2026-10-12 코드73c4b34의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38200870611), 앱 src 동일41692f8의 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38200259371) 통과.

- Core151/151·Release 경고0/오류0·locked restore/format/analyzer/publish. WPF844고유검사·실패0.8분류 관리 복사/추가·착용·명시 저장/미세XY·균일 크기·레이어/재시작·손상 복구·원본 보존. 한벌과 상하의 배타·겉옷/복수 얼굴·귀걸이 공존.6탭×4창크기 실제 항목·60자 이름/검정 선택·팝업 글씨/페이지 스크롤 없음.
- 일반 unsigned와 signed EXE 각각5사례×10초/정상 종료/슬롯·순서 보존, 추가 실행0종료/기존 tray HWND 복원. 트레이/창/모션 수명·native resize 회귀 유지.
- OBS32.2.2/Server2025 build26100/Basic Render Driver: WGC 초록/크로마·투명 native alpha65.08%/7프레임 변화. 같은 source/같은 HWND에서 배경 왕복 통과. 고정 HWND의 always-layered 방식에서는 BitBlt 유효 프레임 없음. WGC를 명시한다.
- 실제 서명자 김태중, 신뢰 전 UnknownError/CI 임시 LocalMachine Root 신뢰 후 Valid/변조 HashMismatch, 서명된 단일 EXE 정상 실행. 해당 Root/개인키는 finally 정리하고 공개 CER만 제공. EXE 아이콘7해상도 payload 일치. 대상 개인PC의 신뢰 등록은 수동이며 인증기관 신원 확인/SmartScreen 평판과 구분한다.
- 첫 신규 ComboBox 글씨16실패를 수정/재검증했다. 중간 WPF844 통과 실행은 CurrentUser Root의 확인 UI 대기 추정으로 취소하고 CI LocalMachine Root/단계 로그/상한으로 수정해 전체 통과했다.
- 개인ZIP27파일/93,271,126bytes/CRC·이전12PNG/원본 해시 일치. 기존ZIP 보존. 개인PNG 공개 전송 없음. 증거 artifacts/qa/plan019/, 패키지 artifacts/VoxPet-win-x64-neulbo-plan019.zip. QA 이후 같은 에이전트 자체 리뷰.

실제 사용자 Windows11 GPU/마이크·물리Shell/개인PNG 화면·인증서 수동 설치와 실제 실행 경고는 별도 실기다. 자동 검사 합격을 사용자PC 경고가 제거된 결과로 표현하지 않는다.

## plan021 다섯 캐릭터 선택

2026-10-12 코드bc20ca0의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38232965708), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38232965718) 통과. QA 이후 단일 에이전트 자체 리뷰로 가독성·종료 순서 경합을 보완했다.

- Core160/160·Release 경고0/오류0·locked restore/format/analyzer/publish, WPF910고유검사·실패0. 다섯 실제 선택 항목/두 창 공유/frozen 입·눈/연속 요청/전체 테스트 취소/누락·손상 복구/슬롯·의상 보존과 지연 마이크 Stop 중 새 캐릭터 적용 차단 확인.
- 일반/김태중 자체 서명 EXE 각각8사례×10초/정상 종료/슬롯·순서 불변. 펠리 선택 복원·종료 저장, 누락·손상 파일은 늘보군으로 복구하고 오디오/방송창 설정 보존. 공개 CI는 임시 합성 PNG만 사용했다.
- 6탭×4창크기와 캐릭터3세부페이지의 선택/팝업 검정 글씨·조작 가시성·페이지 스크롤 없음. 최초24글씨 검사 실패를 명시적 ItemTemplate으로 수정했다.
- OBS WGC 초록/크로마·투명 alpha·동일 HWND/같은 소스 배경 왕복 회귀 통과. BitBlt는 기존 always-layered 호환 제한을 유지한다.
- 신규4개 image_gen RGBA1536×1024/3입×2눈·24셀의 투명 여백/고유 상태/경계 검사, 측면 펠리와 원본 디자인 시각 확인. 기존 늘보군12PNG byte-identical. 개인PNG는 Git/CI에 보내지 않았다.
- 개인 ZIP32파일/100469367bytes/CRC/SHA256/EXE·공개 인증서 해시 대조 통과. 기존ZIP/Downloads 원본 보존. 증거 artifacts/qa/plan021/, 캐릭터 artifacts/characters/plan021/, 배포 artifacts/VoxPet-win-x64-characters-plan021.zip.

신규4종은 평상3입×2눈이며 별도12표정 얼굴은 제공하지 않는다. 사용자 실제 Windows11 개인 그림·물리 마이크/GPU/OBS·의상과 눈물 배치는 대상PC 실기로 확인한다. 자동 합성 QA 및 자체 서명을 실제 사용자PC의 마이크/실행 경고 통과로 표현하지 않는다.

## plan022 마지막 설정 유지

2026-10-12 앱d9ce823/종료 하네스5202b20의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38239838261) 통과. 같은 앱 소스의 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38238891072) 통과. QA 이후 단일 에이전트 자체 리뷰.

- Core168/168·Release 경고0/오류0·locked restore/format/analyzer/publish, WPF929고유검사 실패0. 설정 검사19종: 실제 창 재생성/자동 저장·종료 flush/마이크 분리·순서·재연결·빈 목록과 입력 factory0/최대화·최소화·트레이 정상 bounds/방송창 크기·좌표·열림·닫음·TwoWay 바인딩/음소거·탭·슬롯·내장 얼굴/임시 전체 테스트/저장 실패 이전 파일 보존·재시도 확인.
- unsigned/signed EXE 각각9개 일반 프로세스 사례의10초 유지·정상 종료·슬롯/순서 불변. restart-settings에서 두 창/마지막 설정/마이크 선호 ID 복원을 확인했다. Process.MainWindowHandle이 복원된 방송창을 선택하는 초기 종료 fixture 문제를 Ready의 설정창 HWND로 수정했다.
- 일반 시작 JSON은 worker 읽기, 자동 저장은350ms 지연·worker·순차/원자 교체, 종료는 마지막 snapshot 확정. endpoint ID는 사용자 요청에 따라 로컬 설정에만 저장하고 진단·시작 증거에는 기록하지 않는다. 실제 사용자 설정은 CI로 보내지 않았다.
- OBS WGC/크로마·투명 alpha·동일 HWND/소스 배경 왕복 회귀 유지. BitBlt의 기존 layered 제한 유지.
- 개인 ZIP32파일/100483996bytes/CRC/서명 EXE·CER 해시·기존16PNG byte-identical. 이전 ZIP·원본 보존. 증거 artifacts/qa/plan022/, 배포 artifacts/VoxPet-win-x64-settings-plan022.zip.

혼합 DPI 다중 모니터/RDP·물리 마이크와 사용자 GPU/OBS 실기는 별도다.350ms 자동 저장 전 프로세스 강제 종료는 마지막 변경이 남지 않을 수 있다. 정상 종료·재실행 검사는 실제 Windows 프로세스와 창에서 확인했다.

## plan020 손동작·의상/장식 연결

2026-10-12 최종 코드0a363e5의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38248447249), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38248445309) 통과. QA 이후 단일 에이전트 자체 리뷰/종료 경합 보완 재검증.

- Core177/177·Release 경고0/오류0·locked restore/format/analyzer/publish. 기본5캐릭터/공용 고양이×5동작의 연결 격자 삼각형 뒤집힘0, 유한 시간/관절·모드/간격·중복 입력·예약/중복 키·망토 enum8/구 의상 연결 기본값·원자 저장/손상 복구.
- 실제 WPF1134고유검사·실패0. 모션27종: 소매 표식 이동/몸통 유지·동일 변환/머리 pivot·입+머리 상속/영향도0/5동작 얼굴 원본 보존, 동작/캐릭터별 영역 자동 저장·마지막 종료 편집 flush·재시작·읽기 전 종료 보존, 같은 캐릭터 표정 변경 중 손 유지/다른 캐릭터 취소/사용자 PNG 자산 종류 복원, native 키 등록·충돌/WM_HOTKEY/재설정·해제/실제 합성 좌버튼 down·해제. 의상 연결 자동 저장·미완료 착용 종료의 마지막 표시값 보존.
- 7탭/새 하위 페이지×4창크기, 검정 선택·팝업 글씨·스크롤 없음. 첫 실행에서 발견한 탭 중첩20검사/비동기 blink fixture1검사를 수정했고 사각 팔 회전의 빈틈은 연결 격자/영역 비율 회전으로 보완했다. 재검증 렌더 PNG 시각 확인.
- unsigned/signed EXE 각각9개 일반 실행/10초 유지·정상 종료·마이크 선호/창 크기·위치/탭/표정/캐릭터·슬롯/순서 보존. 오디오 입력 자동 시작 없음.
- OBS32.2.2 WGC 초록/크로마/투명 native alpha·같은 HWND/소스 배경 왕복 통과. 기존 BitBlt layered 제한 유지. 실제 마이크/사용자 GPU/개인 PNG는 합성 CI와 구분한다.
- 김태중 자체 서명·공개 CER·EXE 해시/아이콘·CI 임시 Root 신뢰 후 Valid/변조 HashMismatch. Root/키 finally 정리. 개인키 포함/신뢰 자동 설치 없음.
- 개인 ZIP32파일/100521104bytes/CRC·SHA256 3e9bcc0adcb4ee6a8b10783bd301016daa6d33edb0dcdd46ae659147c273f1bf, 기존16PNG byte-identical/이전ZIP·원본 보존. 개인PNG 공개 전송 없음. 증거 artifacts/qa/plan020/, 배포 artifacts/VoxPet-win-x64-motion-plan020.zip.

손가락 새 작화/자동 관절 인식이 아니라 원본 팔·날개의 연속 변형과 native 소품이다. 머리 흔들림은 선택형, 입 장식 이동량은 수동 조절값이다. 큰 이동·확대의 왜곡은 손 영역/배치로 조정한다. 실제 개인PNG/Windows11 물리 마이크·키보드/사용자 GPU·다중 모니터 실기는 대상PC 항목이다.


## plan023 전체 재시험과 실제 Windows 데스크톱 화면

2026-10-12, 코드c7d2f0f의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38255728224)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38255728219), 정상 앱 동작이 동일한 main1939f3c의 [장시간 OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38252444387)를 다시 실행해 통과했다. plan023 앱 변경은 QA fixture/helper/CI guard뿐이며 renderer/audio/settings 정상 동작은 변경하지 않았다. 사용자는 GitHub Windows 화면 시험을 선택하고 개인 PNG 외부 전송을 거부했다.

- Core177/177(macOS/Windows), locked restore/format/analyzer/Release 경고0·오류0·자체 포함 publish. 실제 WPF1346고유검사·실패0. 일반/서명 EXE 각각9개 시나리오 정상 종료와 재실행·선택·설정·단일 실행/트레이 복원 통과.
- 실제 표시된 HWND 영역에서 desktop SRCCOPY|CAPTUREBLT로225장 수집. 기존 RenderTargetBitmap 검사와 별개이며 실패 시 대체하지 않았다. foreground HWND/색상/OS·bounds·PNG 크기/CRC/소스 기반 coverage 검사와225장 전수 직접 시각 검토를 모두 통과했다.
- 설정7탭21페이지×4크기84장,12표정×3입×2눈72장과 설정창12장,5손동작×4시점20장,머리·입 장식6장,방송/트레이4장,5캐릭터 선택5장,설정 초기/재생성4장,일반/서명 실행18장. 공개 고양이/합성 시트/실제 ImportAsync로 읽은 합성 의상만 사용했다. 소매·손/몸통/머리 pivot/입+머리 상속은 기존 픽셀 위치 검사를 함께 유지했다.
- 첫 실행38254012931은 수동으로 추가한 의상 fixture의 WearCommand 누락 때문에 종료에서 NullReferenceException이 발생했다. 실제 Wardrobe.ImportAsync 경로로 수정했고 전체 재실행으로 통과했다. 상한만 늘리거나 기존 검사를 제외하지 않았다.
- OBS32.2.2 WGC·각7프레임 변화·초록 크로마/투명 native alpha·같은 HWND/소스 배경 전환 통과. 장시간10분 warmup 후60.05분 측정·RSS 증가-2.39MiB·핸들 증가-2·프레임 변화67회. 기준RSS50MiB/핸들50 이하 통과. BitBlt의 layered 캡처 실패는 기존 runner 제한으로 유지하며 WGC를 사용한다.
- 배포 EXE 서명자 김태중/CI 임시 신뢰 Valid/변조 HashMismatch/공개 CER·개인키 미포함 확인. 로컬 개인16PNG의 SHA256/크기와 패키지 복사 바이트 일치. 새 ZIP32파일/100540586bytes·CRC 통과·SHA256 `648823cb71561877d8b66735862581a3ee3aa868f3cd456d511a046fc7de815f`. 이전 ZIP/원본 보존.
- 로컬 증거: artifacts/qa/plan023/windows/gallery.html, windows/_temp/voxpet-screen-coverage.json, visual-review.json, obs-short-complete/, obs-long/, package-summary.json. 갤러리의225장 이름/해시를 시각 검토 기록에 보존했다.

개인 PNG의 실제 Windows 모습·물리 마이크/권한/USB 제거·물리 키보드/Shell·실제 혼합 DPI/사용자 GPU는 이번 환경에서 시험하지 않았다. CI의 합성 자료/가짜 장치 결과로 해당 실기 합격을 주장하지 않는다. 개인 PNG는 외부 전송 없이 새 로컬 ZIP에만 동봉했다.


## plan025 참고 디자인·두 캐릭터·이미지 조합 런타임

2026-10-13 코드1dc9750의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38372703545), [OBS 합성 QA](https://github.com/taejun9/VoxPet/actions/runs/38372703485) 통과.

- locked restore/formatter/analyzer/Release/publish, Core199/199, 경고0/오류0. WPF1487고유검사·실패0. 4창크기/7메뉴/25하위페이지 및 실데스크톱240화면의 가시성·선택값/팝업 검정 텍스트·스크롤 없음·PNG CRC/커버리지 확인. 합성 래스터 배율과 실제 DPI 시험은 구분한다.
- 이미지 세트 등록/frozen PNG/두 창 공유/손상 등록 유지/무기 썸네일/입력 기록·해제/키 repeat 억제·다른 창에 전달/종료 훅 해제/등록 세트와 선택 재시작 복원/초기화 전 종료 보존 확인. 파츠 대체/숨김/소매·무기 포즈/Anchor/레이어/빠른 재시작은 Core 회귀로 확인했다.
- 일반/김태중 자체 서명 EXE 시작 검증과 기존 방송창 OBS 합성 캡처 통과. 개인 파츠·실제 목소리·게임·사용자 OBS는 전송하거나 시험하지 않았다.
- 선택 목록은 늘보군/펠리 두 종. root artifacts/VoxPet-reference-plan025.zip에 기존 개인 PNG13장을 로컬 복사하며 원본 byte equality와 ZIP CRC를 artifacts/qa/plan025/package-report.json에 기록한다. 과거 캐릭터 원본/패키지는 삭제하지 않는다.
- 첫 실패는 새 fixture의 가상 작업 영역, 두 번째32실패는 신규 문자열 ComboBox 글자색이었다. 실제 화면 범위와 명시적 검정 템플릿으로 수정하고 전체 재실행했다.

실제 마이크/OBS/게임 입력 권한·차단/장시간/실제 DPI는 기존 미확인 항목을 유지한다. 새 분리 파츠는 [제작 규격](../product/image-set-format.md)에 따라 준비해 로컬에서 시각 검증한다.


## plan037 VoxPet 2.0 편집기 자동 검증

2026-10-15 최종 d9c55de의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38668937751), [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38668937607) 통과. Core355/355·Release 경고0/오류0·WPF1744/0·실제 desktop250화면·unsigned/signed 각각9개 일반 시작과 정상 종료·저장 파일 보존을 확인했다.

공개 제작 틀로 생성/편집/실제 Anchor drag/16입/full-sprite banks/8종 timeline/trigger/pose/부착 preview·export/reimport를 검사했다. 지원 필드·원본 PNG bytes·preview/native broadcast frame exact 비교와4크기×8페이지의 입력 접근성/글자 대비가 통과했다.10개 새 화면을 시각 검토했다. OBS32.2.2 WGC/크로마/투명 native alpha/같은 HWND 배경 왕복이 통과했다. BitBlt는 기존 제한이다.

증거는 artifacts/test-results/plan037/last에 보존한다. 이번 시험은 public/synthetic 자료만 쓰며 물리 마이크/권한/USB·혼합 DPI/사용자 GPU·실제 입력 장시간을 대체하지 않는다.
