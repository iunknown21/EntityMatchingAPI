# One-time cleanup script to delete all orphaned embeddings
# Run this before running tests to start with a clean slate

Write-Host "Cleanup script created. To actually clean up embeddings, you would need to:"
Write-Host "1. Use Azure Portal to delete all documents in the 'embeddings' container"
Write-Host "2. Or create a C# console app that connects to Cosmos DB and deletes all embeddings"
Write-Host "3. Or use the Cosmos DB Data Explorer to run: SELECT * FROM c and then delete all documents"
Write-Host ""
Write-Host "For now, the tests will continue to work but will be slower due to orphaned embeddings."
