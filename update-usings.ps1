# Update using statements to reference ProfileMatching.Shared.Models

$projectPaths = @(
    "D:\Development\Main\ProfileMatchingAPI\ProfileMatching.Core",
    "D:\Development\Main\ProfileMatchingAPI\ProfileMatching.Infrastructure",
    "D:\Development\Main\ProfileMatchingAPI\ProfileMatching.Functions",
    "D:\Development\Main\ProfileMatchingAPI\ProfileMatching.Tests"
)

$filesUpdated = 0

foreach ($projectPath in $projectPaths) {
    $files = Get-ChildItem -Path $projectPath -Filter "*.cs" -Recurse

    foreach ($file in $files) {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content

        # Replace using ProfileMatching.Core.Models; with using ProfileMatching.Shared.Models;
        # But only if it's a standalone using statement (not a subnamespace like .Search or .Embedding)
        $content = $content -replace 'using ProfileMatching\.Core\.Models;', 'using ProfileMatching.Shared.Models;'

        # Also add using for Shared if Profile is referenced but using is missing
        # (Some files might use fully qualified names)

        if ($content -ne $originalContent) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            Write-Host "Updated: $($file.FullName)"
            $filesUpdated++
        }
    }
}

Write-Host "`nTotal files updated: $filesUpdated"
