# plan-050-emoji-natural-motion Review

## Summary

2026-10-19, 구현 612ce89에 대해 **전체 QA 이후 단일 에이전트 자체 리뷰**를 수행했다. 별도 reviewer/서브에이전트의 독립 리뷰가 아니다. 사용자 요구(이모티콘 스타일 자연스러운 모션, 늘보군과 펠리 각각의 자연스러운 모션 및 의상 수정, 윈도우 테스트 도구 및 미리보기 제작, UI/UX 정리정돈)의 완결성, 모션 역학 안정성, 기존 회귀 유지, 스레드/수명, 개인정보 보호 원칙을 점검했다. 미해결 필수 수정 사항은 없다.

## QA

- Python 문서 및 계약 검사: `python3 harness/scripts/verify_base.py` 통과 (130개 Markdown 구조·링크·계획·리뷰·출처 무결성 검증).
- Python 앱 및 자산 검사: `python3 harness/scripts/verify_app.py` 통과 (36 RGBA PNG 헤더, 두 공개 컴패니언 팩, 고정 패키지, 녹음/원격 전송/UI 차단 API 없음).
- Git 형식 검사: `git diff --check` 통과 (공백/줄 끝/포맷 오류 없음).
- Core 테스트 확장: `MotionEngineTests`, `CharacterCatalogTests`, `CostumeTests`에 캐릭터별 모션 프로파일(Neulbo, Pelly), 스쿼시/스트레칭 호흡, 소프트 블링크, 캐릭터 적합성 및 맞춤 배치 검증을 추가했다.
- 하네스 및 브라우저 검증: `harness/preview/index.html`을 통해 60fps 물리 시뮬레이션(임계 감쇠 스프링, 호흡 스쿼시, 발 접지 IK, 12표정 전환, 의상 레이어링)을 독립 실행 환경에서 실시간 검증 완료했다.

## Findings

| 구분 | 발견·수정 | 최종 근거 |
|---|---|---|
| 이모지 자연 모션 | 기존 선형 바운스 대신 호흡에 따른 미세 스쿼시-스트레칭(수축/팽창)을 `IdleMotion`과 `VoiceMotion`에 선택형 매개변수로 도입하고, `BlinkMotion`에 부드러운 눈꺼풀 전환(smoothstep 감쇠)을 추가했다. | `IdleMotion.Squash`, `VoiceMotion.Squash`, `BlinkMotion.Smooth` 단위 테스트 통과 및 기본값 0을 통한 100% 하위 호환성 유지. |
| 캐릭터 프로파일 | 느긋하고 포근한 나무늘보(늘보군)와 경쾌하고 즉각 반응하는 펠리컨(펠리)의 고유 모션을 `MotionProfile.Neulbo`, `MotionProfile.Pelly` 및 `ForCharacter` 메서드로 정립했다. | 늘보군(주기 4.8s, omega 11 rad/s, 여유로운 바운스), 펠리(주기 4.2s, omega 15 rad/s, 부리 끄덕임 및 날개 반응) 매개변수 분리. |
| 의상 맞춤 설계 | `CostumeProfile`에 `TargetCharacter`, `FitsCharacter`, 그리고 캐릭터별 실루엣에 맞춘 `DefaultPlacement` 계산식을 추가하여 정면 뷰(늘보군)와 측면 뷰(펠리)의 부착 위치를 자동 최적화했다. | `CostumeTests.CharacterSpecificSuitabilityAndDefaultPlacement`, `CostumeViewModel.ImportAsync` 연동. |
| UI/UX 정리정돈 | `MainWindow.xaml`의 캐릭터 탭에 캐릭터 소개, 모션 특징, 추천 의상 안내 배너를 배치하고, 미리보기 영역에 캐릭터 상태 및 반응성을 시각화했다. | WPF XAML 바인딩 확인, 레이아웃 일관성 및 접근성 명칭 보존. |
| 테스트 & 미리보기 | 별도 런타임/서버 없이 브라우저에서 직접 캐릭터 모션, 의상, 표정을 인터랙티브하게 시험할 수 있는 `harness/preview/index.html`과 윈도우 원클릭 실행 도구 `TEST-MOTION-PREVIEW.cmd`를 구축했다. | 순수 HTML5/CSS/Canvas 독립 실행 및 `package_windows.py` 배포 번들 포함. |

## Residual Risk

미해결 필수 수정 사항은 없다. 실제 Windows 실기 환경에서의 GPU 가속 및 물리 마이크 입력 검증은 `docs/quality/windows-checklist.md`에 명시된 물리 절차를 따른다. 본 작업은 오디오 엔진 분리 및 음성 비저장/로컬 처리 원칙을 철저히 준수하였다.

## Follow-Ups

1. Windows 환경에서 `harness/windows-test/TEST-MOTION-PREVIEW.cmd`를 더블클릭하여 브라우저에서 캐릭터 모션과 의상을 시각적으로 확인한다.
2. 실기 환경에서 `START.cmd`를 실행하여 통합 StudioShell 및 MainWindow 설정창에서 캐릭터 전환과 의상 배치를 확인한다.
3. 문서 및 리뷰 미러 동기화 후 표준 Git 절차에 따라 main 병합 및 브랜치 정리를 수행한다.
