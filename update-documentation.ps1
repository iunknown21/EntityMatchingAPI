# Update all documentation files from ProfileMatching to EntityMatching

Write-Host "Updating documentation files..." -ForegroundColor Green

$docsPath = ".\docs"
$mdFiles = Get-ChildItem -Path $docsPath -Filter "*.md" -Recurse -File

Write-Host "Found $($mdFiles.Count) markdown files in docs/" -ForegroundColor Cyan

$updatedFiles = 0
foreach ($file in $mdFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # Replace all ProfileMatching references
    $content = $content -replace 'ProfileMatching', 'EntityMatching'
    $content = $content -replace 'profilematching', 'entitymatching'
    $content = $content -replace 'ProfileMatchingAPI', 'EntityMatchingAPI'
    $content = $content -replace 'profileaiapi', 'entityaiapi'
    $content = $content -replace 'profilesai', 'entitymatchingai'
    $content = $content -replace 'profilematching-apim', 'entitymatching-apim'
    $content = $content -replace 'profilesaidb', 'entitymatchingdb'
    $content = $content -replace '@profilematching/sdk', '@entitymatching/sdk'

    # Update GitHub URLs
    $content = $content -replace 'github.com/iunknown21/ProfileMatchingAPI', 'github.com/iunknown21/EntityMatchingAPI'

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $updatedFiles++
        Write-Host "  Updated: $($file.Name)" -ForegroundColor Yellow
    }
}

# Update README files in SDK folders
$readmeFiles = Get-ChildItem -Path . -Filter "README.md" -Recurse -File | Where-Object { $_.DirectoryName -like "*SDK*" }

Write-Host "`nUpdating SDK README files..." -ForegroundColor Cyan
foreach ($file in $readmeFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    $content = $content -replace 'ProfileMatching', 'EntityMatching'
    $content = $content -replace 'profilematching', 'entitymatching'
    $content = $content -replace 'ProfileMatchingAPI', 'EntityMatchingAPI'
    $content = $content -replace '@profilematching/sdk', '@entitymatching/sdk'
    $content = $content -replace 'github.com/iunknown21/ProfileMatchingAPI', 'github.com/iunknown21/EntityMatchingAPI'

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $updatedFiles++
        Write-Host "  Updated: $($file.FullName)" -ForegroundColor Yellow
    }
}

Write-Host "`n✅ Updated $updatedFiles documentation files" -ForegroundColor Green
