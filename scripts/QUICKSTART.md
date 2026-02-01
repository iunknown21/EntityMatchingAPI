# Progressive Conversation Test - Quick Start

## Quick Start

```bash
# Set your API key
export ENTITY_API_KEY="your-function-key-here"

# Run the test
./scripts/progressive_conversation_test.sh
```

## What Happens

1. ✅ Creates temporary test entity
2. 📝 Sends 5 progressive messages about career interests
3. 🤖 AI responds conversationally and extracts insights
4. 📊 Shows accumulated insights grouped by category
5. 🧹 Offers optional cleanup

## Expected Results

```
✓ 18 insights extracted
✓ 6 categories: hobby, interest, values, personality, lifestyle, preference
✓ Average confidence: 0.86 (86%)
✓ 10 total messages (5 user + 5 AI)
✓ Full conversation history retrieved
```

## Sample Output

```
MESSAGE 1: "I'm really passionate about sports and athletics"
→ AI: "That's so cool. What kind of sports are you into?..."
→ [HOBBY] enjoys sports and athletics (1.00)

MESSAGE 5: "I want to help athletes recover through rehabilitation"
→ AI: "That's a specific and meaningful goal..."
→ [INTEREST] helping athletes recover from injuries (1.00)
→ [INTEREST] sports rehabilitation (1.00)
→ [VALUES] passionate about helping others (0.90)

FINAL INSIGHTS:
  HOBBY: 3 insights (sports, athletics)
  INTEREST: 8 insights (healthcare, physical therapy, helping athletes)
  VALUES: 1 insight (helping others)
  PERSONALITY: 3 insights (caring, supportive, empathetic)
  LIFESTYLE: 2 insights (fitness, active lifestyle)
  PREFERENCE: 1 insight (supportive relationships)
```

## Demonstration Goals

Shows ConversationService:
- ✅ Natural conversational AI
- ✅ Context-aware responses
- ✅ Accurate insight extraction
- ✅ Progressive understanding (general → specific)
- ✅ Category-based organization
- ✅ High confidence scoring (0.60-1.00)

## Prerequisites

- Bash (Git Bash on Windows)
- Python (for JSON parsing)
- curl
- Internet connection

## Troubleshooting

**Permission denied**:
```bash
chmod +x scripts/progressive_conversation_test.sh
```

**Python not found**:
```bash
# Use py launcher on Windows
alias python=py
```

**API errors**:
- Check Function Key in script
- Verify ConversationService enabled in Azure
- Check Groq API key configured

## Cleanup

At test end, you'll be prompted:
```
Delete conversation and test entity from database? (y/n):
```

- **y** = Clean removal (recommended)
- **n** = Keep data for inspection (entity ID displayed)

## Use Cases

This pattern applies to:
- Career discovery chatbots
- Dating profile builders
- Preference learning systems
- Conversational onboarding
- Recommendation engines
- User profiling via dialogue

## Next Steps

1. ✅ Run the test
2. 📊 Review extracted insights
3. 🔍 Inspect conversation flow
4. 💡 Consider integration with search API
5. 🚀 Adapt for your use case

---

**Test Duration**: ~15 seconds
**API Calls**: 7 total
**Success Rate**: 100%
**Last Updated**: 2026-01-30
