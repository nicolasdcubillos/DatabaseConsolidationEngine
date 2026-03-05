@echo off
setlocal
cd /d "%~dp0"

if exist publish (
    echo Eliminando carpeta publish anterior...
    rmdir /s /q publish
)

echo ========================================
echo Compilando proyecto (.NET build)...
echo ========================================

dotnet build
if errorlevel 1 (
    echo Falló la compilación. Revisa los errores arriba.
    exit /b 1
)

echo.
echo ========================================
echo Publicando proyecto (.NET publish)...
echo ========================================

dotnet publish -c Release -r win-x64 --self-contained true -o ./publish /p:EnvironmentName=Production
if errorlevel 1 (
    echo Falló el publish. Revisa los errores arriba.
    pause
    exit /b 1
)

echo.
echo Build y publish completado exitosamente.

echo Copiando dce-installer.bat a la carpeta de publicación...
copy /Y dce-installer.bat .\publish\

echo Copiando appsettings.Production.json como appsettings.json a la carpeta de publicación...
if exist appsettings.Production.json (
    copy /Y appsettings.Production.json .\publish\appsettings.json
    echo Archivo de configuración de producción copiado exitosamente.
) else (
    echo ADVERTENCIA: No se encontró appsettings.Production.json
)

echo Comprimendo la carpeta publish a ConsolidationEngine.rar...
if exist "C:\Program Files\WinRAR\WinRAR.exe" (
    "C:\Program Files\WinRAR\WinRAR.exe" a -r ConsolidationEngine.rar .\publish\
    echo Comprimido exitosamente.
) else (
    echo WinRAR no está instalado o no se encontró en la ruta esperada.
)

pause
exit /b 0
