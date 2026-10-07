# plan-012-full-qa Review

## Summary

전체 테스트·lint, 주석·README, 늘보군 실제 Windows 화면 검증을 QA 이후 동일 에이전트가 자체 리뷰했다. 서브에이전트는 실행하지 않았다. 최종 실행 코드는8fdfd36이며 개인 시트를 기본 공개 배포에 포함하지 않는다.

## QA

- macOS arm64/SDK10.0.401: locked restore, format/analyzer, Release build(경고0/오류0), Core97/97, verify_base.py/verify_app.py/diff --check 통과.
- [기본 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37637240867): Core97/97, 자체 포함 publish, 실제 WPF smoke/슬롯/전역 키/창 수명/작은 창 회귀 통과.
- [늘보군 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37637826804): Core97/97, smoke 실패0/명명 검사210개, 개인 PNG 여섯 상태/합성 반응/관리 복사본/재시작/손상 복구/표정 전환/무음 blink·눈물/두 창 공유/4크기×45컨트롤 통과. 실제 데모(Voice Level38%)/표정 미리보기/초록·투명 방송창/작은 창 PNG를 직접 확인했다.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37637240816): 기본 CC0 캐릭터 WGC green+Chroma Key/native alpha 및 각7프레임 변화 통과. BitBlt는 기존 runner의 유효 프레임 없음 상태다.
- 최종 세 실행의 workflow/deprecation/빌드 경고·오류0. C#35파일/XML doc100블록, XAML4개/Python4개 구문 통과. README 기존 링크 보존과 기능·자산36장·테스트97개·F12/저장 안내를 확인했다.
- 개인 실행 패키지17파일의 EXE/라이선스/manifest/원본 PNG/안내/화면 증거와 ZIP CRC, 원본 SHA256/manifest 일치를 확인했다.

## Findings

- 해결: README의84개/PNG6장 기록과 표정 슬롯 기능 누락을 최신97개/36장·12슬롯·저장/전역 키 안내로 갱신했다.
- 해결: 주석에 전환 초·눈물 좌표/주기·슬롯 읽기 복구·F1~F11/F12 범위를 보완했다. 정적 PNG 헤더 검사의 성공 문구에서 실제 픽셀 정렬 검증 주장을 제거했다. 오디오 production 실행문을 변경하지 않았다.
- 해결: 구형 GitHub Actions의 Node20 및 의존성 deprecation 경고를 공식 Node24 Actions로 갱신하여 제거했다. contents:read와 SDK/package 잠금을 유지했다.
- 해결: 첫 개인 시험37636070658이 추가 모션/화면 시험으로60초 상한을 넘겼다. 기본60초/개인90초의 제한을 명시하고 시험 항목을 그대로 유지했다. 무음 표본의 입 초기 상태와 늘보군 눈물 시작점을 실제 캡처에 맞췄다.
- 해결: 개인37637307666의 입력 URL jwt:expired는 새 단일 자산 URL로 재시험했다. 최종 개인 실행은 모든 판정을 통과했다.
- 개인정보/보존: 사용자가 개인 시트 전송을 명시 승인하고 삭제하지 말라고 요청했다. 원본/private draft/화면 증거/직접 시험 ZIP을 보존했고 임시 URL secret만 제거했다. 앱/CI에 장기 계정 토큰이나 음성을 전달하지 않는다. persistence fixture가 제거/손상시키는 파일은 임시 저장소의 시험 복사본이며 사용자 원본은 보존한다.
- 요구사항·회귀·문서 자체 리뷰에서 미해결 구현 결함은 발견하지 않았다. Git 통합과 정리 결과는 최종 보고/로컬 summary에 기록한다.

## Residual Risk

Windows CI는 합성 숫자·키 입력을 사용한다. 실제 마이크/권한/장치 제거/물리 키보드·DPI와 사용자 OBS/GPU/실제 입력 장시간은 미실행이다. OBS BitBlt는 이 runner에서 유효 프레임을 만들지 못하므로 WGC 결과와 구분한다. 개인 PNG의 공개 재배포 권리는 확인하지 않았으며 공용 기본 자산은 기존 CC0 고양이다.

## Follow-Ups

사용자는 root artifacts/VoxPet-win-x64-neulbo-plan012.zip을 Windows에서 풀고 동봉한 늘보군-테스트안내.txt에 따라 데모/마이크/슬롯 저장/방송창을 직접 확인할 수 있다. 원본은 artifacts/characters/neulbo/늘보군.png에 보존한다. 실제 장치 시험 결과는 Windows 체크리스트에 환경과 함께 추가한다.
