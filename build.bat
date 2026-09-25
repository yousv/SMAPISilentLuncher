@echo off
dotnet publish -c Release -r win-x64
if %ERRORLEVEL% EQU 0 (
    copy /Y "bin\Release\net8.0\win-x64\publish\SMAPI Silent Launcher.exe" "SMAPI Silent Launcher.exe" >nul
    copy /Y "bin\Release\net8.0\win-x64\publish\SMAPI Silent Launcher.exe" "D:\Steam\steamapps\common\Stardew Valley\SMAPI Silent Launcher.exe" >nul
    echo Built: SMAPI Silent Launcher.exe
) else (
    echo Build failed.
    exit /b 1
)
