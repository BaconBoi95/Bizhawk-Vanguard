@echo off
:start

echo.
echo  ----------------
echo   RTC KILL SWITCH
echo  ----------------
echo.
echo  Press any key to activate
pause > nul

taskkill /F /IM StandaloneRTC.exe > nul 2>&1
taskkill /F /IM WerFault.exe > nul 2>&1
taskkill /F /IM EmuHawk.exe > nul
taskkill /F /IM libsneshawk-32-compatibility.exe > nul
taskkill /F /IM libsneshawk-32-performance.exe > nul
taskkill /F /IM libsneshawk-64-compatibility.exe > nul
taskkill /F /IM libsneshawk-64-performance.exe > nul
taskkill /F /IM ffmpeg.exe > nul

goto start
exit