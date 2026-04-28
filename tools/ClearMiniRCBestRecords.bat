@echo off
chcp 65001 >nul 2>&1
title MiniRC 最佳成绩清理工具

echo.
echo ================================================
echo        MiniRC 最佳成绩存档清理工具
echo ================================================
echo.
echo  此工具会删除你在 MiniRC 里保存的最佳圈速记录
echo  （注册表中 BeiBeiTeam\MiniRC 下的 PlayerPrefs）
echo.
echo  操作不可撤销！
echo.
echo  按任意键开始清理，或直接关闭窗口取消...
echo ------------------------------------------------
pause >nul

echo.
echo 正在清理存档...
echo.

set "CLEARED=0"

reg delete "HKCU\Software\BeiBeiTeam\MiniRC" /f >nul 2>&1
if %ERRORLEVEL%==0 (
    echo   [OK]   已清除游戏存档
    set "CLEARED=1"
) else (
    echo   [跳过] 没有发现游戏存档
)

reg delete "HKCU\Software\Unity\UnityEditor\BeiBeiTeam\MiniRC" /f >nul 2>&1
if %ERRORLEVEL%==0 (
    echo   [OK]   已清除编辑器存档
    set "CLEARED=1"
) else (
    echo   [跳过] 没有发现编辑器存档
)

echo.
echo ------------------------------------------------
if "%CLEARED%"=="1" (
    echo  清理完成，重新打开游戏即为初始状态。
) else (
    echo  没有找到任何存档，可能本来就是干净的。
)
echo ------------------------------------------------
echo.
echo 按任意键关闭窗口...
pause >nul
