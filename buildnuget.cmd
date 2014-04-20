xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.dll     NuGetPackage\lib\netcore45
xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.Ext.dll NuGetPackage\lib\netcore45
xcopy /y SQLite.WinRT.Windows81\bin\ARM\Release\SQLite.WinRT.Ext.pri NuGetPackage\lib\netcore45

xcopy /y SQLite.WinRT\bin\Release\SQLite.WinRT.dll NuGetPackage\lib\portable-net45+win+wpa81+wp8

xcopy /y SQLite.WinRT.WindowsPhone8\bin\ARM\Release\SQLite.WinRT.dll     NuGetPackage\lib\wp8
xcopy /y SQLite.WinRT.WindowsPhone8\bin\ARM\Release\SQLite.WinRT.Ext.dll NuGetPackage\lib\wp8
xcopy /y Sqlite.WP80\bin\ARM\Release\Sqlite.WP80.winmd                   NuGetPackage\lib\wp8

xcopy /y SQLite.WinRT.WindowsPhone81\bin\ARM\Release\SQLite.WinRT.dll     NuGetPackage\lib\wpa81
xcopy /y SQLite.WinRT.WindowsPhone81\bin\ARM\Release\SQLite.WinRT.Ext.dll NuGetPackage\lib\wpa81
xcopy /y SQLite.WP81\bin\ARM\Release\Sqlite.WP81.winmd                    NuGetPackage\lib\wpa81

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
xcopy /y Sqlite.WP80\bin\ARM\Release\Sqlite.WP80.winmd                   NuGetPackage\build\wp8\ARM
xcopy /y Sqlite.WP80\bin\ARM\Release\Sqlite.WP80.dll                     NuGetPackage\build\wp8\ARM

xcopy /y SQLite.WinRT.WindowsPhone8\bin\x86\Release\SQLite.WinRT.dll     NuGetPackage\build\wp8\x86
xcopy /y SQLite.WinRT.WindowsPhone8\bin\x86\Release\SQLite.WinRT.Ext.dll NuGetPackage\build\wp8\x86
xcopy /y Sqlite.WP80\bin\Win32\Release\Sqlite.WP80.winmd                 NuGetPackage\build\wp8\x86
xcopy /y Sqlite.WP80\bin\Win32\Release\Sqlite.WP80.dll                   NuGetPackage\build\wp8\x86

xcopy /y SQLite.WinRT.WindowsPhone81\bin\ARM\Release\SQLite.WinRT.dll     NuGetPackage\build\wpa81\ARM
xcopy /y SQLite.WinRT.WindowsPhone81\bin\ARM\Release\SQLite.WinRT.Ext.dll NuGetPackage\build\wpa81\ARM
xcopy /y SQLite.WP81\bin\ARM\Release\Sqlite.WP81.winmd                    NuGetPackage\build\wpa81\ARM
xcopy /y SQLite.WP81\bin\ARM\Release\Sqlite.WP81.dll                      NuGetPackage\build\wpa81\ARM

xcopy /y SQLite.WinRT.WindowsPhone81\bin\x86\Release\SQLite.WinRT.dll     NuGetPackage\build\wpa81\x86
xcopy /y SQLite.WinRT.WindowsPhone81\bin\x86\Release\SQLite.WinRT.Ext.dll NuGetPackage\build\wpa81\x86
xcopy /y SQLite.WP81\bin\Win32\Release\Sqlite.WP81.winmd                  NuGetPackage\build\wpa81\x86
xcopy /y SQLite.WP81\bin\Win32\Release\Sqlite.WP81.dll                    NuGetPackage\build\wpa81\x86

.nuget\NuGet.exe pack NuGetPackage\Package.nuspec