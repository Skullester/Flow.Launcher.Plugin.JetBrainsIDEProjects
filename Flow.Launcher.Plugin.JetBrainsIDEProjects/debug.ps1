dotnet build
$flowLauncherExe = "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"
if (Test-Path $flowLauncherExe) {
    Write-Host "Stopping FlowLauncher..."
    Stop-Process -Name "Flow.Launcher" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    $path = (Get-Item -Path .).Name
    $project = $path -replace '(.*Plugin\.)(.*)', '$2'
    $bin = '.\bin\Debug'
    $dest = "$env:APPDATA\FlowLauncher\Plugins\$project"
    $files = @(
	    "Flow.Launcher.Plugin.$project.deps.json",
	    "Flow.Launcher.Plugin.$project.dll",
	    'plugin.json',
	    'icon.png')

    Set-Location $bin
    mkdir $dest -Force -ErrorAction Ignore | Out-Null
    Copy-Item $files $dest -Force -Recurse
    
    & $flowLauncherExe
}
else
{
    Write-Host "Flow.Launcher.exe not found. Please install Flow Launcher first"
}
Start-Sleep -Seconds 2