# Runs the PlayMode playtest bot headlessly against a scratch copy of the project,
# so it works even while the project is open in the Editor.
$unity = "C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe"
$src = Split-Path $PSScriptRoot -Parent
$dst = "$env:LOCALAPPDATA\Temp\ttplay"

foreach ($d in "Assets", "Packages", "ProjectSettings") {
    robocopy "$src\$d" "$dst\$d" /E /PURGE /NFL /NDL /NJH /NJS /NP | Out-Null
}
if (-not (Test-Path "$dst\Library\PackageCache")) {
    robocopy "$src\Library\PackageCache" "$dst\Library\PackageCache" /E /NFL /NDL /NJH /NJS /NP | Out-Null
}
foreach ($f in "$dst\results.xml", "$dst\log.txt") { if (Test-Path $f) { Remove-Item $f } }
if (Test-Path "$dst\PlaytestScreenshots") { Remove-Item "$dst\PlaytestScreenshots" -Recurse }

$p = Start-Process $unity -ArgumentList @("-batchmode", "-projectPath", "`"$dst`"", "-runTests", "-testPlatform", "PlayMode",
    "-testResults", "`"$dst\results.xml`"", "-logFile", "`"$dst\log.txt`"") -PassThru
if (-not $p.WaitForExit(900000)) { Stop-Process -Id $p.Id -Force; Write-Output "TIMED OUT"; exit 1 }

Select-String -Path "$dst\results.xml" -Pattern '<test-run|<test-case|<message' | ForEach-Object { $_.Line.Trim() }
Write-Output "Screenshots: $dst\PlaytestScreenshots"
exit $p.ExitCode
