# Foodera Local installation — run in PowerShell from this folder: .\install.ps1
# Needs Docker Desktop. Builds and starts the database, file storage, API and web app,
# and makes them start again automatically after a reboot.

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker tapılmadı. Əvvəlcə Docker Desktop quraşdırın: https://www.docker.com/products/docker-desktop/" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host ".env faylı yaradıldı. Onu açıb doldurun (IP, şifrələr, lisenziya açarı), sonra skripti yenidən işə salın." -ForegroundColor Yellow
    notepad ".env"
    exit 0
}

New-Item -ItemType Directory -Force -Path "backups" | Out-Null

Write-Host "Foodera qurulur (ilk dəfə bir neçə dəqiqə çəkə bilər)..." -ForegroundColor Cyan
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { Write-Host "Quraşdırma alınmadı." -ForegroundColor Red; exit 1 }

$ip = (Get-Content ".env" | Where-Object { $_ -match "^SERVER_IP=" }) -replace "^SERVER_IP=", ""
Write-Host ""
Write-Host "Hazırdır." -ForegroundColor Green
Write-Host "  Bu kompüterdə:      http://localhost:3000"
Write-Host "  Terminallardan:     http://$($ip):3000"
Write-Host ""
Write-Host "Növbəti addımlar:"
Write-Host "  1. SuperAdmin ilə daxil olun, Şirkətlər bölməsində restoranın şirkətini yaradın"
Write-Host "     (Company Code mərkəzi serverdəki ilə EYNİ olmalıdır)."
Write-Host "  2. Platformadan aldığınız lisenziya açarını daxil edin (http://localhost:3000/license)."
Write-Host "  3. Gündəlik ehtiyat nüsxə üçün: .\backup.ps1 -Schedule"
