#write-host "import ""$($PSScriptRoot)\settings.reg"" /reg:32"
Start-Process "reg.exe" -Verb Runas -ArgumentList "import ""$($PSScriptRoot)\settings.reg"" /reg:32"
#[void][System.Console]::ReadKey($true)