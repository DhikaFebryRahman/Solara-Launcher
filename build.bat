@echo off
setlocal

cd /d "%~dp0"

echo Membersihkan output lama...
if exist "bin\Release\net8.0-windows\win-x64\publish" rd /s /q "bin\Release\net8.0-windows\win-x64\publish"

echo Melakukan publish standalone single-file...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

if errorlevel 1 (
    echo.
    echo Build GAGAL. Periksa pesan error di atas.
    pause
    exit /b 1
)

echo.
echo Build BERHASIL.
echo File exe standalone ada di:
echo   bin\Release\net8.0-windows\win-x64\publish\MC.exe
echo.
echo Aplikasi bersifat portable: folder .minecraft akan dibuat
echo di folder yang sama dengan MC.exe. Pindahkan seluruh isi
echo folder "publish" bersamaan agar tetap berfungsi.
pause
