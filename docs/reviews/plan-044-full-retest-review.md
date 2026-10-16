# plan-044-full-retest Review

## Summary

사용자의 전체 재시험 요청에 따라 최신 main22215a6을 bin/obj 없는 새 작업트리와 새 Windows/OBS runner에서 다시 검증했다. 모든 QA 후 동일 에이전트가 별도 단계로 자체 리뷰했다. 앱1.0.1의 카드 표시 오류 수정을 유지했고 추가 앱/시험 코드 변경은 없다.

## QA

- SDK10.0.401 locked restore, format verify/analyzer, Release 전체 빌드 경고0/오류0, Core380/380/skip0, verify_base/verify_app/compileall/diff check 통과. 로컬 TRX와 판정은 `artifacts/test-results/plan044/local/`에 보존한다.
- main22215a6의 새 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38913337799): Core380/WPF2090고유검사·실패0. 이전 plan043의2090개 검사 집합과 정확히 같다. 카드 worker/frozen/가변/null 이미지의 이름 읽기·암시적 WPF 텍스트 바인딩·병렬 읽기·실제 Home/End·영문/한글 검색·접근성 이름·필터/빈 결과·반복 선택/편집/내보내기/삭제 원본 보존/재시작 회귀 통과.
- 실제 desktop324장 coverage·PNG CRC, 일반/서명 EXE 각각9사례의10초 실행·정상 종료·저장 파일 보존, 서명/아이콘/변조·임시 키 정리 통과. ZIP을 새 폴더에 풀고 Windows PowerShell5.1 동봉 도구로1784고유검사/실패0 확인.
- 새 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38913348630): OBS32.2.2 WGC 초록/크로마/native alpha·같은 HWND의 배경 왕복·각7프레임 변화 통과. 실제 녹화/송출/마이크 사용 없음. BitBlt는 기존 layered 창 제한이다.
- 의상14분류/17종·동작9종·모자8프레임80ms·날개6프레임140ms·단발/장발/폭탄머리 포함, 편집/왕복/공통 미리보기와 방송 공유 회귀 유지. 임시 자산 점검에서 날개의6프레임이 모두 달라야 한다는 가정은 실패했으나 생성기/PNG를 확인해0·1·2·3·2·1 왕복4자세가 의도임을 확인하고 점검을 바로잡았다. 앱 오류나 QA 실패를 무시하지 않았다.
- 공개 실제 화면75장을 직접 검토했다. 카드 선택/편집/재시작3장은 원본 크기로도 확인했다. 원본 샘플20장과 OBS4캡처도 직접 확인했다. 추가 필수 시각 수정 사항은 없다. 작은 창/편집기의 스크롤을 유지한다.
- `artifacts/VoxPet-Windows-test-plan044.zip`:52파일/95,870,506bytes, SHA256 `11e192512b8e575897a31fc3a3f829f0c3435b4eec5c7f620e3161ce10233d18`, EXE SHA256 `5ef96b90c5e6d2e079ccefdf08880d6e12d52b07265860ca2605f96bf70941f7`. 공용39파일은 새 CI에서 시험한 ZIP과 동일하다. 개인13PNG는 기존 plan043 ZIP에서 읽어 이전 전달 해시 목록과 대조한 뒤 바이트 그대로 추가했다. 샘플 패키지 소스 일치/PE x64/CRC 통과. 이전 ZIP은 덮어쓰지 않았다.
- 사용자 last-error.json SHA256은 이전 기록과 같고 외부 전송하지 않았다. README/AGENTS·앱/Core/오디오/하네스/테스트/자산과 다른 plan024 작업트리2개를 변경하지 않았다. 완료 변경은 이 계획/리뷰/검증 기록 문서뿐이다.

## Findings

plan043에서 보고된 InvalidOperationException/80131509/CharacterCard.PrintMembers→ToString을 수정 전 실제 WPF에서 재현했고 Name 전용 출력·RuntimeLayer 안전 레이블·명시적 검색/접근성 이름으로 수정한 근거를 재검토했다. 원본 오류와 재현의 종류/코드/메서드 일치를 보존했으며 이번 새 Windows 실행에서도 해당 회귀가 통과했다. 이미지 디코더를 문자열화하거나 전역 예외를 무시하는 우회는 없다. 앱/시험/동봉 자산은 검증한 main22215a6과 동일하고 이전 검사 누락이 없다. 추가 필수 수정 사항은 없다.

## Residual Risk

자동 시험은 실제 Windows WPF를 공개/합성 자료·가짜 입력으로 실행한 결과다. 물리 마이크/권한/USB·사용자 GPU/OBS/DPI·개인 그림 Windows 시각 확인·새60분 실기는 별도다. 모든 환경의 결함0을 보장하지 않는다. BitBlt layered 제한이 있으므로 OBS는 WGC를 사용한다. 배포 EXE는 unsigned이며 CI의 일회용 서명 시험은 사용자 신뢰 설정을 변경하지 않는다.

## Follow-Ups

기존 앱을 종료하고 새 폴더에 ZIP 전체를 풀어 START.cmd로 실행한다. TEST-SMOKE.cmd는 실제 합성 키/클릭을 사용하므로 전용 Windows/VM에서 실행한다. [패키지 안내](../quality/windows-test-package.md)와 [실기 체크리스트](../quality/windows-checklist.md)의 대상PC 항목을 확인한다. 새 오류는 앱 버전1.0.1.0인지와 조작 순서·최소 오류 JSON으로 구분한다.
