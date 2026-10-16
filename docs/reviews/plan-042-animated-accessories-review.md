# plan-042-animated-accessories Review

## Summary

모든14분류/9동작을 유지하고 의상17종으로 확장했다. 헬리콥터 모자는8프레임/80ms, 망토는 날개6프레임/140ms로 착용 중 독립 반복한다. 단발·장발·폭탄머리는 머리 장식 분류로 하나씩 선택하고 모자와 함께 착용한다. 샘플1.1.0 선택은 이전 복사본을 보존한다. 프레임 수·속도·PNG를 편집하고 내보내기/재읽기해도 보존한다. 전체 QA 후 같은 에이전트가 자체 리뷰했다.

## QA

- 전용 새 작업트리에서 locked restore → format verify/analyzer → Release 경고0/오류0 → Core380/380/skip0. base/app/compileall/diff check 통과. 번들78파일/50개512RGBA PNG의 전체 CRC,14분류/17종/9동작, 모자8개 고유 그림과 날개4자세를 왕복하는6프레임의 인접 프레임 변화 확인.
- 최종 코드 dad72a1의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38881016955): Core380/380·WPF2037고유검사/실패0·실데스크톱321장·unsigned/signed 각9개 일반 시작/10초 유지/정상 종료/파일 보존. 이전1981개 검사 전부 유지하고56개 추가했다. Windows PowerShell5.1 동봉 도구로 ZIP을 다시 풀어1734고유검사/실패0도 통과했다.
- 17종 개별 착용/해제·9동작/관절/idle, 헤어 배타/모자 공존, idle4시점 실제 픽셀/올바른 프레임·방송 공유·해제, 이전 버전 관리 파일 bytes 보존/새 버전 재사용을 검사했다. 편집기 속도/프레임·관련 없는 수정 보존·잘못된 개수 rollback·정지/반복 전환·새 프레임 칸·preview·모든 PNG bytes/export/load 보존을 확인했다.
- 최종72화면을 직접 시각 검토했다. 선택값/팝업6검사와 기존 대비 검사, 편집 후 외부 스크롤을 끝까지 이동한 실제 메뉴 전환 회귀도 통과했다. 제목과 완전한 미리보기 복원을 확인했다. 작은 창과 편집기는 스크롤을 사용한다.1280×820 요청 페이지는 CI 작업 영역에 맞춘 실제992×688이다.
- 같은 dad72a1의 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38881016833): OBS32.2.2 WGC 초록/크로마·투명 native alpha·같은 HWND 배경 왕복·각7프레임 변화 통과. BitBlt는 기존 layered 창 제한을 유지한다. 녹화/송출·물리 마이크 없이 공개 합성 자료를 사용했다.
- `artifacts/VoxPet-Windows-test-plan042-final.zip`:52파일/95,863,839bytes, PE x64/CRC 통과. Windows에서 시험한 공용39파일과 기존 개인13PNG 모두 byte-identical이며 번들 샘플도 검증 소스와 같다. 이전 ZIP/원본 보존·개인 자료 외부 전송 없음. SHA256 `c45f653663f2d0438320d2a2fb5930965b0c7c6f92255abcab11a073680d571f`, EXE SHA256 `28cb15fde2811bfdcad49a3f9a779797f58a522abc3bf8ea90ce73186c22b833`.
- 증거: `artifacts/test-results/plan042/final/`의 Windows/OBS 로그·JSON/TRX/화면, windows-verification.json/obs-verification.json/visual-review.json/package-verification.json. 로컬 Core/샘플 검사는 `plan042/local/`, 앞선 두 실행과 중간 ZIP은 보존했다. 이후 변경은 기록 문서뿐이며 앱/하네스/테스트/동봉 안내는 최종 CI와 같다.

## Findings

첫 실행1dd8fab은 편집기 ComboBox의 흰 글씨와 정지→반복 신규 생성의 기본100ms를 반영하지 않은 fixture 기대 때문에2항목이 실패했다. 명시적 검정 item template과 선택값/팝업 검사를 추가하고 fixture에서160ms를 다시 입력했다. 두 번째3cc710c는 WPF2035/0·portable1732/0 통과했으나 시각 QA에서 편집→동작 이동의 외부 스크롤 잔류를 발견했다. 메뉴 전환 시 상단 복원과 실제 스크롤 회귀2검사를 추가한 dad72a1로 전체를 다시 실행해 통과했다. 검사 제외·시간 제한 완화는 없다.

Core 프레임 목록은 복사/읽기 전용이고2~64프레임/16~10000ms를 검증한다. 큰 유한 시간은 modulo 후 나누어 overflow를 막는다. 렌더 중 파일 I/O/가변 상태가 없고 모든 프레임은 기존 PNG/전체 decoded 메모리 상한을 적용한다. 정적 JSON 호환·포즈 PNG 우선/미지정 포즈 hide/fallback을 보존했다. 오디오·녹음/전송 경계와 README/AGENTS, 다른 plan024 작업트리2개는 변경하지 않았다. 추가 필수 수정 사항은 없다.

## Residual Risk

물리 마이크/권한/USB 제거·사용자 GPU/OBS·혼합 DPI·개인 그림의 실제 Windows 시각 확인과 새60분 측정은 대상PC 실기다. CI는 공개/합성 자료와 가짜 장치를 사용한다. 배포 EXE는 unsigned이며 CI 자체 서명 시험이 사용자PC의 실행 경고 제거를 의미하지 않는다. TEST-SMOKE는 합성 키/좌클릭을 사용하므로 전용 Windows/VM에서 실행하고 일반 수동 조작은 START.cmd를 사용한다.

헤어는 headAccessory로 리본과도 교체된다. 포즈 PNG를 명시한 레이어는 해당 그림을 반복 프레임보다 우선한다. 정지로 바꾼 뒤 반복을 새로 만들면 기본100ms이며 원하는 간격을 다시 지정한다. 샘플을 별도 개인 캐릭터로 관리할 때는 기본 정보의 ID/이름을 바꿔 내보낸다. 이전 샘플과 개인 자료는 보존한다.

## Follow-Ups

[샘플/편집 안내](../guides/studio-samples.md), [Windows 시험](../quality/windows-test-package.md), [실기 체크리스트](../quality/windows-checklist.md)로 대상PC의 물리 장치·DPI/OBS 결과를 기록한다.
