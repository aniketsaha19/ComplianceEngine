#!/bin/bash

# COMPLETE SETUP: Compliance Engine + Node.js MCP Server
# This script starts both the .NET compliance engine and the Node.js MCP server

set -e

echo "🚀 COMPREHENSIVE MCP SERVER SETUP"
echo "=================================="

# Step 1: Kill any existing processes
echo "🛑 Step 1: Stopping existing processes..."
pkill -f "node|dotnet" 2>/dev/null || true
sleep 3
echo "✅ Processes cleaned"

# Step 2: Start compliance engine (.NET)
echo "🔧 Step 2: Starting Compliance Engine (.NET)..."
cd engine
export ASPNETCORE_ENVIRONMENT="Development"
export ASPNETCORE_URLS="http://localhost:5071"
export PORT="5071"
# Start in background, don't wait for it
dotnet run > /dev/null 2>&1 &
DOTNET_PID=$!
cd ..

echo "⏳ Waiting for compliance engine to initialize..."
# Wait for the engine to be ready (with timeout)
for i in {1..15}; do
    if curl -s http://localhost:5071/health >/dev/null 2>&1; then
        echo "✅ Compliance engine ready (PID: $DOTNET_PID)"
        break
    fi
    echo "   ⏳ Waiting... ($i/15)"
    sleep 2
done

# Step 3: Start MCP server (Node.js)
echo "🌐 Step 3: Starting MCP Server (Node.js)..."
cd mcp-server
export COMPLIANCE_API_URL="http://localhost:5071"
export PORT=3000
export COMPLIANCE_API_KEY="demo-token"
# Start in background, but give it a moment
npm start &
MCP_PID=$!
cd ..

sleep 3
echo "✅ MCP server started (PID: $MCP_PID)"

# Step 4: Show status
echo ""
echo "🎯 SETUP COMPLETE!"
echo "=================="
echo "📍 Compliance Engine: http://localhost:5071 (PID: $DOTNET_PID)"
echo "📡 MCP Server: http://localhost:3000 (PID: $MCP_PID)" 
echo "🔗 MCP SSE Endpoint: http://localhost:3000/sse"
echo "📨 MCP Message Endpoint: http://localhost:3000/mcp/message"
echo ""
echo "Available tools:"
echo "- create_portfolio, list_portfolios"
echo "- add_rule, list_rules"
echo "- get_holdings, check_compliance"
echo "- check_trade_compliance, record_trade"
echo ""
echo "💡 Claude Desktop Configuration:"
echo '{'
echo '  "mcpServers": {'
echo '    "compliance-engine-local": {'
echo '      "command": "npx",'
echo '      "args": ["mcp-remote", "http://localhost:3000/sse"]'
echo '    }'
echo '  }'
echo '}'
echo ""
echo "🛑 To stop both servers:"
echo "kill -9 $DOTNET_PID $MCP_PID"
echo ""
echo "🔍 Health checks:"
echo "- Compliance Engine: curl http://localhost:5071/health"
echo "- MCP Server: curl http://localhost:3000/health"
echo ""

# Do NOT wait - let the script exit while servers run in background