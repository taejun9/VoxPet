# plan-026-app-icon Review

## Summary

QA 이후 단일 에이전트 자체 리뷰. 사용자 지정 프로그램 그림의 원본 보존·PNG/ICO 변환·EXE/창/트레이/머리글 연결과 macOS 테스트 안내를 검토했다. 외부 독립 리뷰는 수행하지 않았다.

## QA

- macOS: locked restore/공식 취약성 확인, formatter/analyzer, Release 경고0·오류0, Core199/199, verify_base/verify_app/diff 통과.
- 첨부 원본과 source PNG byte equality, 출력 SHA256 재생성 일치,16/24/32/48/64/128/256 크기의 RGBA ICO 확인.
- 코드9b33c7b의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38376122497): EXE 추출 아이콘과 ICO 픽셀 일치, WPF1487검사 실패0, 일반/서명 EXE 시작과 트레이·240실제 화면 커버리지 통과. 제목 표시줄과 머리글 새 그림을 실제 desktop PNG로 시각 확인.
- [OBS 합성 QA](https://github.com/taejun9/VoxPet/actions/runs/38376122611) 통과.
- 새 로컬 패키지49파일 ZIP CRC 정상, 기존 개인 캐릭터13PNG가 원본과 일치한다. 개인 시트 외부 전송 없음.

## Findings

- 아이콘 변경은 ApplicationIcon과 기존 WPF/트레이 ICO 경로를 그대로 사용한다. 새 PNG Resource와 동일 크기 머리글 Image를 추가해 기존 레이아웃을 유지했다.
- generator가 이전 마이크 아이콘을 다시 만드는 문제를 없애고 지정 원본에서 생성한다. 사용하지 않는 이전 SVG는 제거하며 참조 잔여가 없다.
- 원본 그림의 배경·여백·색상은 보존한다. Pillow는 개발용 재생성에만 필요하며 앱 실행 의존성을 추가하지 않는다.
- Windows 전용 WPF/WASAPI/입력/트레이는 맥 네이티브 실행 가능으로 표현하지 않는다. Core와 Windows 대상 컴파일만 macOS에서 시험했다.
- 미해결 P1/P2 결함 없음. 오디오/이미지 런타임 동작 변경 없음.

## Residual Risk

실제 Windows 마이크·게임 입력·DPI·장시간·사용자 OBS 시험은 기존 체크리스트 대상이다. macOS GUI 이식이나 Windows 가상머신 실기는 이번 작업에서 수행하지 않았다.

## Follow-Ups

검증된 완료 기록을 main에 통합하고 정해진 Git 정리 절차를 수행한다. macOS 네이티브 실행이 필요하면 별도 UI·오디오·입력 이식 범위를 확정한다.
