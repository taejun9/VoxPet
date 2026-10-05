# VoxPet 사용 안내

VoxPet은 마이크 음량에 맞춰 PNG 고양이의 입과 몸을 움직이는 Windows 앱이다.
음성은 로컬 메모리에서만 분석하며 저장하거나 전송하지 않는다. 데모는 마이크를 사용하지 않는다.

## 실행

Windows x64에서 배포 폴더의 `VoxPet.exe`를 실행한다. 자체 포함 배포본은 .NET 설치가 필요 없다.
폴더의 Licenses와 자산 manifest는 실행 파일과 함께 보관한다.
현재 권장 검증 대상은 Windows 11 x64의 지원 중인 버전이다. Windows 10 전체 버전의 호환성을 보장하지 않는다.
빌드와 UI smoke 통과 여부는 [검증 기록](quality/windows-checklist.md)에서 확인한다.
개발 실행/배포 명령은 [개발 환경](quality/development.md)에 있다.

1. 마이크를 선택한다. 연결 후 목록에 없으면 ↻로 새로고침한다.
2. Start를 누르고 말한다. 분석 중 상태와 선택 장치가 하단에 표시된다.
3. 캐릭터가 너무 자주 반응하면 Noise Gate를 높인다. 말할 때 반응이 약하면 Sensitivity를 올린다. 아주 작은 입력은 고급 입력 반응 범위에서 낮은 입력과 Gate를 함께 낮춘다.
4. Attack은 입이 열리는 속도, Release는 닫히는 속도다. 값이 클수록 천천히 반응한다.
5. Stop은 캡처를 해제하고 레벨을 0으로 초기화한다. 장치 교체는 Stop 이후 선택한다.
6. 마이크 없이 데모 버튼으로 캐릭터·방송창 동작을 확인한다. Stop으로 종료한다.

RMS/Peak는 full scale 기준이고 dBFS는 상대 레벨이다. 실제 음압 dB SPL이나 발화 여부를 판정하지 않는다.
Peak가 1 이상이면 CLIP이 표시된다. Windows 입력 볼륨을 낮춰 확인한다.
Gate는 음성과 키보드 같은 소음을 의미적으로 구별하지 못한다.
무입력이 250ms 넘게 지속되면 레벨을 무음으로 취급하고 Release를 적용한다.

## OBS 방송창

캐릭터 방송창 열기를 누르면 설정 화면과 별개의 `VoxPet Character` 창이 열린다.
두 창은 하나의 마이크 세션과 캐릭터 상태를 공유한다. 방송창만 닫아도 분석은 계속된다. 설정창을 최소화해도 방송창은 계속 표시된다.
방송창을 드래그해 이동하고 우측 하단에서 크기를 조절한다. Esc 또는 우클릭 메뉴로 닫는다.

OBS에 Window Capture 소스를 추가해 `VoxPet Character`를 선택한다.
캡처 방식은 Windows Graphics Capture(OBS에서 Windows 10 이상 방식)를 먼저 시도한다. OBS 32.2.2/Windows Server 2025의 합성 데모 검증에서 이 방식은 통과했고 BitBlt는 유효한 프레임을 얻지 못했다. 사용자 GPU/Windows 조합은 별도 확인한다.
초록 배경을 켜고 OBS의 Chroma Key 필터에서 Green을 선택하면 배경을 제거할 수 있다.
초록 배경을 끄면 WPF 창이 투명해지지만, OBS 캡처에서 alpha가 보존되는지는 버전/방식에 따라 실기 검증이 필요하다.
위 OBS 32.2.2/WGC 합성 시험에서는 native alpha도 보존됐다. 사용자 환경에서 검은 배경이나 캡처 실패가 보이면 초록 배경과 Chroma Key를 사용한다.
최소화/가림/스케일/장시간 캡처는 [체크리스트](quality/windows-checklist.md)로 확인한다.

## 오류 해결과 저장 설정

- 마이크 없음: USB/오디오 연결과 Windows 소리 설정의 입력 장치를 확인한 뒤 새로고침한다.
- 접근 거부: Windows 설정의 개인정보 및 보안 → 마이크 → 데스크톱 앱의 마이크 접근 허용을 확인한다.
- 연결 제거/캡처 중단: 자동으로 무음과 오류 안내로 전환한다. 다시 연결하고 새로고침 → Start로 재시작한다.
- 종료 지연: 5초를 넘으면 오류를 표시하며 사용 중인 캡처를 강제로 해제하지 않는다. 잠시 후 Stop/앱 종료를 다시 시도한다.
- 설정은 앱 종료 때 `%LOCALAPPDATA%\VoxPet\settings.json`에 저장한다. 조정값과 방송창 배경/항상 위 옵션만 포함한다.
- 설정 파일이 손상되면 기본값으로 시작한다. 기본값 버튼은 오디오 슬라이더를 초기화한다.
- 기본 캐릭터는 독자 제작한 CC0 PNG 6장이며 [manifest](../src/VoxPet.App/Assets/Characters/manifest.json)에 크기/anchor/출처를 기록했다.

마이크 캡처·Windows/OBS 실기·DPI/장시간 검증이 끝나기 전에는 정식 실사용 검증 완료 버전으로 취급하지 않는다.
