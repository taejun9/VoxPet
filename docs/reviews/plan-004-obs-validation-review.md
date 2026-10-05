# plan-004-obs-validation Review

## Summary

QA 이후 단일 에이전트 자체 리뷰. 마이크 없는 데모 fixture, 공식 portable OBS/RPC 하네스, 방송창 grip 표시와 기존 수명 회귀를 검토했다.

## QA

Windows Core70/build/publish/native WPF smoke: [86374ad](https://github.com/taejun9/VoxPet/actions/runs/37276105532) 통과.
OBS32.2.2/WS5.7.4, Server2025 build26100/Microsoft Basic Render Driver/D3D11: [green/transparent](https://github.com/taejun9/VoxPet/actions/runs/37276105483) WGC 통과. native alpha65.1%/7프레임변화, chroma 후green0%/캐릭터 유지.
[d35f707 장시간](https://github.com/taejun9/VoxPet/actions/runs/37269646209): warmup10분+실제측정60.17분, RSS+1.58MiB/handles+0/68회 움직임 검사, 최종 유효frame. 문서/자산/diff 검사 통과.

## Findings

- OBS raw PNG에 resize grip 표시가 보였다. opacity0으로 native resize 영역을 유지하며 표시를 제거했고 WM_NCHITTEST=HTBOTTOMRIGHT 및 raw/keyed corner 검사 통과.
- 단계 사이 제거한 OBS input 이름을 즉시 재사용하면서 CreateInput601이 발생했다. 단계별 이름을 분리하여 실제 transparent 캡처를 검증했다.
- 생산 앱에 networking/녹음 기능이 추가되지 않았다. fixture는 개인설정을 읽거나 저장하지 않고 숫자데모만 사용한다. OBS는 일회용runner에 한정되며 오디오source/녹화/방송이 없음을 조회했다.
- 원래 README 보존, 테스트 버전/환경/한계를 명시. 남은 하드웨어 검증을 자동 검사로 축소하지 않았다.

## Residual Risk

BitBlt는 이 환경에서 유효한 프레임이 없었다. 실제 Windows11 마이크/권한/USB제거/사용자GPU/물리DPI/실제입력장시간은 미검증이다. native alpha 성공은 해당 CI 환경의 WGC에 한정한다.

## Follow-Ups

plan005 작은 창/plan006 Stop오류 수정을 main 결과와 통합하여 누적 QA한다. 실제 마이크가 있는 Windows 환경의 체크리스트 결과가 필요하다. 전체 사용자 목표는 아직 미완료다.
