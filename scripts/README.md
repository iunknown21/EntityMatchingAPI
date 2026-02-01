# Test Scripts

This directory contains test scripts for demonstrating and validating the EntityMatching API functionality.

## Progressive Conversation Test

**File**: `progressive_conversation_test.sh`

### Purpose

Demonstrates the ConversationService API by conducting a progressive conversation that builds career interests from general to specific:

1. **General interest**: "I like sports"
2. **Healthcare interest**: "I'm interested in helping people recover"
3. **Values**: "I love helping people achieve goals"
4. **Personal experience**: "Physical therapy helped me"
5. **Specific career goal**: "I want to help athletes through rehabilitation"

### What It Tests

- ✅ POST `/api/v1/entities/{entityId}/conversation` - Send messages, get AI responses
- ✅ AI insight extraction (categories: interest, values, personality, etc.)
- ✅ Insight accumulation across conversation
- ✅ GET `/api/v1/entities/{entityId}/conversation` - Retrieve full history
- ✅ DELETE `/api/v1/entities/{entityId}/conversation` - Optional cleanup

### Requirements

- **Bash** (Git Bash on Windows, native on Linux/Mac)
- **Python** (for JSON parsing)
- **curl** (for API requests)
- **Internet connection** (to reach Azure Function API)

### Usage

```bash
# Navigate to project root
cd /d/Development/Main/EntityMatchingAPI

# Set your API key (required)
export ENTITY_API_KEY="your-function-key-here"

# Run the test
./scripts/progressive_conversation_test.sh
```

### Expected Output

```
======================================================================
PROGRESSIVE CONVERSATION TEST
======================================================================
Entity ID: d79df7b1-111f-4880-90c3-4697f7a2692c
User ID: test-user-progressive
Messages: 5
======================================================================

Creating test entity...
✓ Test entity created successfully

======================================================================
MESSAGE 1 of 5
======================================================================
USER: I'm really passionate about sports and athletics

----------------------------------------------------------------------
AI RESPONSE:
----------------------------------------------------------------------
That's awesome! So, you're into sports and athletics, but what about your
partner? Are they into sports as well, or do they have different interests?

----------------------------------------------------------------------
NEW INSIGHTS EXTRACTED:
----------------------------------------------------------------------
  [HOBBY] enjoys sports and athletics (confidence: 1.00)
  [INTEREST] interested in athletic activities (confidence: 1.00)

New insights this message: 2

... (4 more messages) ...

======================================================================
FULL CONVERSATION HISTORY
======================================================================

CONVERSATION:
----------------------------------------------------------------------
  [USER] I'm really passionate about sports and athletics
  [AI]   That's awesome! So, you're into sports and athletics...
  [USER] I'm also interested in healthcare and helping people recover...
  [AI]   That's really great to hear that you're passionate about...
  ...

======================================================================
ACCUMULATED INSIGHTS (grouped by category):
======================================================================

HOBBY:
  • enjoys sports and athletics (1.00)
  • enjoys sports (0.90)
  • sports (0.90)

INTEREST:
  • interested in athletic activities (1.00)
  • healthcare (1.00)
  • helping people recover from injuries (1.00)
  • empowering others (1.00)
  • interested in health and wellness (0.80)
  • helping athletes recover from injuries (1.00)
  • physical therapy (0.90)

LIFESTYLE:
  • values staying active (0.80)
  • active lifestyle (0.80)

PERSONALITY:
  • caring and supportive (0.80)
  • passionate about helping others (1.00)
  • resilient (0.70)
  • caring (0.70)

VALUES:
  • making a positive impact (1.00)
  • helping others (0.80)

======================================================================
STATISTICS:
======================================================================
  Total messages: 10
  Total insights: 18
  Unique categories: 5
  Average confidence: 0.89
======================================================================

Delete conversation and test entity from database? (y/n):
```

### Success Criteria

- [ ] All 5 messages sent successfully
- [ ] AI provides conversational responses (not errors)
- [ ] Insights extracted with reasonable confidence scores (0.7-0.95)
- [ ] Insights accumulate across messages
- [ ] GET history returns all conversation chunks
- [ ] Insights grouped by category
- [ ] No API errors or timeouts
- [ ] Demonstrates progression from general → specific interests

### Configuration

The script uses:
- **API URL**: `https://entityaiapi.azurewebsites.net/api/v1`
- **Function Key**: Embedded in script (production key)
- **Entity ID**: Auto-generated with timestamp (`test-conversation-{timestamp}`)
- **User ID**: `test-user-progressive`

To modify configuration, edit the top of the script:
```bash
API_KEY="your-function-key"
API_URL="your-api-url"
```

### Cleanup

At the end of the test, you'll be prompted to delete the conversation:
- **Yes (y)**: Removes conversation from Cosmos DB
- **No (n)**: Preserves conversation (prints entity ID for reference)

### Troubleshooting

**Python not found**:
```bash
# Windows: Ensure Python is in PATH
python --version

# Use py launcher if python doesn't work
alias python=py
```

**Curl not found**:
```bash
# Windows: Use Git Bash (includes curl)
# Or install curl for Windows
```

**API errors**:
- Check Function Key is valid
- Verify ConversationService is enabled in Azure
- Check Groq API key is configured in Function App settings
- Verify Cosmos DB conversations container exists

**JSON parsing errors**:
- Check API response format
- Verify Python JSON module is available
- Check for network/timeout issues

### Related Tests

- **progressive_search_test.sh**: Similar progressive demonstration using search API instead of conversation
- Both tests can be run together to show different interaction patterns leading to the same career discovery

### Next Steps

After running this test, you can:

1. **Integrate with Search**: Use extracted insights to build search queries
2. **Compare Approaches**: Run alongside progressive search test
3. **Test Other Narratives**: Modify messages to explore different career paths
4. **Validate Production**: Use on staging environment before deployment

### Technical Details

**AI Model**: Groq llama-3.3-70b-versatile
**Insight Categories**: hobby, preference, interest, personality, values, lifestyle, restriction
**Storage**: Azure Cosmos DB (conversations container, partitioned by entityId)
**Response Time**: < 3 seconds per message (typical)

### Example Use Cases

This pattern demonstrates:
- **Career Discovery**: Interactive dialogue to find matching careers
- **Profile Building**: Conversational onboarding for dating apps
- **Preference Learning**: Understanding user preferences through chat
- **Recommendation Systems**: Extracting structured data from natural conversation
