@echo off
setlocal
echo ========================================================
echo  VoxPet · 이모지 자연 모션 및 캐릭터 미리보기
echo ========================================================
echo.
echo 브라우저에서 인터랙티브 모션 미리보기를 엽니다...
echo (늘보군 / 펠리 호흡, 바운스, 의상, 표정 12종 실시간 테스트)
echo.

set "PREVIEW_HTML=%~dp0..\preview\index.html"
if exist "%PREVIEW_HTML%" (
    start "" "%PREVIEW_HTML%"
) else (
    if exist "%~dp0harness\preview\index.html" (
        start "" "%~dp0harness\preview\index.html"
    ) else (
        echo [안내] 미리보기 파일을 찾을 수 없습니다: %PREVIEW_HTML%
    )
)

echo.
echo 앱 실행을 원하시면 START.cmd를 실행하세요.
echo 합성 자동 검증을 원하시면 TEST-SMOKE.cmd를 실행하세요.
echo ========================================================
endlocal
