set msbuild=C:\Program Files (x86)\MSBuild\12.0\bin

rem "%msbuild%\msbuild.exe" /p:Configuration=Release /p:Platform=x86 /t:Rebuild
rem "%msbuild%\msbuild.exe" /p:Configuration=Release /p:Platform=x64 /t:Rebuild
rem "%msbuild%\msbuild.exe" /p:Configuration=Release /p:Platform=ARM /t:Rebuild

xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.dll     NuGetPackage\lib\netcore45
xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.Ext.dll NuGetPackage\lib\netcore45
xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.Ext.pri NuGetPackage\lib\netcore45

xcopy /y SQLite.WinRT\bin\Release\SQLite.WinRT.dll NuGetPackage\lib\portable-wp8+win8

xcopy /y SQLite.WinRT.WindowsPhone8\bin\ARM\Release\SQLite.WinRT.dll     NuGetPackage\lib\wp8
xcopy /y SQLite.WinRT.WindowsPhone8\bin\ARM\Release\SQLite.WinRT.Ext.dll NuGetPackage\lib\wp8
xcopy /y SQLite.WinRT.WindowsPhone8\bin\ARM\Release\Sqlite.winmd         NuGetPackage\lib\wp8

xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.dll     NuGetPackage\build\netcore45\ARM
xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.Ext.dll NuGetPackage\build\netcore45\ARM
xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.Ext.pri NuGetPackage\build\netcore45\ARM

xcopy /y SQLite.WinRT.Windows81\bin\x64\Release\SQLite.WinRT.dll     NuGetPackage\build\netcore45\x64
xcopy /y SQLite.WinRT.Windows81\bin\x64\Release\SQLite.WinRT.Ext.dll NuGetPackage\build\netcore45\x64
xcopy /y SQLite.WinRT.Windows81\bin\x64\Release\SQLite.WinRT.Ext.pri NuGetPackage\build\netcore45\x64

xcopy /y SQLite.WinRT.Windows81\bin\x86\Release\SQLite.WinRT.dll     NuGetPackage\build\netcore45\x86
xcopy /y SQLite.WinRT.Windows81\bin\x86\Release\SQLite.WinRT.Ext.dll NuGetPackage\build\netcore45\x86
xcopy /y SQLite.WinRT.Windows81\bin\x86\Release\SQLite.WinRT.Ext.pri NuGetPackage\build\netcore45\x86

xcopy /y SQLite.WinRT.WindowsPhone8\bin\ARM\Release\SQLite.WinRT.dll     NuGetPackage\build\wp8\ARM
xcopy /y SQLite.WinRT.WindowsPhone8\bin\ARM\Release\SQLite.WinRT.Ext.dll NuGetPackage\build\wp8\ARM
xcopy /y Sqlite\bin\ARM\Release\Sqlite.winmd                             NuGetPackage\build\wp8\ARM
xcopy /y Sqlite\bin\ARM\Release\Sqlite.dll                               NuGetPackage\build\wp8\ARM

xcopy /y SQLite.WinRT.WindowsPhone8\bin\x86\Release\SQLite.WinRT.dll     NuGetPackage\build\wp8\x86
xcopy /y SQLite.WinRT.WindowsPhone8\bin\x86\Release\SQLite.WinRT.Ext.dll NuGetPackage\build\wp8\x86
xcopy /y Sqlite\bin\Win32\Release\Sqlite.winmd                           NuGetPackage\build\wp8\x86
xcopy /y Sqlite\bin\Win32\Release\Sqlite.dll                             NuGetPackage\build\wp8\x86

.nuget\NuGet.exe pack NuGetPackage\Package.nuspec