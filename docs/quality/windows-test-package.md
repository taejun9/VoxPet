# Windows 직접 테스트 패키지

Windows QA workflow의 `VoxPet-Windows-test-package` artifact에서 `VoxPet-Windows-test.zip`을 받는다. ZIP 전체를 새 폴더에 풀고 `START.cmd` 또는 `VoxPet.exe`를 실행한다. 자체 포함 win-x64 EXE이므로 별도 SDK/runtime 설치는 필요 없다. 공용 unsigned 빌드이며 게시자 신원을 인증서로 검증한 배포가 아니다. 자산과 Licenses도 함께 보관한다.

## 마이크 없는 자동 검사

일반 앱을 닫고 `TEST-SMOKE.cmd`를 실행한다. Windows PowerShell 5.1에서 동작하며 실행 결과를 기다리는 동안 합성 키/마우스 입력과 창 테스트가 있으므로 다른 작업을 잠시 멈춘다. 180초 안에 완료하고 종료 코드0·JSON failures0·실행된 검사 목록을 함께 확인한다. 오류/시간 초과/JSON 누락은 실패다.

결과는 `%TEMP%/VoxPet-tests/<새 ID>/`에 PNG/JSON으로 남고 콘솔에 위치를 표시한다. 마이크·개인 PNG·기존 사용자 설정을 사용하지 않으며 네트워크 전송이 없다. 일반 사용자 실행에서는 CI desktop 캡처를 활성화하지 않는다. 자동 검사 결과는 물리 마이크/사용자 GPU/OBS 실기 합격을 의미하지 않는다.

`TEST-README.txt`의 수동 체크리스트에서 마이크 권한 거부·USB 분리/재연결·반복 Start/Stop·트레이·OBS WGC·혼합 DPI·60분 사용·편집/파일 실패 복구를 확인하고 OS/GPU/마이크/OBS 버전과 재현 순서를 기록한다. 개인정보를 공개 결과에 넣지 않는다. [실기 상태](windows-checklist.md)와 구분해 기록한다.

## 저장소에서 패키지 재생성

[Windows QA](development.md)의 `qa.ps1 -Publish -Smoke` 성공 이후 Python 3.10 이상으로 실행한다.

```powershell
python -X utf8 harness/scripts/package_windows.py artifacts/win-x64 artifacts/VoxPet-Windows-test.zip
```

기존 ZIP을 덮어쓰지 않으므로 새 출력 이름을 사용한다. 패키징은 PE x64·필수 자산/라이선스·중복/링크 파일을 검사하고 PDB를 제외한 publish 및 실행 도구를 묶는다. ZIP CRC와 SHA256을 출력한다. Windows CI는 생성한 ZIP을 다시 풀어 실제 포함된 테스트 도구로 자동 검사를 한 번 더 실행한다. Core 테스트·formatter/analyzer는 SDK를 갖춘 저장소 QA에서 별도로 수행한다.

## plan039 회귀 범위

`RuntimeSafetyQa`는 유효한 합성 패키지의 첫 화면/지연 동작에 극단적 변환을 넣어 실패를 재현한다. 첫 화면 준비 실패는 마지막 scene/document/저장 선택을 유지하고, 재생 중 실패는 마지막 화면과 앱을 유지하며 native 입력을 해제한다. 다시 선택하면 복구한다.

동작/표정/부착물 삭제 후 미리보기 참조, 앵커 없는 캐릭터 편집, 미적용 입력 중 삭제/드래그, 입력10000회 주입의 대기128개 상한도 실제 ViewModel에서 검사한다. 사용자 PNG·물리 음성은 사용하지 않는다.

패키지 Canvas 너비/높이는 각각1~16384로 검증한다. 극소/과대 값과 양쪽 경계·사용자 지정 상한은 Core에서, 편집 실패의 문서 보존은 WPF에서 검사한다.
