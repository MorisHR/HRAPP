# HRMS Device Sync - Windows Service Deployment Script
# Run this script as Administrator

param(
    [string]$ServiceName = "HRMS Device Sync Service",
    [string]$BinaryPath = "C:\HRMS\DeviceSync\HRMS.DeviceSync.exe",
    [string]$Description = "Synchronizes biometric attendance data from ZKTeco devices to HRMS API",
    [ValidateSet("Install", "Uninstall", "Start", "Stop", "Restart", "Status")]
    [string]$Action = "Install"
)

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Administrator)) {
    Write-ColorOutput "ERROR: This script requires Administrator privileges!" "Red"
    Write-ColorOutput "Please run PowerShell as Administrator and try again." "Yellow"
    exit 1
}

Write-ColorOutput "═══════════════════════════════════════════════════" "Cyan"
Write-ColorOutput "  HRMS Device Sync - Windows Service Deployment" "Cyan"
Write-ColorOutput "═══════════════════════════════════════════════════" "Cyan"

switch ($Action) {
    "Install" {
        Write-ColorOutput "`n📦 Installing Windows Service..." "Yellow"

        # Check if service already exists
        $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($existingService) {
            Write-ColorOutput "⚠️  Service already exists. Uninstalling first..." "Yellow"
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            sc.exe delete $ServiceName
            Start-Sleep -Seconds 2
        }

        # Create the service
        New-Service -Name $ServiceName `
                    -BinaryPathName $BinaryPath `
                    -DisplayName $ServiceName `
                    -Description $Description `
                    -StartupType Automatic

        Write-ColorOutput "✅ Service installed successfully!" "Green"
        Write-ColorOutput "`n🚀 Starting service..." "Yellow"
        Start-Service -Name $ServiceName

        $status = Get-Service -Name $ServiceName
        Write-ColorOutput "`n📊 Service Status: $($status.Status)" "Green"
    }

    "Uninstall" {
        Write-ColorOutput "`n🗑️  Uninstalling Windows Service..." "Yellow"

        $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $existingService) {
            Write-ColorOutput "⚠️  Service not found!" "Yellow"
            exit 0
        }

        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        sc.exe delete $ServiceName

        Write-ColorOutput "✅ Service uninstalled successfully!" "Green"
    }

    "Start" {
        Write-ColorOutput "`n🚀 Starting service..." "Yellow"
        Start-Service -Name $ServiceName
        $status = Get-Service -Name $ServiceName
        Write-ColorOutput "✅ Service Status: $($status.Status)" "Green"
    }

    "Stop" {
        Write-ColorOutput "`n⏹️  Stopping service..." "Yellow"
        Stop-Service -Name $ServiceName -Force
        $status = Get-Service -Name $ServiceName
        Write-ColorOutput "✅ Service Status: $($status.Status)" "Green"
    }

    "Restart" {
        Write-ColorOutput "`n🔄 Restarting service..." "Yellow"
        Restart-Service -Name $ServiceName -Force
        $status = Get-Service -Name $ServiceName
        Write-ColorOutput "✅ Service Status: $($status.Status)" "Green"
    }

    "Status" {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $service) {
            Write-ColorOutput "⚠️  Service not found!" "Yellow"
            exit 1
        }

        Write-ColorOutput "`n📊 Service Status:" "Cyan"
        Write-ColorOutput "  Name: $($service.Name)" "White"
        Write-ColorOutput "  Display Name: $($service.DisplayName)" "White"
        Write-ColorOutput "  Status: $($service.Status)" "$(if ($service.Status -eq 'Running') { 'Green' } else { 'Yellow' })"
        Write-ColorOutput "  Startup Type: $($service.StartType)" "White"
    }
}

Write-ColorOutput "`n═══════════════════════════════════════════════════" "Cyan"
Write-ColorOutput "  Deployment Complete!" "Cyan"
Write-ColorOutput "═══════════════════════════════════════════════════" "Cyan"

# Show logs location
Write-ColorOutput "`n📝 Logs Location: C:\HRMS\DeviceSync\logs\" "Cyan"
Write-ColorOutput "💡 View logs: Get-Content C:\HRMS\DeviceSync\logs\device-sync-*.txt -Tail 50 -Wait" "Cyan"
