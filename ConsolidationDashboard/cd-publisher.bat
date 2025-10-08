@echo off
setlocal
cd /d "%~dp0"

REM Carpeta donde se publicará el proyecto
set PUBLISH_DIR=publish

REM Eliminar carpeta de publicación anterior
if exist %PUBLISH_DIR% (
    echo Eliminando carpeta publish anterior...
    rmdir /s /q %PUBLISH_DIR%
)

echo ========================================
echo Compilando proyecto (.NET build)...
echo ========================================

REM Build del proyecto en Release
dotnet build -c Release
if errorlevel 1 (
    echo Falló la compilación. Revisa los errores arriba.
    exit /b 1
)

echo.
echo ========================================
echo Publicando proyecto (.NET publish)...
echo ========================================

REM Publicar como self-contained para Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -o %PUBLISH_DIR%
if errorlevel 1 (
    echo Falló el publish. Revisa los errores arriba.
    pause
    exit /b 1
)

echo.
echo Build y publish completado exitosamente.
echo Ejecutable generado en: %PUBLISH_DIR%

REM Copiar cd-installer.bat a la carpeta de publicación
if exist cd-installer.bat (
    echo Copiando cd-installer.bat a la carpeta de publicación...
    copy /Y cd-installer.bat %PUBLISH_DIR%\
)

REM Comprimir la carpeta publish en publish.rar si WinRAR existe
set WINRAR_PATH="C:\Program Files\WinRAR\WinRAR.exe"
if exist %WINRAR_PATH% (
    echo Comprimendo la carpeta publish a publish.rar...
    %WINRAR_PATH% a -r publish.rar %PUBLISH_DIR%\
    echo Comprimado exitosamente.
) else (
    echo WinRAR no está instalado o no se encontró en la ruta esperada.
)

pause
exit /b 0
