# ProfileMatchingAPI - Large Scale Demo Runner
# This script runs the 1000-profile demo and keeps the data for live API testing

Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host "                                                                    " -ForegroundColor Cyan
Write-Host "              ProfileMatchingAPI - DEMO RUNNER                      " -ForegroundColor Cyan
Write-Host "                                                                    " -ForegroundColor Cyan
Write-Host "   This will create 1000 diverse profiles with AI embeddings       " -ForegroundColor Cyan
Write-Host "   and keep them in the database for live API demonstrations       " -ForegroundColor Cyan
Write-Host "                                                                    " -ForegroundColor Cyan
Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variable to keep test data
$env:SKIP_TEST_CLEANUP = "true"
Write-Host "[OK] Set SKIP_TEST_CLEANUP=true (data will be kept)" -ForegroundColor Green
Write-Host ""

# Confirm before running (this is expensive!)
Write-Host "WARNING: This demo will:" -ForegroundColor Yellow
Write-Host "  - Create 1000 profiles in Cosmos DB" -ForegroundColor Yellow
Write-Host "  - Generate 1000 AI summaries (uses OpenAI API)" -ForegroundColor Yellow
Write-Host "  - Generate 1000 embeddings (uses OpenAI API)" -ForegroundColor Yellow
Write-Host "  - Take approximately 20-30 minutes to complete" -ForegroundColor Yellow
Write-Host "  - Cost approximately $2-5 in OpenAI API fees" -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "Do you want to continue? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host "Demo cancelled." -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Starting demo..." -ForegroundColor Green
Write-Host ""

# Navigate to test directory
Set-Location ProfileMatching.Tests

# Run the specific demo test
dotnet test --filter "FullyQualifiedName~LargeScaleSearchDemoTests.Demo_1000Profiles_ComprehensiveSearchShowcase" --verbosity normal

# Check if successful
if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "====================================================================" -ForegroundColor Green
    Write-Host "                                                                    " -ForegroundColor Green
    Write-Host "                     DEMO DATA CREATED!                             " -ForegroundColor Green
    Write-Host "                                                                    " -ForegroundColor Green
    Write-Host "====================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "You now have 1000 profiles in your database ready for demo!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Start the Functions app:" -ForegroundColor White
    Write-Host "     cd ..\ProfileMatching.Functions" -ForegroundColor Gray
    Write-Host "     func start" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Try these API queries:" -ForegroundColor White
    Write-Host "     POST http://localhost:7071/api/v1/search/profiles" -ForegroundColor Gray
    Write-Host '     Body: { "query": "outdoor enthusiast who loves hiking", "limit": 10 }' -ForegroundColor Gray
    Write-Host ""
    Write-Host "     POST http://localhost:7071/api/v1/search/profiles" -ForegroundColor Gray
    Write-Host '     Body: { "query": "software engineer interested in AI", "limit": 10 }' -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. To clean up the demo data:" -ForegroundColor White
    Write-Host "     Remove-Item Env:\SKIP_TEST_CLEANUP" -ForegroundColor Gray
    Write-Host "     dotnet test --filter 'FullyQualifiedName~LargeScaleSearchDemoTests'" -ForegroundColor Gray
    Write-Host ""
}
else {
    Write-Host ""
    Write-Host "[X] Demo failed. Check the output above for errors." -ForegroundColor Red
    Write-Host ""
}

# Navigate back
Set-Location ..
