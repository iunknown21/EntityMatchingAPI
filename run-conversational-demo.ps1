# ProfileMatchingAPI - Conversational Profile Building Demo
# This script demonstrates building profiles through natural conversation

Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host "                                                                    " -ForegroundColor Cyan
Write-Host "       Conversational Profile Building Demo                        " -ForegroundColor Cyan
Write-Host "                                                                    " -ForegroundColor Cyan
Write-Host "  Build profiles through natural conversation, not forms!          " -ForegroundColor Cyan
Write-Host "                                                                    " -ForegroundColor Cyan
Write-Host "====================================================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variable to keep test data
$env:SKIP_TEST_CLEANUP = "true"
Write-Host "[OK] Set SKIP_TEST_CLEANUP=true (data will be kept)" -ForegroundColor Green
Write-Host ""

# Confirm before running
Write-Host "INFO: This demo will:" -ForegroundColor Yellow
Write-Host "  - Create 50 profiles through conversational AI" -ForegroundColor Yellow
Write-Host "  - Use Groq AI for natural conversation" -ForegroundColor Yellow
Write-Host "  - Generate AI summaries and embeddings" -ForegroundColor Yellow
Write-Host "  - Take approximately 5-10 minutes to complete" -ForegroundColor Yellow
Write-Host "  - Cost approximately `$0.50-1.00 in API fees" -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "Do you want to continue? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host "Demo cancelled." -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Starting conversational demo..." -ForegroundColor Green
Write-Host ""

# Navigate to test directory
Set-Location ProfileMatching.Tests

# Run the conversational demo test
dotnet test --filter "FullyQualifiedName~ConversationalProfileDemoTests.Demo_ConversationalProfileBuilding_50Profiles" --verbosity normal

# Check if successful
if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "====================================================================" -ForegroundColor Green
    Write-Host "                                                                    " -ForegroundColor Green
    Write-Host "              Conversational Profiles Created!                      " -ForegroundColor Green
    Write-Host "                                                                    " -ForegroundColor Green
    Write-Host "====================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "You now have 50 conversation-built profiles in your database!" -ForegroundColor Green
    Write-Host ""
    Write-Host "What makes these special:" -ForegroundColor Cyan
    Write-Host "  - Built through natural language (no forms!)" -ForegroundColor White
    Write-Host "  - AI-extracted insights from conversations" -ForegroundColor White
    Write-Host "  - More realistic and varied than parametric profiles" -ForegroundColor White
    Write-Host "  - Demonstrates your unique conversational profiling feature" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Start the Functions app:" -ForegroundColor White
    Write-Host "     cd ..\ProfileMatching.Functions" -ForegroundColor Gray
    Write-Host "     func start" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Search for these conversational profiles:" -ForegroundColor White
    Write-Host "     POST http://localhost:7071/api/v1/search/profiles" -ForegroundColor Gray
    Write-Host '     Body: { "query": "jazz musician who loves intimate venues", "limit": 10 }' -ForegroundColor Gray
    Write-Host ""
    Write-Host "     POST http://localhost:7071/api/v1/search/profiles" -ForegroundColor Gray
    Write-Host '     Body: { "query": "outdoor guide with wilderness training", "limit": 10 }' -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. To clean up the demo data:" -ForegroundColor White
    Write-Host "     Remove-Item Env:\SKIP_TEST_CLEANUP" -ForegroundColor Gray
    Write-Host "     dotnet test --filter 'FullyQualifiedName~ConversationalProfileDemoTests'" -ForegroundColor Gray
    Write-Host ""
}
else {
    Write-Host ""
    Write-Host "[X] Demo failed. Check the output above for errors." -ForegroundColor Red
    Write-Host ""
}

# Navigate back
Set-Location ..
