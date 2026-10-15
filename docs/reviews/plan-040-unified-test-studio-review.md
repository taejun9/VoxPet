# plan-040-unified-test-studio Review

## Summary

새 사이드바로 일반 실행의 화면을 통합하고 공개 의상14분류·동작9종을 번들로 제공했다. 단일 에이전트가 QA 후 자체 리뷰했으며, 카드/선택값 대비와 설정 이름 문제를 보완한 최종 c8bcf46을 전체 재검증했다. 미해결 필수 수정 사항은 없다.

## QA

- 로컬 SDK10.0.401 locked restore/format/analyzer/Release 경고0·오류0/Core369/369, verify_base/verify_app/Python compileall/diff check 통과.
- 최종 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38733607211): Core369/369·WPF1981고유검사/실패0·실제 desktop305화면·unsigned/signed 각각9개 일반 시작/정상 종료/저장 보존 통과. 생성한 ZIP을 풀어 PowerShell5.1 동봉 도구1694고유검사/실패0도 확인했다. JSON/TRX/로그를 직접 대조했다.
- 샘플 원본57파일(32PNG/14의상/9동작/character·manifest)은 모든 분류/템플릿·로드/왕복·착용/해제/충돌·관절 이동/효과/idle 복귀를 검사했다. WPF는 실제 재생 버튼, 공통 미리보기/별도 방송창, 샘플 반복 선택의 관리 복사본 재사용, 설정 보존과 마이크 factory0을 확인했다.
- 신규55화면과 변경된 캐릭터 목록을 포함한56개 캡처를 직접 시각 검토했다.6페이지×4요청크기·PNG 설정7항목·의상14종·동작9종·방송창이며 가장 큰1280×820 요청은 CI 작업 영역에 맞춘 실제992×688이다. 나머지는960×540/640×480/480×320이다. 선택값/팝업30검사와 이름/패널 의미 대응도 통과했다.
- 최종 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38733607371): OBS32.2.2 WGC 초록+크로마/native alpha/동일 HWND 배경 전환·각7프레임 변화 통과. BitBlt의 기존 layered 제한은 유지한다.
- 로컬 개인 ZIP `artifacts/VoxPet-Windows-test-plan040-final.zip`:52파일/95,605,036bytes·CRC·PE x64 통과, SHA256 `61b2183499cc2872d99156805844b7d85f7ec2840ed78d94396f98e44a0cbc3d`. Windows에서 시험한 공용 ZIP의39파일을 실행 도구의 줄바꿈까지 byte-identical로 유지하고 이전 개인13PNG만 그대로 추가했다. EXE SHA256 `0a73512788d142ba7e4733e26fe9a3269147524d59d3d0450d208ac9832cc30c`.
- 증거는 `artifacts/test-results/plan040/last/`의 Windows/OBS JSON·로그·화면·visual-review.json·package-verification.json에 보존한다. 이후 변경은 완료 계획·리뷰·체크리스트뿐이며 앱/실행 도구/배포 안내는 CI 기준과 같다.

## Findings

| 중요도 | 근거 | 조치와 확인 |
|---|---|---|
| P2 | MainWindow.ShowLegacySettings / StudioShell.LegacySettings | 설정 버튼이 전체 이전 화면으로 이동해 navigation이 혼재했다. 설정 패널을 Studio SettingsHost로 옮기고 일반 동선을 ShowStudioSettings로 통합했다. 이전 전체 화면은 기존 자동 회귀 fixture에만 남기며 START.cmd의 일반 UI는 같은 사이드바를 유지한다.6페이지·설정7항목·설정값 보존 회귀로 확인했다. |
| P2 | StudioShell DataTemplate / SettingsSection | 첫 Windows 실행에서 카드 글자 대비1실패를 재현했고 이후 시각 리뷰로 선택값의 밝은 글씨도 확인했다. 이름·버전과 데이터 종류별 선택 템플릿을 명시했으며 글씨의 실제 존재/색상과 팝업을 검사했다. 최종56화면에서 해결을 확인했다. |
| P2 | StudioSettingsResources.Button / MainWindow 표정·의상 버튼 | 일반 ContentTemplate이 사용자 지정 TextBlock 내용을 문자열화할 위험이 있었다. 해당 버튼의 ContentTemplate을 해제하고 Foreground를 명시 상속했다. 사용자 지정 내용 보존 검사와 실제 표정 슬롯 화면을 확인했다. |
| P2 | StudioShellViewModel.SettingsSections | 기존4번 패널은 눈 깜빡임/눈물인데 표정 시험으로 표기했다. 이름과 안내를 실제 내용으로 맞추고 표시된 CheckBox와 선택 항목 의미를 검사했다. |

## Residual Risk

실제 사용자 Windows11의 물리 마이크/권한/USB 제거·GPU/OBS·혼합 DPI·60분 실기는 수행하지 않았다. 개인13PNG는 외부 전송 없이 로컬 무결성만 확인했다. 작은 창은 스크롤로 접근하며 기존 편집기는 내부 가로 이동도 사용한다. CI의 합성 입력·가상 작업 영역 시험을 물리 실기 합격으로 표현하지 않는다.

EXE는 unsigned이며 CI의 별도 자체 서명 시험은 사용자PC 신뢰/실행 경고 제거를 뜻하지 않는다. 자동 시험은 합성 키·좌클릭과 이전 회귀용 화면을 사용하므로 전용 Windows/VM에서 실행한다. 수동 조작은 START.cmd를 사용한다.

## Follow-Ups

[샘플 안내](../guides/studio-samples.md)와 [직접 시험 안내](../quality/windows-test-package.md)로 대상PC 실기를 수행하고 [체크리스트](../quality/windows-checklist.md)에 OS/GPU/마이크/OBS 버전과 결과를 기록한다. 샘플은 개인 PNG를 바꾸지 않으며 원래 캐릭터를 목록에서 다시 선택할 수 있다.
