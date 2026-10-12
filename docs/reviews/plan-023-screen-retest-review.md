# plan-023-screen-retest Review

## Summary

QA 이후 단일 에이전트 자체 리뷰. 서브에이전트나 독립 검토는 실행하지 않았다. 기존 전체 검사에225장 실제 Windows 데스크톱 캡처·직접 시각 검토를 추가하고 개인 자산을 외부 전송하지 않은 재검증 배포를 확인했다.

## QA

Core177/177, WPF1346·실패0, 일반/서명 EXE9사례씩,225장 desktop coverage/foreground/PNG CRC·직접 시각 검토, locked restore/format/Release/publish 경고0·오류0·문서/자산/diff 확인. Windows38255728224/OBS38255728219 c7d2f0f, 장시간 OBS38252444387 main1939f3c(동일 정상 앱 동작) 통과.10분 warmup 후60.05분 측정·RSS 증가-2.39MiB·핸들 증가-2·프레임 변화67회. ZIP32파일·CRC·기존16PNG 바이트 보존·EXE/CER hash/서명 확인.

## Findings

- 첫 screen fixture의 수동 CostumeItem 생성은 종료 시 WearCommand 미설정 오류를 발생시켰다. 실제 ImportAsync로 추가/착용하고 전체 수명·종료/전체 검사를 다시 통과했다.
- ScreenEvidence는 일회용 CI guard와 안전한 장면 이름을 확인하고 실제 desktop HWND 영역만 캡처한다. GDI/DC 자원을 finally 해제하고 Topmost도 복원한다. 실패한 픽셀의 PNG/메타데이터는 판정 전에 보존하며 synthetic renderer fallback이 없다.
- startup 화면은 AppPaths.QaFolder/RUNNER_TEMP와 명시 QA_SCREEN_NAME guard가 필요하다. 정상 사용자에게 화면 저장·음성 저장·추적·원격 전송을 추가하지 않는다. 기존 timer pause helper는 QA에서만 사용하고 종료 중 재시작하지 않는다.
- 기존 렌더러/설정·오디오 동작·개인 PNG·이전 배포를 보존했다.225장의 설정/표정/손·소매·머리/입 연결·재실행 화면에 누락/잘림을 발견하지 않았다.

## Residual Risk

공용 고양이와 합성 시트/의상만 Windows CI에서 표시했다. 개인 PNG는 사용자 요청으로 전송하지 않았으며 개인 캐릭터 Windows 실기/물리 마이크/USB 제거·권한·혼합 DPI·사용자 GPU 결과는 없다. desktop transparent PNG는 뒤 창이 합성된 화면이며 native alpha는 OBS WGC로 별도 확인했다. layered BitBlt 제한·자체 인증서 신뢰/SmartScreen 제한·큰 모션의 원본 PNG 변형 제한을 유지한다.

## Follow-Ups

대상 Windows PC에서 개인 캐릭터/의상 배치를 확인하고 실제 마이크·USB 제거·권한·혼합 DPI·사용자 GPU/OBS를 실기 검증한다.
