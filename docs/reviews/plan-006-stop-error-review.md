# plan-006-stop-error Review

## Summary

QA 이후 단일 에이전트 자체 리뷰. native Stop 중 Ended 오류가 정상 종료로 숨겨지는 문제와 plan004/005 통합 회귀를 검토했다.

## QA

수정 전 targeted 회귀는 Expected Faulted / Actual Stopped로 실패했다. 수정 후 macOS Release build 경고·오류0/Core71 통과.
통합4ad3180 [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37278367856): Core71/71, build/publish/UI smoke 실패0, native 방송창 수명/resize/4크기×15controls/3startup bounds/wheel 통과.
[OBS CI](https://github.com/taejun9/VoxPet/actions/runs/37278367918): green+Chroma Key/native transparent alpha와7프레임 변화 통과. JSON/PNG 직접 확인. BitBlt는 이 runner에서 유효 프레임을 얻지 못했다.
문서/자산/diff 검사 통과. 배포본은 native Windows artifact이며 PE AMD64/필수 배포 파일/ZIP CRC/SHA256 확인.

## Findings

- Cleanup 이전의 Ended만 확인하던 StopAsync가 worker의 정리 대기 중 발생한 native 오류를 놓쳤다. 정리 완료 이후 해당 run의 오류를 읽도록 수정했다.
- 회귀는 오류 안내/Faulted, 자원 없음/무음 및 새 Start의 Running/Error=null 복구를 함께 검사한다. 기존 정상 종료와 timeout 재시도도 유지했다.
- callback은 worker completion 전에 전달되며 Stop/Dispose 이후 settled 결과를 읽는다. UI 스레드 대기나 음성 저장·전송을 추가하지 않았다.
- 이미 push한 브랜치에 main을 정상 merge하여 plan004/005의 방송창·OBS·반응형 UI 검증을 누적했다. 원래 README를 유지했다.

## Residual Risk

Core fake는 실제 마이크 점유 해제 증거가 아니다. 현재 호스트는 macOS이며 GitHub Windows runner에는 물리 마이크가 없다.
Windows11 권한/USB 제거/실제 장치100회 시작종료/DPI 전환/실제 입력60분 및 사용자 GPU·OBS 조합은 미검증이다. 합성60.17분 자원 결과는 plan004의 d35f707 버전에 한정한다.

## Follow-Ups

검증된 portable ZIP을 사용해 마이크가 있는 Windows11 환경에서 windows-checklist.md를 수행하고 실패 재현 조건을 제공받는다. 전체 목표는 실기 검증까지 완료로 표시하지 않는다.
