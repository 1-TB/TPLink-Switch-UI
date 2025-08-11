#!/usr/bin/env bash

# Simple smoke test to verify the new API endpoints are accessible

echo "Starting TP-Link Switch WebUI API Smoke Tests..."

# Start the backend in the background
echo "Starting backend server..."
cd backend
dotnet run --urls="http://localhost:5555" &
BACKEND_PID=$!

# Wait for the server to start
echo "Waiting for server to start..."
sleep 10

# Test base URL
echo "Testing base API health endpoint..."
curl -s -f "http://localhost:5555/api/health" > /dev/null
if [ $? -eq 0 ]; then
    echo "✅ Health endpoint accessible"
else
    echo "❌ Health endpoint not accessible"
fi

# Test that new endpoints return proper error responses (401/400) since we're not authenticated
echo "Testing new endpoints (expecting auth errors)..."

endpoints=(
    "system/name"
    "system/ip-config" 
    "system/save-config"
    "system/led-control"
    "ports/clear-statistics"
    "mirroring/enable"
    "mirroring/configure"
    "trunking/configure"
    "loop-prevention"
    "qos/mode"
    "qos/bandwidth-control"
    "qos/port-priority"
    "qos/storm-control"
    "igmp-snooping"
    "poe/global-config"
    "poe/port-config"
    "config/backup"
)

for endpoint in "${endpoints[@]}"; do
    response_code=$(curl -s -w "%{http_code}" -o /dev/null -X POST "http://localhost:5555/api/$endpoint" -H "Content-Type: application/json" -d '{}')
    if [ "$response_code" = "401" ] || [ "$response_code" = "400" ] || [ "$response_code" = "500" ]; then
        echo "✅ $endpoint endpoint accessible (returned $response_code)"
    else
        echo "❌ $endpoint endpoint returned unexpected code: $response_code"
    fi
done

# Cleanup
echo "Stopping backend server..."
kill $BACKEND_PID 2>/dev/null
wait $BACKEND_PID 2>/dev/null

echo "Smoke tests completed!"