#!/bin/bash

# Test script to verify the holdings endpoint circular reference fix

echo "=== CLEANUP ==="
pkill -9 -f "dotnet" 2>/dev/null || true
sleep 2

echo "=== STARTING ENGINE ==="
cd engine
dotnet run --urls="http://localhost:5150" > /tmp/engine.log 2>&1 &
ENGINE_PID=$!
echo "Engine PID: $ENGINE_PID"
echo "Waiting for engine to start..."

# Wait for engine to be ready
for i in {1..30}; do
    if curl -s -f "http://localhost:5150" > /dev/null 2>&1; then
        echo "✅ Engine is running on port 5150"
        break
    fi
    echo "⏳ Waiting... ($i/30)"
    sleep 1
done

echo
echo "=== TESTING HOLDINGS ENDPOINT (Portfolio 11) ==="
echo "Testing: GET /portfolio/11/holdings"
echo "Expected: No circular reference error"
echo
curl -s -H "accept: application/json" "http://localhost:5150/portfolio/11/holdings" | python3 -m json.tool 2>/dev/null || curl -s -H "accept: application/json" "http://localhost:5150/portfolio/11/holdings"

echo
echo "=== TESTING COMPLIANCE ENDPOINT (Portfolio 17) ==="  
echo "Testing: GET /portfolio/17/compliance-summary"
curl -s -H "accept: application/json" "http://localhost:5150/portfolio/17/compliance-summary" | python3 -m json.tool 2>/dev/null || curl -s -H "accept: application/json" "http://localhost:5150/portfolio/17/compliance-summary"

echo
echo "=== TESTING DIFFERENT HOLDINGS (Portfolio 18) ==="
curl -s -H "accept: application/json" "http://localhost:5150/portfolio/18/holdings" | python3 -m json.tool 2>/dev/null || curl -s -H "accept: application/json" "http://localhost:5150/portfolio/18/holdings"

echo
echo "=== CLEANUP ==="
kill $ENGINE_PID 2>/dev/null || true
echo "Engine stopped"