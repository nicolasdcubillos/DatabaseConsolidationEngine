@echo off
setlocal

cd /d "%~dp0"
set "SERVICE_NAME=ConsolidationEngine"
set "DISPLAY_NAME=ConsolidationEngine - CC Sistemas Windows Service"
set "EXE_PATH=%~dp0%ConsolidationEngine.exe"

if not exist "%EXE_PATH%" (
    echo [ERROR] No se encontró el ejecutable en: %EXE_PATH%
    set "EXE_PATH=%~dp0%\publish\ConsolidationEngine.exe"
)

if not exist "%EXE_PATH%" (
    echo [ERROR] No se encontró el ejecutable en: %EXE_PATH%
    pause
    exit /b 1
)

:MENU
cls
echo.
echo ========== Servicio: %SERVICE_NAME% ==========
echo 1. Crear servicio
echo 2. Iniciar servicio
echo 3. Detener servicio
echo 4. Eliminar servicio
echo 6. Ver estado del servicio
echo 5. Salir
echo ===============================================
set /p CHOICE="Opcion: "

if "%CHOICE%"=="1" (
    sc.exe create "%SERVICE_NAME%" binPath= "%EXE_PATH%" DisplayName= "%DISPLAY_NAME%"
    pause
    goto MENU
)
if "%CHOICE%"=="2" (
    sc.exe start "%SERVICE_NAME%"
    pause
    goto MENU
)
if "%CHOICE%"=="3" (
    sc.exe stop "%SERVICE_NAME%"
    pause
    goto MENU
)
if "%CHOICE%"=="4" (
    sc.exe delete "%SERVICE_NAME%"
    pause
    goto MENU
)
if "%CHOICE%"=="6" (
    sc.exe query "%SERVICE_NAME%"
    pause
    goto MENU
)
if "%CHOICE%"=="5" (
    echo Saliendo...
    timeout /t 1 >nul
    exit /b 0
)

echo Opción inválida.
pause
goto MENU
