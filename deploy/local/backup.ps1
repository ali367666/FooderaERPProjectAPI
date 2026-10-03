# Foodera database backup.
#   .\backup.ps1            -> one backup now into .\backups (keeps the last 14)
#   .\backup.ps1 -Schedule  -> registers a daily Windows task at 03:00 that runs this script
param([switch]$Schedule)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if ($Schedule) {
    $action = New-ScheduledTaskAction -Execute "powershell.exe" `
        -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    $trigger = New-ScheduledTaskTrigger -Daily -At 3am
    Register-ScheduledTask -TaskName "Foodera DB backup" -Action $action -Trigger $trigger -Force | Out-Null
    Write-Host "Gündəlik ehtiyat nüsxə hər gün saat 03:00 üçün quruldu." -ForegroundColor Green
    exit 0
}

$password = (Get-Content ".env" | Where-Object { $_ -match "^DB_PASSWORD=" }) -replace "^DB_PASSWORD=", ""
$stamp = Get-Date -Format "yyyyMMdd-HHmm"
$file = "/var/opt/mssql/backups/FooderaERP-$stamp.bak"

docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$password" `
    -Q "BACKUP DATABASE [FooderaERP] TO DISK = N'$file' WITH INIT, COMPRESSION"
if ($LASTEXITCODE -ne 0) { Write-Host "Ehtiyat nüsxə alınmadı." -ForegroundColor Red; exit 1 }

# Keep the newest 14 backups.
Get-ChildItem "backups" -Filter "FooderaERP-*.bak" | Sort-Object LastWriteTime -Descending |
    Select-Object -Skip 14 | Remove-Item -Force

Write-Host "Ehtiyat nüsxə: backups\FooderaERP-$stamp.bak" -ForegroundColor Green
