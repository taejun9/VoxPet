# plan-041-full-retest Review

## Summary

사용자 요청에 따라 이전 결과를 재사용하지 않고 새 작업트리와 새 Windows/OBS runner에서 전체 자동 QA를 처음부터 수행했다. 앱·테스트·실행 도구 소스는 main5c5dd4c와 동일하며 이번 검증 기준은 b44685e다. 모든 QA 후 같은 에이전트가 자체 리뷰했다. 새 필수 수정 사항이나 재현된 회귀는 없다.

## QA

- 새 작업트리에 bin/obj가 없음을 확인한 뒤 SDK10.0.401 locked restore → format verify/analyzer → Release build → Core 전체 테스트를 수행했다. 경고0·오류0·Core369/369/skip0, 샘플3/3 통과. 문서111개/자산36RGBA/잠금·금지 API/Python compileall/diff check도 통과했다. 샘플 ZIP57파일/32개512RGBA PNG의 전체 chunk CRC를 별도 확인했다.
- 새 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38874585603): Core369/369·WPF1981고유검사/실패0·실제 desktop305장·unsigned/signed 각9개 일반 시작/정상 종료/저장 파일 보존 통과. 지난 plan040의1981개 검사 집합과 정확히 일치해 누락이 없다. ZIP을 풀고 Windows PowerShell5.1 동봉 도구로1694고유검사/실패0도 확인했다.
- 단일 navigation·샘플14분류/9동작·착용/해제/한벌 충돌·관절/효과/idle 복귀·방송창 공유·반복 선택의 관리 복사본 재사용·선택값/팝업30검사·눈물 패널 이름 대응을 다시 통과했다. Studio/편집기 목록56캡처를 직접 시각 검토했다. 가장 큰1280×820 요청은 CI 작업 영역에 맞춘 실제992×688이며 나머지는960×540/640×480/480×320이다.
- 새 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38874637732): source b44685e 일치, OBS32.2.2 WGC 초록/크로마/투명 alpha·같은 HWND 배경 왕복·각7프레임 변화 통과. BitBlt는 기존 layered 창 제한을 유지한다.
- 새 `artifacts/VoxPet-Windows-test-plan041.zip`:52파일/95,605,003bytes, PE x64·CRC 통과. Windows에서 시험한 공용 ZIP39파일을 줄바꿈까지 byte-identical로 유지하고 기존 개인13PNG만 바이트 그대로 추가했다. 이전 ZIP/원본은 보존하고 개인 자료는 외부로 보내지 않았다. SHA256 `2cbb2b016a55222e94c1a0bf38688bc3971ceb25f0444cd3661670e027d027e0`, EXE SHA256 `a190363e58a30664f223932673bd05a99016a2bc5dd6f226c923f1ffa2f81ac0`.
- 증거는 `artifacts/test-results/plan041/`의 local TRX/하네스·샘플 무결성 결과, Windows/OBS 화면·로그·판정 JSON, windows-verification.json/visual-review.json/package-verification.json에 보존했다. 이후 변경은 계획·리뷰·체크리스트뿐이며 앱/하네스/테스트/배포 안내는 새 CI 기준과 같다.

## Findings

추가 앱 수정이 필요한 실패는 재현되지 않았다. 이전에 고친 카드/선택값 대비와 눈물 패널 이름도 실제 새 화면 및 회귀 검사에서 정상이다. 검사 제외나 합격 기준 완화 없이 통과했다.

plan040은 main push 이후 승인 검토의 사용량 제한으로 실행되지 않았던 정리만 남아 있었다. 사용자 재개 요청 후 정상 승인 경로로 같은 커밋 detach → branch -d → worktree remove를 완료했다. 기존 plan024 작업트리2개와 개인 자산을 보존했다.

## Residual Risk

물리 마이크/권한/USB 제거·사용자 GPU/OBS·혼합 DPI·개인 그림의 Windows 시각 확인과 새60분 장시간 측정은 이번 자동 재시험 범위와 구분한다. CI는 공개/합성 자료와 가짜 장치를 사용하며 마이크를 자동 시작하지 않는다. 작은 창은 스크롤하며 기존 편집기는 내부 가로 이동도 사용한다.

배포 EXE는 unsigned다. 자동 TEST-SMOKE는 합성 키/좌클릭과 이전 회귀용 화면을 사용하므로 전용 Windows/VM에서 실행한다. 일반 수동 조작은 START.cmd의 통합 Studio를 사용한다. CI 자체 서명 시험은 사용자PC 실행 경고 제거를 의미하지 않는다.

## Follow-Ups

[Studio 샘플 안내](../guides/studio-samples.md)로 의상 착용/동작 재생을 시험하고, [Windows 직접 시험](../quality/windows-test-package.md)과 [실기 체크리스트](../quality/windows-checklist.md)에 대상PC의 마이크/GPU/DPI 결과를 기록한다.
