@echo off
chcp 65001 >nul
echo ============================================
echo  部署 VpetAPI 新版本（无 Harmony）到游戏目录
echo ============================================
echo.

copy /Y "d:\DevelopmentFolder\C#\女鹅Mod\Vpet.Plugin.VpetAPI\1230_VpetAPIHttp\plugin\VPet.Plugin.VpetAPI.dll" "E:\steam\steamapps\common\VPet\mod\1230_VpetAPIHttp\plugin\"
if errorlevel 1 goto :fail
copy /Y "d:\DevelopmentFolder\C#\女鹅Mod\Vpet.Plugin.VpetAPI\1230_VpetAPIHttp\plugin\VPet.Plugin.VpetAPI.pdb" "E:\steam\steamapps\common\VPet\mod\1230_VpetAPIHttp\plugin\"
if errorlevel 1 goto :fail

echo 部署消费返利系统（撤销 UI 直接修改的回退版）...
copy /Y "d:\DevelopmentFolder\C#\女鹅Mod\Vpet.Plugin.消费返利系统\1152_消费返利系统\plugin\VPet.Plugin.消费返利系统.dll" "E:\steam\steamapps\common\VPet\mod\1152_消费返利系统\plugin\"
if errorlevel 1 goto :fail
copy /Y "d:\DevelopmentFolder\C#\女鹅Mod\Vpet.Plugin.消费返利系统\1152_消费返利系统\plugin\VPet.Plugin.消费返利系统.pdb" "E:\steam\steamapps\common\VPet\mod\1152_消费返利系统\plugin\"
if errorlevel 1 goto :fail

echo.
echo 部署后目录内容：
dir "E:\steam\steamapps\common\VPet\mod\1230_VpetAPIHttp\plugin"
dir "E:\steam\steamapps\common\VPet\mod\1152_消费返利系统\plugin"

echo.
echo 部署完成！现在可以启动桌宠了。
pause
exit /b 0

:fail
echo.
echo 部署失败：文件复制出错。请确认桌宠已完全退出后重试。
pause
exit /b 1
