@echo off
REM =========================
REM CloudScript 자동 병합 스크립트
REM =========================

REM 최종 CloudScript 파일 이름
set OUTPUT_FILE=CloudScript.js

REM 기존 CloudScript 파일이 있으면 삭제
if exist %OUTPUT_FILE% del %OUTPUT_FILE%

REM 자동 생성 주석 추가
echo // Auto-generated CloudScript >> %OUTPUT_FILE%

REM handlers 폴더 안 JS 파일을 합치기 (순서 중요)
type handlers\GoldHandler.js>> %OUTPUT_FILE%
type handlers\TicketHandler.js >> %OUTPUT_FILE%
type handlers\DrawBulletHandler.js >> %OUTPUT_FILE%
type handlers\StatHandler.js >> %OUTPUT_FILE%
type handlers\SingleDataHandler.js >> %OUTPUT_FILE%
type handlers\WeaponHandler.js >> %OUTPUT_FILE%
type handlers\GadgetHandler.js >> %OUTPUT_FILE%
type handlers\ChatHandler.js >> %OUTPUT_FILE%

echo 합치기 완료! 파일: %OUTPUT_FILE%
pause
