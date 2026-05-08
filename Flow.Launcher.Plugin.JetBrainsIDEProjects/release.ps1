dotnet publish -c Release -r win-x64 --no-self-contained
$AppDataFolder = "$env:APPDATA"
$flowLauncherExe = "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"
if (Test-Path $flowLauncherExe) {
    Write-Host "Stopping FlowLauncher..."
    Stop-Process -Name "Flow.Launcher" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    Write-Host "Deleting old binaries of $project..."
    $path = (Get-Item -Path .).Name
    $project = $path -replace '(.*Plugin\.)(.*)', '$2'
    $dest = "$AppDataFolder\FlowLauncher\Plugins\$project"
    if (Test-Path $dest) {
        Remove-Item -Recurse -Force $dest
    }
    Write-Host "Copy new binaries of $project..."
    $bin = "bin\Release\win-x64\publish"
    Copy-Item $bin "$AppDataFolder\FlowLauncher\Plugins\" -Recurse -Force
    Rename-Item -Path "$AppDataFolder\FlowLauncher\Plugins\publish" -NewName "$project"

    Write-Host "Starting FlowLauncher..."

    Start-Process $flowLauncherExe
} else {
    Write-Host "Flow.Launcher.exe not found. Please install Flow Launcher first"
}
Start-Sleep -Seconds 2