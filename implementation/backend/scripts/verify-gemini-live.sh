#!/usr/bin/env bash
set -euo pipefail

API_KEY="${GEMINI_API_KEY:-${TITAN_API_KEY:-${AI_LLM_API_KEY:-YOUR_API_KEY_HERE}}}"
BASE_URL="${AI_LLM_BASE_URL:-https://generativelanguage.googleapis.com/v1beta/openai/}"
MODEL="${AI_LLM_MODEL:-gemini-2.5-flash}"

ENDPOINT="${BASE_URL%/}/chat/completions"

echo "=========================================================="
echo "Testing Gemini OpenAI-Compatible Endpoint Connectivity"
echo "Endpoint: $ENDPOINT"
echo "Model:    $MODEL"
echo "=========================================================="

HTTP_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}\n" -X POST "$ENDPOINT" \
  -H "Authorization: Bearer $API_KEY" \
  -H "Content-Type: application/json" \
  -d "{
    \"model\": \"$MODEL\",
    \"messages\": [
      {\"role\": \"system\", \"content\": \"You are a test ping responder. Respond strictly with JSON.\"},
      {\"role\": \"user\", \"content\": \"Respond with JSON: {\\\"status\\\": \\\"ok\\\", \\\"timestamp\\\": 123456}\"}
    ],
    \"response_format\": {\"type\": \"json_object\"},
    \"temperature\": 0.0
  }")

BODY=$(echo "$HTTP_RESPONSE" | sed -e '/HTTP_STATUS:/d')
STATUS=$(echo "$HTTP_RESPONSE" | tr -d '\n' | sed -e 's/.*HTTP_STATUS://')

echo "HTTP Status Code: $STATUS"
echo "Response Body:"
echo "$BODY"
echo "=========================================================="

if [ "$STATUS" -eq 200 ]; then
  echo "SUCCESS: Successfully connected to Gemini OpenAI-compatible API and received HTTP 200 OK."
  exit 0
elif [ "$STATUS" -eq 429 ]; then
  echo "INFO: Connected to Gemini API successfully (HTTP 429 Rate Limit/Quota Exceeded from Google API)."
  exit 0
else
  echo "ERROR: Unexpected status code $STATUS received from Gemini API."
  exit 1
fi
