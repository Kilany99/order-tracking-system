# scripts/monitoring/Setup-Monitoring.ps1
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

# Create directory structure
Write-ColorOutput Yellow "Creating directory structure..."
New-Item -ItemType Directory -Force -Path "$ProjectRoot\monitoring\grafana\dashboards"
New-Item -ItemType Directory -Force -Path "$ProjectRoot\monitoring\grafana\provisioning\dashboards"
New-Item -ItemType Directory -Force -Path "$ProjectRoot\monitoring\grafana\provisioning\datasources"
New-Item -ItemType Directory -Force -Path "$ProjectRoot\monitoring\prometheus"

# Create datasource configuration
Write-ColorOutput Yellow "Creating datasource configuration..."
@"
apiVersion: 1

datasources:
- name: Prometheus
  type: prometheus
  access: proxy
  orgId: 1
  url: http://prometheus:9090
  basicAuth: false
  isDefault: true
  version: 1
  editable: true
  jsonData:
    timeInterval: "15s"
"@ | Out-File -FilePath "$ProjectRoot\monitoring\grafana\provisioning\datasources\datasources.yaml" -Encoding UTF8

# Create dashboard provisioning configuration
Write-ColorOutput Yellow "Creating dashboard provisioning configuration..."
@"
apiVersion: 1

providers:
- name: 'default'
  orgId: 1
  folder: ''
  folderUid: ''
  type: file
  disableDeletion: false
  updateIntervalSeconds: 10
  allowUiUpdates: true
  options:
    path: /var/lib/grafana/dashboards
"@ | Out-File -FilePath "$ProjectRoot\monitoring\grafana\provisioning\dashboards\dashboards.yaml" -Encoding UTF8

# Create Prometheus configuration
Write-ColorOutput Yellow "Creating Prometheus configuration..."
@"
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'order-service'
    static_configs:
      - targets: ['order-service:80']
    metrics_path: '/metrics'

  - job_name: 'driver-service'
    static_configs:
      - targets: ['driver-service:80']
    metrics_path: '/metrics'

  - job_name: 'notification-service'
    static_configs:
      - targets: ['notification-service:80']
    metrics_path: '/metrics'
"@ | Out-File -FilePath "$ProjectRoot\monitoring\prometheus\prometheus.yml" -Encoding UTF8

# Stop existing containers
Write-ColorOutput Yellow "Stopping existing containers..."
Set-Location $ProjectRoot
docker-compose down

# Start containers
Write-ColorOutput Yellow "Starting containers..."
docker-compose up -d

# Wait for services to start
Write-ColorOutput Yellow "Waiting for services to start..."
Start-Sleep -Seconds 10

# Verify Prometheus
Write-ColorOutput Yellow "Verifying Prometheus..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:9090/-/healthy" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-ColorOutput Green "Prometheus is healthy"
    }
}
catch {
    Write-ColorOutput Red "Prometheus is not responding"
}

# Verify Grafana
Write-ColorOutput Yellow "Verifying Grafana..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-ColorOutput Green "Grafana is healthy"
    }
}
catch {
    Write-ColorOutput Red "Grafana is not responding"
}

Write-ColorOutput Green "Setup complete!"
Write-ColorOutput Green "Access Grafana at http://localhost:3000"
Write-ColorOutput Green "Default credentials: admin/admin"