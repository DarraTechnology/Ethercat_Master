@echo off
chcp 65001 >nul
title Darra EtherCAT 状态恢复
echo ============================================================
echo   Darra EtherCAT Master - 状态恢复程序 (松下 A6B 伺服)
echo ============================================================
echo.

:: 检查是否安装了 .NET SDK
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未安装 .NET SDK
    echo 请从 https://dotnet.microsoft.com/download 下载安装
    pause
    exit /b 1
)

echo 正在编译并运行...
echo.
dotnet run --project "%~dp0EtherCATRestore.csproj"
echo.
pause
