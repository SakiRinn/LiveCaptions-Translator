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

:: 创建发布目录
set RELEASE_DIR=release_v%VERSION%
if exist "%RELEASE_DIR%" rd /s /q "%RELEASE_DIR%"
mkdir "%RELEASE_DIR%"

:: 发布不带运行时版本（更小的文件体积）
echo Publishing without runtime...
set NO_RUNTIME_DIR=%RELEASE_DIR%\no-runtime
mkdir "%NO_RUNTIME_DIR%"
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:VersionPrefix=%VERSION% -o "%NO_RUNTIME_DIR%"
if errorlevel 1 (
    echo Error: Publish without runtime failed
    exit /b 1
)

:: 发布包含运行时版本（独立运行）
echo Publishing with runtime...
set WITH_RUNTIME_DIR=%RELEASE_DIR%\with-runtime
mkdir "%WITH_RUNTIME_DIR%"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:VersionPrefix=%VERSION% -o "%WITH_RUNTIME_DIR%"
if errorlevel 1 (
    echo Error: Publish with runtime failed
    exit /b 1
)

:: 复制额外文件到两个目录
echo Copying additional files...
copy "README.md" "%NO_RUNTIME_DIR%\"
copy "LICENSE.txt" "%NO_RUNTIME_DIR%\"
copy "README.md" "%WITH_RUNTIME_DIR%\"
copy "LICENSE.txt" "%WITH_RUNTIME_DIR%\"

:: 创建压缩文件
echo Creating ZIP files...
powershell Compress-Archive -Path "%NO_RUNTIME_DIR%\*" -DestinationPath "%RELEASE_DIR%\LiveCaptionsTranslator_v%VERSION%_no_runtime.zip" -Force
powershell Compress-Archive -Path "%WITH_RUNTIME_DIR%\*" -DestinationPath "%RELEASE_DIR%\LiveCaptionsTranslator_v%VERSION%_with_runtime.zip" -Force

echo.
echo Build completed successfully!
echo Release files are in: %RELEASE_DIR%
echo Created packages:
echo  - LiveCaptionsTranslator_v%VERSION%_no_runtime.zip (不包含运行时)
echo  - LiveCaptionsTranslator_v%VERSION%_with_runtime.zip (包含运行时)
echo.

pause