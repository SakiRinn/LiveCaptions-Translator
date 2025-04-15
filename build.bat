@echo off
setlocal enabledelayedexpansion

echo ===== LiveCaptionsTranslator Build Script =====

:: 设置版本号（可以根据需要修改）
set VERSION=1.0.0

:: 清理旧的构建文件
echo Cleaning old build files...
dotnet clean
if errorlevel 1 (
    echo Error: Clean failed
    exit /b 1
)

:: 还原NuGet包
echo Restoring packages...
dotnet restore
if errorlevel 1 (
    echo Error: Restore failed
    exit /b 1
)

:: Debug构建（测试用）
echo Building Debug version...
dotnet build -c Debug
if errorlevel 1 (
    echo Error: Debug build failed
    exit /b 1
)

:: Release构建
echo Building Release version...
dotnet build -c Release
if errorlevel 1 (
    echo Error: Release build failed
    exit /b 1
)

:: 发布（单文件模式）
echo Publishing Release version...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:VersionPrefix=%VERSION%
if errorlevel 1 (
    echo Error: Publish failed
    exit /b 1
)

:: 创建发布目录
set RELEASE_DIR=release_v%VERSION%
if not exist "%RELEASE_DIR%" mkdir "%RELEASE_DIR%"

:: 复制发布文件
echo Copying files to release directory...
xcopy /Y /E "bin\Release\net8.0-windows\win-x64\publish\*" "%RELEASE_DIR%\"

:: 复制额外文件（如果需要）
copy "README.md" "%RELEASE_DIR%\"
copy "LICENSE.txt" "%RELEASE_DIR%\"

echo.
echo Build completed successfully!
echo Release files are in: %RELEASE_DIR%
echo.

pause