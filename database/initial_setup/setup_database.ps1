#Requires -Version 5.1
<#
.SYNOPSIS
    One-time setup: creates the workshopadmin database, admin user (owner),
    and app user (limited privileges for the backend).

.DESCRIPTION
    Must be run as a PostgreSQL superuser (e.g., postgres).

    Users created:
      workshopadmin_admin  - database owner, runs migrations (CREATE/ALTER/DROP)
      workshopadmin_app    - backend user (SELECT, INSERT, UPDATE, DELETE only)

.PARAMETER AdminPassword
    Password for workshopadmin_admin. Prompted securely if omitted.

.PARAMETER AppPassword
    Password for workshopadmin_app. Prompted securely if omitted.

.PARAMETER PgHost
    PostgreSQL host (default: localhost).

.PARAMETER PgPort
    PostgreSQL port (default: 5432).

.EXAMPLE
    .\setup_database.ps1
    .\setup_database.ps1 -AdminPassword "admin123" -AppPassword "app123"
#>

[CmdletBinding()]
param(
    [string]$AdminPassword,
    [string]$AppPassword,
    [string]$PgHost = "localhost",
    [string]$PgPort = "5432"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$DbName = "workshopadmin"
$AdminUser = "workshopadmin_admin"
$AppUser = "workshopadmin_app"

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

function Read-SecurePassword {
    param([string]$UserName)
    $securePassword = Read-Host "Enter password for $UserName" -AsSecureString
    $password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    )
    if ([string]::IsNullOrWhiteSpace($password)) {
        Write-Error "ERROR: Password cannot be empty."
        exit 1
    }
    return $password
}

# ---------------------------------------------------------------------------
# Prompt for passwords if not provided
# ---------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    $AdminPassword = Read-SecurePassword -UserName $AdminUser
}

if ([string]::IsNullOrWhiteSpace($AppPassword)) {
    $AppPassword = Read-SecurePassword -UserName $AppUser
}

# ---------------------------------------------------------------------------
# Create admin user (database owner, runs migrations)
# ---------------------------------------------------------------------------
Write-Log "Checking if user '$AdminUser' exists..."
$adminExists = Invoke-Psql -Query "SELECT 1 FROM pg_roles WHERE rolname = '$AdminUser';"

if ($adminExists -eq "1") {
    Write-Log "User '$AdminUser' already exists. Updating password..."
    Invoke-PsqlCommand -Query "ALTER USER $AdminUser WITH PASSWORD '$AdminPassword';"
}
else {
    Write-Log "Creating user '$AdminUser'..."
    Invoke-PsqlCommand -Query "CREATE USER $AdminUser WITH PASSWORD '$AdminPassword';"
}

# ---------------------------------------------------------------------------
# Create app user (backend, limited privileges)
# ---------------------------------------------------------------------------
Write-Log "Checking if user '$AppUser' exists..."
$appExists = Invoke-Psql -Query "SELECT 1 FROM pg_roles WHERE rolname = '$AppUser';"

if ($appExists -eq "1") {
    Write-Log "User '$AppUser' already exists. Updating password..."
    Invoke-PsqlCommand -Query "ALTER USER $AppUser WITH PASSWORD '$AppPassword';"
}
else {
    Write-Log "Creating user '$AppUser'..."
    Invoke-PsqlCommand -Query "CREATE USER $AppUser WITH PASSWORD '$AppPassword';"
}

# ---------------------------------------------------------------------------
# Create database (owned by admin)
# ---------------------------------------------------------------------------
Write-Log "Checking if database '$DbName' exists..."
$dbExists = Invoke-Psql -Query "SELECT 1 FROM pg_database WHERE datname = '$DbName';"

if ($dbExists -eq "1") {
    Write-Log "Database '$DbName' already exists. Skipping creation."
}
else {
    Write-Log "Creating database '$DbName' owned by '$AdminUser'..."
    Invoke-PsqlCommand -Query "CREATE DATABASE $DbName OWNER $AdminUser ENCODING 'UTF8' TEMPLATE template0;"
}

# ---------------------------------------------------------------------------
# Configure privileges
# ---------------------------------------------------------------------------
Write-Log "Configuring admin privileges..."

Invoke-PsqlCommand -Query "GRANT ALL PRIVILEGES ON DATABASE $DbName TO $AdminUser;" -Database $DbName
Invoke-PsqlCommand -Query "GRANT ALL ON SCHEMA public TO $AdminUser;" -Database $DbName
Invoke-PsqlCommand -Query "GRANT CREATE ON DATABASE $DbName TO $AdminUser;" -Database $DbName

Write-Log "Configuring app user privileges..."

# App: connect to the database
Invoke-PsqlCommand -Query "GRANT CONNECT ON DATABASE $DbName TO $AppUser;" -Database $DbName

# App: use the public schema (but not create objects)
Invoke-PsqlCommand -Query "GRANT USAGE ON SCHEMA public TO $AppUser;" -Database $DbName

# App: DML only on existing and future tables/sequences
Invoke-PsqlCommand -Query "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO $AppUser;" -Database $DbName
Invoke-PsqlCommand -Query "GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO $AppUser;" -Database $DbName
Invoke-PsqlCommand -Query "GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO $AppUser;" -Database $DbName

# Default privileges: auto-grant to app user on objects created by admin
Invoke-PsqlCommand -Query "ALTER DEFAULT PRIVILEGES FOR ROLE $AdminUser IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $AppUser;" -Database $DbName
Invoke-PsqlCommand -Query "ALTER DEFAULT PRIVILEGES FOR ROLE $AdminUser IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO $AppUser;" -Database $DbName
Invoke-PsqlCommand -Query "ALTER DEFAULT PRIVILEGES FOR ROLE $AdminUser IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO $AppUser;" -Database $DbName

Write-Log "Setup complete."
Write-Host ""
Write-Host "=============================="
Write-Host "  Database:   $DbName"
Write-Host "  Admin user: $AdminUser  (owner, migrations)"
Write-Host "  App user:   $AppUser  (backend, DML only)"
Write-Host "  Host:       $PgHost"
Write-Host "  Port:       $PgPort"
Write-Host "=============================="
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Copy .env.example to .env in script_runners\"
Write-Host "  2. Use $AdminUser credentials in .env (for running migrations)"
Write-Host "  3. Use $AppUser credentials in your backend appsettings.json"
Write-Host "  4. Run .\run_migrations.ps1 to apply migrations"
