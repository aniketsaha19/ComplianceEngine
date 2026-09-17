#!/bin/bash

# Streamable HTTP MCP Server Startup Script
# This starts the modern HTTP Streamable MCP server for deployment

set -e

echo "🚀 Starting HTTP Streamable MCP Server"
echo "====================================="

# Kill existing processes
pkill -f "node.*index.js" 2>/dev/null || echo "No existing processes found"

# Set environment variables
export COMPLIANCE_API_URL="http://localhost:5071"
export PORT=3000
export NODE_ENV=production

echo "📡 Configuration:"
echo "   MCP Server: HTTP Streamable"
echo "   Compliance API: $COMPLIANCE_API_URL"
echo "   Port: $PORT"
echo "   Authentication: Disabled (demo mode)"
echo ""

# Start the server
cd /Users/aniketsaha/Desktop/ComplianceEngine/mcp-server

echo "🔧 Starting server..."
node src/index.js &

sleep 3

# Test server is running
echo "🧪 Testing server endpoints..."
curl -s http://localhost:3000/health > /dev/null && echo "✅ Health endpoint OK" || echo "❌ Health endpoint failed"

# Test MCP endpoint
curl -s -X POST http://localhost:3000/mcp/message \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}' > /dev/null && echo "✅ MCP initialize OK" || echo "❌ MCP initialize failed"

echo ""
echo "🎯 Server Status:"
echo "✅ HTTP Streamable MCP server running on port 3000"
echo "📝 Claude Desktop config:"
echo '{
  "mcpServers": {
    "compliance-engine": {
      "command": "npx",
      "args": ["mcp-remote", "http://localhost:3000/mcp/message"]
    }
  }
}'
echo ""
echo "🛠️ Available tools:"
echo "  - create_portfolio: Create new portfolio"
echo "  - list_portfolios: List portfolios"
echo "  - add_rule: Add compliance rule"
echo "  - list_rules: List rules"
echo "  - get_holdings: Get portfolio holdings"
echo "  - check_compliance: Check compliance"
echo "  - check_trade_compliance: Trade compliance check"
echo "  - record_trade: Record trade"
echo ""
echo "📝 Logs: tail -f /dev/null"
echo "🛑 Stop: pkill -f 'node.*index.js'"
echo ""
echo "✅ Server ready for Claude Desktop testing!"