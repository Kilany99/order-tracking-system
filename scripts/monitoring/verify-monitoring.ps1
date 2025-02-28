# scripts/monitoring/Verify-Monitoring.ps1
# Script location
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = (Get-Item $ScriptDir).Parent.Parent.FullName

# Function to write colored output
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

# Check Grafana
Write-ColorOutput Yellow "Checking Grafana status..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-ColorOutput Green "Grafana is healthy"
    }
}
catch {
    Write-ColorOutput Red "Grafana is not responding"
}

# Check Prometheus
Write-ColorOutput Yellow "Checking Prometheus status..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9090/-/healthy" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-ColorOutput Green "Prometheus is healthy"
    }
}
catch {
    Write-ColorOutput Red "Prometheus is not responding"
}

# Check Grafana datasources
Write-ColorOutput Yellow "Checking Grafana datasources..."
$headers = @{
    Authorization = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:admin"))
}
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/api/datasources" -Headers $headers -UseBasicParsing
    $response.Content | ConvertFrom-Json | ConvertTo-Json
}
catch {
    Write-ColorOutput Red "Failed to get datasources"
}

# Check Prometheus targets
Write-ColorOutput Yellow "Checking Prometheus targets..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9090/api/v1/targets" -UseBasicParsing
    $response.Content | ConvertFrom-Json | ConvertTo-Json
}
catch {
    Write-ColorOutput Red "Failed to get targets"
}