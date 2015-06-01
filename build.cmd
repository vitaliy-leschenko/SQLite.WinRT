set msbuild=C:\Program Files (x86)\MSBuild\14.0\bin

"%msbuild%\msbuild.exe" /p:Configuration=Release /p:Platform=x86 /t:Rebuild
"%msbuild%\msbuild.exe" /p:Configuration=Release /p:Platform=x64 /t:Rebuild
"%msbuild%\msbuild.exe" /p:Configuration=Release /p:Platform=ARM /t:Rebuild
