# plan-021-character-selection Review

## Summary

단일 에이전트의 QA 후 자체 리뷰다. 사용자 요구 다섯 이름/순서·측면 펠리·두 창 공유·선택 저장·원본/슬롯/의상 보존을 검토했다. 별도 에이전트의 독립 리뷰는 수행하지 않았다.

## QA

macOS locked restore/format/Release build 경고0·오류0/Core160/160, verify_base/verify_app/diff --check 통과. 신규4RGBA 1536×1024와24셀의 투명/비어 있지 않음/셀 내부 여백·6고유 상태를 확인했고 기존12PNG byte-identical이다. image_gen 원본을 복사했으며 사용자 Downloads 원본을 변경하지 않았다.

수정본2f9bc4f의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38231988396)는 Core160/WPF908·실패0와 일반/자체 서명 EXE 각각8개 시작/종료 사례를 통과했다. 공개 CI에는 합성 fixture만 보냈다. 종료 경합 보완 bc20ca0의 [최종 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38232965708)는 Core160/WPF910·실패0, 일반/자체 서명 EXE 각각8개 사례와 지연 Stop 중 캐릭터 적용 차단을 통과했다. [최종 OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38232965718)는 WGC·크로마/투명 alpha·동일 HWND/소스 배경 왕복을 통과했다. BitBlt는 기존 layered 창 제한으로 실패를 별도 기록한다.

개인 ZIP32파일/CRC/SHA256 및 EXE·공개 인증서 해시를 확인했다. 기존12PNG와 신규4PNG를 원본 바이트 그대로 동봉했으며 개인키는 없다.

## Findings

- P2 해결: DisplayMemberPath만 사용한 새 ComboBox의 선택값/팝업이 전역 밝은 TextBlock 스타일을 사용했다. 초기 Windows CI의24가독성 검사 실패를 명시적 검정 ItemTemplate으로 해결했고908검사 통과로 확인했다.
- P2 해결: DisposeAsync가 오디오 해제를 먼저 기다리면 Stop 지연 동안 준비한 캐릭터가 적용될 수 있었다. 종료 진입에서 Expressions.CloseAsync를 즉시 시작하고 오디오 해제와 함께 기다리도록 변경했다. 합성 입력의 Stop을 제어한 selection_audio_stop_delay_cannot_apply_pending_character 검사 통과로 종료 도중 시트 적용 차단을 확인했다.
- 설정 ID는 고정 목록으로 정규화한다. 누락/손상 캐릭터는 기존 그림/선택을 유지하고 시작은 늘보군/내장 고양이 순서로 복구한다. ID 오류는 오디오/창 설정을 보존한다.
- 동일 gate/revision이 선택·PNG 불러오기·표정 IO·전체 테스트를 조정한다. 최신 요청만 적용하고 테스트의 오래된 모습 복원을 버린다. 이미지 가족은 worker에서 모두 decode/Freeze한 뒤 UI에서 적용한다.
- 음성 처리와 원격/녹음 API는 변경하지 않았다. private PNG/원본/생성 프롬프트는 ignored artifacts에 보관하며 Git/CI에서 제외했다. README와 진행 중인 plan020은 변경하지 않았다.

## Residual Risk

macOS에서는 WPF/WASAPI를 실행할 수 없다. CI의 실제 Selector/창·EXE 실행은 합성 수치/PNG이며 개인 그림과 물리 마이크·사용자 GPU/OBS 실기를 대체하지 않는다. 신규4종은 평상3입×2눈이며 별도12표정 얼굴 그림은 제공하지 않는다. 기존 의상과 눈물의 위치는 캐릭터 체형에 따라 사용자가 조정한다. 개인 자체 서명은 인증기관 신원 확인이나 SmartScreen 경고 제거를 의미하지 않는다.

## Follow-Ups

대상 Windows PC에서 다섯 실제 그림·입/눈·펠리 측면, 마이크와 OBS WGC 캡처를 확인한다.
