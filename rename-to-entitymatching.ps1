# Comprehensive rename script from ProfileMatching to EntityMatching
# This script updates all C# files, project files, and configuration files

Write-Host "Starting ProfileMatching -> EntityMatching rename process..." -ForegroundColor Green

# Get all C# files
$csFiles = Get-ChildItem -Path . -Filter "*.cs" -Recurse -File

Write-Host "Found $($csFiles.Count) C# files to process..." -ForegroundColor Cyan

$updatedFiles = 0
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # Replace namespace declarations
    $content = $content -replace 'namespace ProfileMatching\.', 'namespace EntityMatching.'
    $content = $content -replace 'namespace ProfileMatching\b', 'namespace EntityMatching'

    # Replace using statements
    $content = $content -replace 'using ProfileMatching\.', 'using EntityMatching.'

    # Replace any remaining ProfileMatching references in strings or comments that should be EntityMatching
    # Be careful not to replace things like "profile matching" in documentation
    $content = $content -replace '\bProfileMatching\b(?!\.)', 'EntityMatching'

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $updatedFiles++
        Write-Host "  Updated: $($file.FullName)" -ForegroundColor Yellow
    }
}

Write-Host "`nUpdated $updatedFiles C# files" -ForegroundColor Green

# Get all .csproj files
$csprojFiles = Get-ChildItem -Path . -Filter "*.csproj" -Recurse -File

Write-Host "`nFound $($csprojFiles.Count) .csproj files to process..." -ForegroundColor Cyan

$updatedCsproj = 0
foreach ($file in $csprojFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # Replace project references
    $content = $content -replace 'ProfileMatching\.Core', 'EntityMatching.Core'
    $content = $content -replace 'ProfileMatching\.Infrastructure', 'EntityMatching.Infrastructure'
    $content = $content -replace 'ProfileMatching\.Functions', 'EntityMatching.Functions'
    $content = $content -replace 'ProfileMatching\.Shared', 'EntityMatching.Shared'
    $content = $content -replace 'ProfileMatching\.Tests', 'EntityMatching.Tests'
    $content = $content -replace 'ProfileMatching\.SDK', 'EntityMatching.SDK'

    # Update RootNamespace and AssemblyName
    $content = $content -replace '<RootNamespace>ProfileMatching', '<RootNamespace>EntityMatching'
    $content = $content -replace '<AssemblyName>ProfileMatching', '<AssemblyName>EntityMatching'

    # Update PackageId for SDK projects
    $content = $content -replace '<PackageId>ProfileMatching\.SDK</PackageId>', '<PackageId>EntityMatching.SDK</PackageId>'

    # Update Description
    $content = $content -replace 'ProfileMatchingAPI', 'EntityMatchingAPI'
    $content = $content -replace 'Profile Matching', 'Entity Matching'

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $updatedCsproj++
        Write-Host "  Updated: $($file.FullName)" -ForegroundColor Yellow
    }
}

Write-Host "`nUpdated $updatedCsproj .csproj files" -ForegroundColor Green

# Update JSON files (local.settings.json, package.json, etc.)
$jsonFiles = Get-ChildItem -Path . -Filter "*.json" -Recurse -File | Where-Object { $_.Name -notlike "*lock.json" }

Write-Host "`nFound $($jsonFiles.Count) JSON files to check..." -ForegroundColor Cyan

$updatedJson = 0
foreach ($file in $jsonFiles) {
    try {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content

        # Update package.json if it exists
        if ($file.Name -eq "package.json") {
            $content = $content -replace '"name":\s*"@profilematching/sdk"', '"name": "@entitymatching/sdk"'
            $content = $content -replace 'ProfileMatchingAPI', 'EntityMatchingAPI'
            $content = $content -replace 'Profile Matching', 'Entity Matching'
        }

        # Update other JSON config files
        $content = $content -replace 'ProfileMatching', 'EntityMatching'

        if ($content -ne $originalContent) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            $updatedJson++
            Write-Host "  Updated: $($file.FullName)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "  Skipped: $($file.FullName) (parsing error)" -ForegroundColor Gray
    }
}

Write-Host "`nUpdated $updatedJson JSON files" -ForegroundColor Green

Write-Host "`n=== Rename Process Complete ===" -ForegroundColor Green
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  - C# files updated: $updatedFiles" -ForegroundColor White
Write-Host "  - .csproj files updated: $updatedCsproj" -ForegroundColor White
Write-Host "  - JSON files updated: $updatedJson" -ForegroundColor White
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. Run: dotnet restore" -ForegroundColor White
Write-Host "  2. Run: dotnet build" -ForegroundColor White
Write-Host "  3. Run: dotnet test" -ForegroundColor White
