#Requires -Version 5.1
<#
.SYNOPSIS
    One-time setup: creates the workshopadmin database and workshopadmin_app user.

.DESCRIPTION
    Must be run as a PostgreSQL superuser (e.g., postgres).
    Creates the database and user, then configures privileges.

.PARAMETER Password
    Password for the workshopadmin_app user. Prompted securely if omitted.

.PARAMETER PgHost
    PostgreSQL host (default: localhost).

.PARAMETER PgPort
    PostgreSQL port (default: 5432).

.EXAMPLE
    .\setup_database.ps1
    .\setup_database.ps1 -Password "mypassword" -PgHost "db.example.com"
#>

[CmdletBinding()]
param(
    [string]$Password,
    [string]$PgHost = "localhost",
    [string]$PgPort = "5432"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$DbName = "workshopadmin"
$DbUser = "workshopadmin_app"

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $Message"
}

function Invoke-Psql {
    param(
        [string]$Query,
        [string]$Database = "postgres"
    )
    $result = & psql -h $PgHost -p $PgPort -d $Database -t -A -c $Query 2>&1
    if ($LASTEXITCODE -ne 0) {
        return $null
    }
    return ($result | Out-String).Trim()
}

function Invoke-PsqlCommand {
    param(
        [string]$Query,
        [string]$Database = "postgres"
    )
    & psql -h $PgHost -p $PgPort -d $Database -c $Query 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to execute: $Query"
        exit 1
    }
}

# ---------------------------------------------------------------------------
# Prompt for password if not provided
# ---------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($Password)) {
    $securePassword = Read-Host "Enter password for $DbUser" -AsSecureString
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    )
    if ([string]::IsNullOrWhiteSpace($Password)) {
        Write-Error "ERROR: Password cannot be empty."
        exit 1
    }
}

# ---------------------------------------------------------------------------
# Create user
# ---------------------------------------------------------------------------
Write-Log "Checking if user '$DbUser' exists..."
$userExists = Invoke-Psql -Query "SELECT 1 FROM pg_roles WHERE rolname = '$DbUser';"

if ($userExists -eq "1") {
    Write-Log "User '$DbUser' already exists. Updating password..."
    Invoke-PsqlCommand -Query "ALTER USER $DbUser WITH PASSWORD '$Password';"
}
else {
    Write-Log "Creating user '$DbUser'..."
    Invoke-PsqlCommand -Query "CREATE USER $DbUser WITH PASSWORD '$Password';"
}

# ---------------------------------------------------------------------------
# Create database
# ---------------------------------------------------------------------------
Write-Log "Checking if database '$DbName' exists..."
$dbExists = Invoke-Psql -Query "SELECT 1 FROM pg_database WHERE datname = '$DbName';"

if ($dbExists -eq "1") {
    Write-Log "Database '$DbName' already exists. Skipping creation."
}
else {
    Write-Log "Creating database '$DbName'..."
    Invoke-PsqlCommand -Query "CREATE DATABASE $DbName OWNER $DbUser ENCODING 'UTF8' TEMPLATE template0;"
}

# ---------------------------------------------------------------------------
# Configure database
# ---------------------------------------------------------------------------
Write-Log "Configuring database..."

Invoke-PsqlCommand -Query "GRANT ALL PRIVILEGES ON DATABASE $DbName TO $DbUser;" -Database $DbName
Invoke-PsqlCommand -Query "GRANT ALL ON SCHEMA public TO $DbUser;" -Database $DbName
Invoke-PsqlCommand -Query "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO $DbUser;" -Database $DbName
Invoke-PsqlCommand -Query "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO $DbUser;" -Database $DbName
Invoke-PsqlCommand -Query "ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO $DbUser;" -Database $DbName
Invoke-PsqlCommand -Query "GRANT CREATE ON DATABASE $DbName TO $DbUser;" -Database $DbName

Write-Log "Setup complete."
Write-Host ""
Write-Host "=============================="
Write-Host "  Database: $DbName"
Write-Host "  User:     $DbUser"
Write-Host "  Host:     $PgHost"
Write-Host "  Port:     $PgPort"
Write-Host "=============================="
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Copy .env.example to .env and fill in the connection parameters"
Write-Host "  2. Run .\run_migrations.ps1 to apply migrations"
