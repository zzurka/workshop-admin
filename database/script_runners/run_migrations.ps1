#Requires -Version 5.1
<#
.SYNOPSIS
    Database migration runner for PostgreSQL.

.DESCRIPTION
    Executes .sql scripts from database/scripts/ in lexicographic order.
    Tracks execution in migration.migration_history with SHA-256 checksums.

.PARAMETER DryRun
    Show which scripts would be executed without running them.

.PARAMETER Verbose
    Print detailed output for each script.

.EXAMPLE
    .\run_migrations.ps1
    .\run_migrations.ps1 -DryRun
    .\run_migrations.ps1 -Verbose
#>

[CmdletBinding()]
param(
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------
$ScriptRunnerDir = $PSScriptRoot
$ProjectRoot = (Resolve-Path "$ScriptRunnerDir\..\..").Path
$SqlDir = Join-Path $ProjectRoot "database\scripts"
$EnvFile = Join-Path $ScriptRunnerDir ".env"

# ---------------------------------------------------------------------------
# Utility functions
# ---------------------------------------------------------------------------
function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $Message"
}

function Write-Verbose-Log {
    param([string]$Message)
    if ($VerbosePreference -eq "Continue") {
        Write-Log $Message
    }
}

function Get-FileChecksum {
    param([string]$FilePath)
    $hash = Get-FileHash -Path $FilePath -Algorithm SHA256
    return $hash.Hash.ToLower()
}

function Invoke-Psql {
    param(
        [string]$Query,
        [switch]$TuplesOnly
    )

    $psqlArgs = @()
    if ($TuplesOnly) {
        $psqlArgs += "-t", "-A"
    }
    $psqlArgs += "-c", $Query

    $prevPref = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $result = & psql @psqlArgs 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $prevPref

    if ($exitCode -ne 0) {
        return $null
    }
    return ($result | Out-String).Trim()
}

function Invoke-PsqlFile {
    param([string]$FilePath)

    $prevPref = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $result = & psql -v ON_ERROR_STOP=1 -f $FilePath 2>&1
    $exitCode = $LASTEXITCODE
    $ErrorActionPreference = $prevPref

    return @{
        Output   = ($result | Out-String).Trim()
        ExitCode = $exitCode
    }
}

# ---------------------------------------------------------------------------
# Load environment variables
# ---------------------------------------------------------------------------
function Import-EnvFile {
    if (Test-Path $EnvFile) {
        Get-Content $EnvFile | ForEach-Object {
            $line = $_.Trim()
            if ($line -and -not $line.StartsWith("#")) {
                $parts = $line -split "=", 2
                if ($parts.Count -eq 2) {
                    $key = $parts[0].Trim()
                    $value = $parts[1].Trim()
                    [System.Environment]::SetEnvironmentVariable($key, $value, "Process")
                }
            }
        }
        Write-Verbose-Log "Loaded connection parameters from $EnvFile"
    }
}

function Test-ConnectionParams {
    $required = @("PGHOST", "PGPORT", "PGDATABASE", "PGUSER")
    foreach ($var in $required) {
        $value = [System.Environment]::GetEnvironmentVariable($var, "Process")
        if ([string]::IsNullOrWhiteSpace($value)) {
            Write-Error "ERROR: $var is not set. Configure it in $EnvFile or as an environment variable."
            exit 1
        }
    }
}

# ---------------------------------------------------------------------------
# Ensure migration tracking infrastructure exists
# ---------------------------------------------------------------------------
function Initialize-TrackingTable {
    $bootstrapScript = Join-Path $SqlDir "20260313_1400_S_migration_DDL.sql"

    $tableExists = Invoke-Psql -TuplesOnly -Query @"
        SELECT EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema = 'migration' AND table_name = 'migration_history'
        );
"@

    if ($tableExists -ne "t") {
        Write-Log "Migration tracking table not found. Running bootstrap migration..."

        if (-not (Test-Path $bootstrapScript)) {
            Write-Error "ERROR: Bootstrap script not found at $bootstrapScript"
            exit 1
        }

        if ($DryRun) {
            Write-Log "[DRY RUN] Would execute bootstrap: $(Split-Path $bootstrapScript -Leaf)"
            return
        }

        $result = Invoke-PsqlFile -FilePath $bootstrapScript
        if ($result.ExitCode -ne 0) {
            Write-Error "ERROR: Bootstrap migration failed:`n$($result.Output)"
            exit 1
        }
        Write-Log "Bootstrap migration completed successfully."
    }
}

# ---------------------------------------------------------------------------
# Main migration loop
# ---------------------------------------------------------------------------
function Start-Migrations {
    $sqlFiles = @(Get-ChildItem -Path $SqlDir -Filter "*.sql" | Sort-Object Name)
    $total = $sqlFiles.Count
    $executed = 0
    $skipped = 0
    $failed = 0

    if ($total -eq 0) {
        Write-Log "No SQL scripts found in $SqlDir"
        return
    }

    Write-Log "Found $total SQL script(s) in $SqlDir"
    Write-Host ""

    foreach ($sqlFile in $sqlFiles) {
        $scriptName = $sqlFile.Name
        $checksum = Get-FileChecksum -FilePath $sqlFile.FullName

        # Query the tracking table for this script
        $row = Invoke-Psql -TuplesOnly -Query @"
            SELECT checksum_sha256, success
            FROM migration.migration_history
            WHERE script_name = '$scriptName'
            ORDER BY id DESC LIMIT 1;
"@

        if (-not [string]::IsNullOrWhiteSpace($row)) {
            $parts = $row -split "\|"
            $storedChecksum = $parts[0]
            $storedSuccess = $parts[1]

            if ($storedSuccess -eq "t") {
                if ($storedChecksum -ne $checksum) {
                    Write-Host ""
                    Write-Error @"
ERROR: Checksum mismatch for $scriptName
  Stored:  $storedChecksum
  Current: $checksum
  The script was modified after execution. Create a new migration instead.
"@
                    exit 1
                }
                Write-Verbose-Log "SKIP: $scriptName (already executed)"
                $skipped++
                continue
            }
            else {
                # Script previously failed - delete the failed record and retry
                Write-Log "RETRY: $scriptName (previous execution failed)"
                if (-not $DryRun) {
                    Invoke-Psql -TuplesOnly -Query "DELETE FROM migration.migration_history WHERE script_name = '$scriptName' AND success = FALSE;" | Out-Null
                }
            }
        }

        # Execute the script
        if ($DryRun) {
            Write-Log "[DRY RUN] Would execute: $scriptName (checksum: $($checksum.Substring(0, 16))...)"
            $executed++
            continue
        }

        Write-Log "EXEC: $scriptName"
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

        $result = Invoke-PsqlFile -FilePath $sqlFile.FullName

        $stopwatch.Stop()
        $durationMs = $stopwatch.ElapsedMilliseconds

        if ($result.ExitCode -eq 0) {
            # Record success
            Invoke-Psql -TuplesOnly -Query @"
                INSERT INTO migration.migration_history
                    (script_name, checksum_sha256, execution_ms, success)
                VALUES
                    ('$scriptName', '$checksum', $durationMs, TRUE);
"@ | Out-Null
            Write-Verbose-Log "  Completed in ${durationMs}ms"
            $executed++
        }
        else {
            # Record failure
            $safeError = ($result.Output -replace "'", "''") | Select-Object -First 20
            $safeErrorStr = ($safeError -join "`n")
            Invoke-Psql -TuplesOnly -Query @"
                INSERT INTO migration.migration_history
                    (script_name, checksum_sha256, execution_ms, success, error_message)
                VALUES
                    ('$scriptName', '$checksum', $durationMs, FALSE, '$safeErrorStr');
"@ | Out-Null

            Write-Host ""
            Write-Host "FAILED: $scriptName" -ForegroundColor Red
            Write-Host $result.Output
            Write-Host ""
            Write-Log "Stopping execution due to failure."
            $failed++
            break
        }
    }

    # Summary
    Write-Host ""
    Write-Host "=============================="
    Write-Log "Migration Summary"
    Write-Host "  Total scripts: $total"
    Write-Host "  Executed:      $executed"
    Write-Host "  Skipped:       $skipped"
    Write-Host "  Failed:        $failed"
    Write-Host "=============================="

    if ($failed -gt 0) {
        exit 1
    }
}

# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------
Write-Log "Starting database migrations..."
if ($DryRun) {
    Write-Log "*** DRY RUN MODE - no changes will be made ***"
}
Write-Host ""

Import-EnvFile
Test-ConnectionParams
Initialize-TrackingTable
Start-Migrations

Write-Log "Done."
