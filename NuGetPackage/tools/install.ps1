param($installPath, $toolsPath, $package, $project)

Write-Host
Write-Host "Please make sure that target platform is set to x86, x64 or ARM."
Write-Host "Add reference to SDK:"
Write-Host "[Windows Phone Silverlight]: 'SQLite for Windows Phone'"
Write-Host "[Windows Store Apps]: 'SQLite for Windows Runtime (Windows 8.1)' and 'Microsoft Visual C++ 2013 Runtime Package for Windows'"
Write-Host "[Windows Phone 8.1 (WinRT)]: 'SQLite for Windows Phone 8.1' and 'Microsoft Visual C++ 2013 Runtime Package for Windows Phone'"
