#!/bin/bash

# Progressive Conversation Test for ConversationService
# Demonstrates building career interests from general to specific through dialogue

set -e

# Configuration
API_KEY="${ENTITY_API_KEY:-your-function-key-here}"
API_URL="https://entityaiapi.azurewebsites.net/api/v1"
# Generate a valid GUID for entity ID
ENTITY_ID=$(python -c "import uuid; print(str(uuid.uuid4()))")
USER_ID="test-user-progressive"

# Check if API key is set
if [ "$API_KEY" = "your-function-key-here" ]; then
  echo -e "${RED}ERROR: API key not set${NC}"
  echo "Please set the ENTITY_API_KEY environment variable:"
  echo "  export ENTITY_API_KEY=\"your-actual-key\""
  exit 1
fi

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Progressive messages: General sports interest → Specific athletic training career
messages=(
  "I'm really passionate about sports and athletics"
  "I'm also interested in healthcare and helping people recover from injuries"
  "I love helping people achieve their goals and overcome challenges"
  "I had a sports injury a few years ago and physical therapy really helped me recover"
  "I want to help athletes recover from injuries and get back to their sport through rehabilitation"
)

echo -e "${BLUE}======================================================================"
echo "PROGRESSIVE CONVERSATION TEST"
echo "======================================================================"
echo -e "Entity ID: ${GREEN}${ENTITY_ID}${NC}"
echo -e "User ID: ${GREEN}${USER_ID}${NC}"
echo -e "Messages: ${GREEN}${#messages[@]}${NC}"
echo -e "${BLUE}======================================================================${NC}"
echo ""

# Step 1: Create a test entity
echo -e "${YELLOW}Creating test entity...${NC}"
create_response=$(curl -s -X POST \
  "${API_URL}/entities" \
  -H "x-functions-key: ${API_KEY}" \
  -H "Content-Type: application/json" \
  -d "{
    \"id\": \"${ENTITY_ID}\",
    \"ownedByUserId\": \"${USER_ID}\",
    \"name\": \"Test Entity for Conversation\",
    \"description\": \"Temporary entity for progressive conversation test\"
  }")

# Check if entity creation succeeded
if echo "$create_response" | grep -q '"error"'; then
  echo -e "${RED}Failed to create test entity:${NC}"
  echo "$create_response" | python -m json.tool 2>/dev/null || echo "$create_response"
  exit 1
fi

echo -e "${GREEN}✓ Test entity created successfully${NC}"
echo ""

total_insights=0

# Send each message
for i in "${!messages[@]}"; do
  msg_num=$((i + 1))
  message="${messages[$i]}"

  echo -e "${BLUE}======================================================================"
  echo "MESSAGE $msg_num of ${#messages[@]}"
  echo -e "======================================================================${NC}"
  echo -e "${YELLOW}USER:${NC} $message"
  echo ""

  # POST message
  response=$(curl -s -X POST \
    "${API_URL}/entities/${ENTITY_ID}/conversation" \
    -H "x-functions-key: ${API_KEY}" \
    -H "Content-Type: application/json" \
    -d "{\"message\": \"$message\", \"userId\": \"$USER_ID\"}")

  # Check for errors
  if echo "$response" | grep -q '"error"'; then
    echo -e "${RED}ERROR in API response:${NC}"
    echo "$response" | python -m json.tool 2>/dev/null || echo "$response"
    exit 1
  fi

  # Parse and display
  echo "$response" | python -c "
import sys, json

try:
    data = json.load(sys.stdin)

    print('${BLUE}----------------------------------------------------------------------')
    print('AI RESPONSE:')
    print('----------------------------------------------------------------------${NC}')
    print(data.get('aiResponse', 'No response'))
    print('')

    insights = data.get('newInsights', [])
    if insights:
        print('${BLUE}----------------------------------------------------------------------')
        print('NEW INSIGHTS EXTRACTED:')
        print('----------------------------------------------------------------------${NC}')
        for insight in insights:
            cat = insight.get('category', 'unknown')
            text = insight.get('insight', '')
            conf = insight.get('confidence', 0.0)
            print(f'  [${GREEN}{cat.upper()}${NC}] {text} (confidence: {conf:.2f})')
        print(f'\n${YELLOW}New insights this message: {len(insights)}${NC}')
    else:
        print('${YELLOW}No new insights extracted from this message.${NC}')
    print('')

except json.JSONDecodeError as e:
    print(f'${RED}JSON parsing error: {e}${NC}', file=sys.stderr)
    print(sys.stdin.read(), file=sys.stderr)
    sys.exit(1)
except Exception as e:
    print(f'${RED}Error: {e}${NC}', file=sys.stderr)
    sys.exit(1)
"

  if [ $? -ne 0 ]; then
    echo -e "${RED}Failed to process response${NC}"
    exit 1
  fi

  # Small delay between messages for rate limiting
  if [ $msg_num -lt ${#messages[@]} ]; then
    sleep 2
  fi
done

# Get full conversation history
echo ""
echo -e "${BLUE}======================================================================"
echo "RETRIEVING FULL CONVERSATION HISTORY"
echo -e "======================================================================${NC}"
echo ""

history=$(curl -s -X GET \
  "${API_URL}/entities/${ENTITY_ID}/conversation" \
  -H "x-functions-key: ${API_KEY}")

# Check for errors
if echo "$history" | grep -q '"error"'; then
  echo -e "${RED}ERROR retrieving history:${NC}"
  echo "$history" | python -m json.tool 2>/dev/null || echo "$history"
  exit 1
fi

# Parse and display history
echo "$history" | python -c "
import sys, json
from collections import defaultdict

try:
    data = json.load(sys.stdin)
    chunks = data.get('conversationChunks', [])
    insights = data.get('extractedInsights', [])

    print('${BLUE}======================================================================')
    print('FULL CONVERSATION HISTORY')
    print('======================================================================${NC}')
    print('')
    print('${BLUE}CONVERSATION:${NC}')
    print('${BLUE}----------------------------------------------------------------------${NC}')
    for chunk in chunks:
        speaker = chunk.get('speaker', 'unknown')
        text = chunk.get('text', '')
        if speaker == 'user':
            print(f'  [${YELLOW}USER${NC}] {text}')
        else:
            print(f'  [${GREEN}AI${NC}]   {text}')

    print('')
    print('${BLUE}======================================================================')
    print('ACCUMULATED INSIGHTS (grouped by category):')
    print('======================================================================${NC}')

    by_category = defaultdict(list)
    for insight in insights:
        cat = insight.get('category', 'unknown')
        text = insight.get('insight', '')
        conf = insight.get('confidence', 0.0)
        by_category[cat].append((text, conf))

    if by_category:
        for category, items in sorted(by_category.items()):
            print(f'\n${GREEN}{category.upper()}:${NC}')
            for text, conf in items:
                print(f'  • {text} (${YELLOW}{conf:.2f}${NC})')
    else:
        print('\n${YELLOW}No insights accumulated.${NC}')

    print('')
    print('${BLUE}======================================================================')
    print('STATISTICS:')
    print('======================================================================${NC}')
    print(f'  Total messages: ${GREEN}{len(chunks)}${NC}')
    print(f'  Total insights: ${GREEN}{len(insights)}${NC}')
    print(f'  Unique categories: ${GREEN}{len(by_category)}${NC}')

    if insights:
        avg_conf = sum(i.get('confidence', 0) for i in insights) / len(insights)
        print(f'  Average confidence: ${GREEN}{avg_conf:.2f}${NC}')

    print('${BLUE}======================================================================${NC}')

    sys.exit(0)

except json.JSONDecodeError as e:
    print(f'${RED}JSON parsing error: {e}${NC}', file=sys.stderr)
    print(sys.stdin.read(), file=sys.stderr)
    sys.exit(1)
except Exception as e:
    print(f'${RED}Error: {e}${NC}', file=sys.stderr)
    sys.exit(1)
"

if [ $? -ne 0 ]; then
  echo -e "${RED}Failed to process conversation history${NC}"
  exit 1
fi

# Optional cleanup
echo ""
read -p "Delete conversation and test entity from database? (y/n): " delete_data
if [ "$delete_data" = "y" ] || [ "$delete_data" = "Y" ]; then
  echo -e "${YELLOW}Deleting conversation...${NC}"
  delete_conv_response=$(curl -s -X DELETE \
    "${API_URL}/entities/${ENTITY_ID}/conversation" \
    -H "x-functions-key: ${API_KEY}")

  # Note: DELETE conversation returns 204 No Content on success (empty response)
  echo -e "${GREEN}✓ Conversation deleted${NC}"

  echo -e "${YELLOW}Deleting test entity...${NC}"
  delete_entity_response=$(curl -s -X DELETE \
    "${API_URL}/entities/${ENTITY_ID}" \
    -H "x-functions-key: ${API_KEY}")

  # Note: DELETE entity returns 204 No Content on success (empty response)
  echo -e "${GREEN}✓ Test entity deleted${NC}"
else
  echo -e "${BLUE}Data preserved. Entity ID: ${ENTITY_ID}${NC}"
fi

echo ""
echo -e "${GREEN}======================================================================"
echo "TEST COMPLETE"
echo -e "======================================================================${NC}"
