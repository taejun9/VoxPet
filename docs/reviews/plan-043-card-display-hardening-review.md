# plan-043-card-display-hardening Review

## Summary

사용자가 제공한 최소 오류 기록을 진단 데이터로만 읽고 원본과 동일한 예외 종류/코드/호출 경로를 실제 Windows에서 재현했다. 이전 QA가 카드의 자동 문자열 변환 경로를 놓친 것을 확인하고 Name 전용 출력·유사 모델·명시적 검색/접근성 이름을 수정했다. 앱 버전은1.0.1이다. 모든 QA 후 같은 에이전트가 자체 리뷰했다.

## QA

- 새 작업트리에서 SDK10.0.401 locked restore → format verify/analyzer → Release 경고0/오류0 → Core380/380/skip0. base/app/Python compileall/diff check 통과. README/AGENTS·Core/오디오·샘플 PNG/패키지·개인 자료는 변경하지 않았다.
- 수정 전 d2ba151의 [Windows 재현](https://github.com/taejun9/VoxPet/actions/runs/38891560054): 실제 CharacterCard에 worker가 만든 frozen 샘플 PNG를 넣고 UI ToString을 호출했다. 원본과 동일한 System.InvalidOperationException/HResult80131509/CharacterCard.PrintMembers→ToString을 포착했다. 기존2037개 검사는 모두 통과하고 새 문자열 계약만 실패했다. 최소 진단 JSON은 공개 합성 fixture에서 새로 만든 것이며 사용자 오류 파일을 업로드하지 않았다. `repro/comparison.json`에서 사용자 기록의 메서드 집합 포함/종류/코드 일치를 대조했다.
- 최종772e757의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38892871936): Core380·WPF2090고유검사/실패0·실제 desktop324장·unsigned/signed 각9개 일반 시작/10초 유지/정상 종료/저장 파일 보존. 기존2037개 전부 유지하고53개를 추가했다. ZIP을 새 폴더에 풀어 Windows PowerShell5.1 동봉 도구로1784고유검사/실패0도 통과했다. 검사 제외/상한 완화는 없다.
- 새 회귀는 frozen decoder의 원래 스레드 경계와 안전한 픽셀 읽기, UI/worker 문자열·암시적 WPF 텍스트 바인딩·null/가변 이미지·유니코드 이름·record 복사/해시·1000회 병렬 이름 읽기를 검사한다. CharacterCard/EditorRow/RuntimeLayer가 이미지/복잡한 item을 문자열화하지 않음을 확인했다.
- 공개 합성 legacy/sample/native 카드5개의 실제 UI Automation 이름, Home/End 키로 다른 항목 선택, 영문/한글 text composition 검색, 이름/ID 필터·빈 결과 명령 비활성,4회 선택/새로고침, 손상 등록의 현재 scene 보존, 편집/내보내기/재읽기, 복사본 삭제의 원본 보존, 실제 창/모델 재생성 후 선택 복원을 검사했다. 편집기 표정/동작/부착물3개 선택 상자도 표시 이름 입력으로 다른 항목에서 첫 항목을 찾았다. 검색 prefix 대기는 WPF와 같은 Windows double-click 설정을 따른다. 입력 factory0 확인.
- 기존17의상/9동작·회전 모자/날개/3헤어·편집 왕복·메뉴 상단 복원·오디오/트레이/전역 입력/파일 실패 회귀를 모두 유지했다. 새 카드3화면과 기존 Studio/설정/편집72화면, 총75화면을 직접 시각 검토했다. 작은 창/편집기는 스크롤을 사용한다.
- 동일772e757의 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38892872151): OBS32.2.2 WGC 초록/크로마/투명 native alpha·같은 HWND 배경 왕복·각7프레임 변화 통과. BitBlt는 기존 layered 제한이다. 녹화/송출·실제 마이크는 사용하지 않았다.
- `artifacts/VoxPet-Windows-test-plan043.zip`:52파일/95,870,529bytes·PE x64/CRC 통과. 최종 CI 공용39파일은 byte-identical이며 개인13PNG는 남아 있는 plan042 ZIP에서 읽어 이전 전달 해시 목록과 대조하고 그대로 추가했다. 샘플 패키지도 최종 검증 소스와 같다. SHA256 `35f9cb87924b178ffc365a4f052fd9cbc786f6cb73e5ef490273f6b500d46eb5`, EXE SHA256 `dcbbd99017dde18b0c610ffa9b54b25cb049e96a4128151940a7c981f3fe83fe`. 이전 파일/오류 원본 보존·개인 자료 외부 전송 없음.
- 증거: `artifacts/test-results/plan043/local/`, `repro/`, `final/`. 최종 windows-verification.json/obs-verification.json/visual-review.json/package-verification.json 및 로그/TRX/화면에 판정과 소스 커밋을 보존했다. 이후 변경은 완료 기록 문서뿐이며 앱/하네스/테스트/동봉 자산은 최종 CI와 같다.

## Findings

원인은 frozen BitmapFrame의 그림 사용과 문자열 변환이 다른 경로인 데 있다. 자동 record PrintMembers가 ImageSource.ToString을 호출하면 [BitmapFrameDecode가 원래 decoder로 위임](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Imaging/BitmapFrameDecode.cs)하고 [BitmapDecoder.ToString은 VerifyAccess](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Imaging/BitmapDecoder.cs)를 한다(확인2026-10-16). 실제 worker 생성 이미지를 UI에서 문자열화하는 재현이 사용자 기록과 일치했다. 로그만으로 사용자의 정확한 조작은 단정하지 않는다.

CharacterCard.ToString은 Name만 읽게 바꿨고 직접 ImageSource를 가진 RuntimeLayer도 안전한 레이블만 반환한다. EditorRow는 이미 Name 전용이었다. 다른 이미지 보유 record는 표준 dictionary/container를 보유하며 이미지를 직접 출력하지 않음을 확인했다. 라이브러리는 TextSearch.Name 경로와 컨테이너의 접근성 이름을 명시하고 편집기 선택 목록도 Name을 사용한다. 이는 [TextSearch의 ToString fallback](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/TextSearch.cs)을 피하고 이름으로 검색하는 동작을 고정한다(확인2026-10-16).

전역 예외 처리/오류 종료를 바꾸거나 예외를 숨기지 않았다. 이미지 디코딩/렌더링/수명과 녹음/전송 경계는 유지했다. 표시 이름을 읽는 작업은 이미지에 접근하지 않는다. 재현 실패→수정 후 통과, 실제 사용자 조작과 배포 파일 일치를 확인했으며 추가 필수 수정 사항은 없다.

## Residual Risk

모든 환경에서 결함0을 보장하는 판정은 아니다. CI는 공개/합성 자료와 가짜 장치로 실행한 실제 Windows WPF다. 물리 마이크/권한/USB·사용자 GPU/OBS·혼합 DPI·개인 그림 Windows 시각 확인·새60분 실기는 대상PC 항목이다. 기존 작은 창/편집기 스크롤과 BitBlt layered 제한을 유지한다.

배포 EXE는 unsigned다. CI 임시 자체 서명 시험은 사용자PC 실행 경고 제거를 의미하지 않는다. TEST-SMOKE는 합성 키/좌클릭을 사용하므로 전용 Windows/VM에서 실행한다. 일반 조작은 기존 앱을 종료하고 새 폴더에 ZIP을 모두 풀어 START.cmd로 시작한다. 새 오류 기록의 앱 버전은1.0.1.0으로 구분할 수 있다.

## Follow-Ups

[Windows 직접 시험](../quality/windows-test-package.md)과 [실기 체크리스트](../quality/windows-checklist.md)로 대상PC의 마이크/GPU/DPI 결과를 기록한다. 새 오류가 발생하면 원본 최소 오류 기록의 앱 버전·시각·메서드와 조작 순서로 재현한다. 사용자 오류/개인 PNG 원본은 유지한다.
