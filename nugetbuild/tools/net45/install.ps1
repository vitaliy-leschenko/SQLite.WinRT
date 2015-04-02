param($installPath, $toolsPath, $package, $project)

Write-Host
Write-Host "Please make sure that:"
Write-Host "- you call global::NativeDllHelper.SetDllDirectory() at startup." -foregroundcolor Blue
Write-Host "- property 'Copy to Output Directory' of x86\sqlite3.dll is setted to 'Copy if newer'." -foregroundcolor Blue
Write-Host "- property 'Copy to Output Directory' of x64\sqlite3.dll is setted to 'Copy if newer'." -foregroundcolor Blue

