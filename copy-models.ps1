# Copy shared models from ProfileMatching.Core to ProfileMatching.Shared

$sourceBase = "D:\Development\Main\ProfileMatchingAPI\ProfileMatching.Core\Models"
$destBase = "D:\Development\Main\ProfileMatchingAPI\ProfileMatching.Shared\Models"

# Files to copy (relative to Models folder)
$files = @(
    "Common\ImportantDate.cs",
    "Common\ProfileImage.cs",
    "Personality\LoveLanguages.cs",
    "Personality\PersonalityClassifications.cs",
    "PreferencesAndInterests.cs",
    "ExperiencePreferences.cs",
    "Privacy\FieldVisibility.cs",
    "Privacy\FieldVisibilitySettings.cs",
    "Preferences\EntertainmentPreferences.cs",
    "Preferences\SocialPreferences.cs",
    "Preferences\AdventurePreferences.cs",
    "Preferences\LearningPreferences.cs",
    "Preferences\StylePreferences.cs",
    "Preferences\SensoryPreferences.cs",
    "Preferences\NaturePreferences.cs",
    "Preferences\GiftPreferences.cs",
    "Preferences\ActivityPreferences.cs",
    "Preferences\AccessibilityNeeds.cs",
    "Preferences\DietaryRestrictions.cs"
)

foreach ($file in $files) {
    $sourcePath = Join-Path $sourceBase $file
    $destPath = Join-Path $destBase $file

    if (Test-Path $sourcePath) {
        Write-Host "Copying $file..."

        # Read file content
        $content = Get-Content $sourcePath -Raw

        # Replace namespace
        $content = $content -replace 'namespace ProfileMatching\.Core\.Models', 'namespace ProfileMatching.Shared.Models'

        # Ensure destination directory exists
        $destDir = Split-Path $destPath -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }

        # Write to destination
        Set-Content -Path $destPath -Value $content -NoNewline

        Write-Host "  -> Copied to $destPath"
    }
    else {
        Write-Host "WARNING: Source file not found: $sourcePath"
    }
}

Write-Host "`nCopy complete!"
