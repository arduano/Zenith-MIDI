del /S /F /Q build\*
rmdir /s /q build
mkdir build

call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat" 
rmdir /s /q bin\x64\Release
MSBuild.exe Zenith.sln /t:Zenith /p:Configuration=Release /p:Platform=x64
rmdir /s /q bin\x86\Release
MSBuild.exe Zenith.sln /t:Zenith /p:Configuration=Release /p:Platform=x86
MSBuild.exe Zenith.sln /t:ZenithInstaller /p:Configuration=Release

rmdir "bin\x64\Release\debug" /s /q
rmdir "bin\x64\Release\Plugins\Assets" /s /q
forfiles /p "bin\x64\Release\Plugins" /c "cmd /c del ..\lib\@file" 
del bin\x64\Release\.lib\*Render.dll
rmdir "bin\x86\Release\debug" /s /q
rmdir "bin\x86\Release\Plugins\Assets" /s /q
forfiles /p "bin\x86\Release\Plugins" /c "cmd /c del ..\lib\@file" 
del bin\x86\Release\.lib\*Render.dll
powershell -c Compress-Archive -Path "bin\x64\Release\*" -CompressionLevel Optimal -Force -DestinationPath build\Zenithx64.zip
powershell -c Compress-Archive -Path "bin\x86\Release\*" -CompressionLevel Optimal -Force -DestinationPath build\Zenithx86.zip
copy ZenithInstaller\bin\Release\ZenithInstaller.exe build\ZenithInstaller.exe