#!/bin/bash

# Compliance Engine MCP Gateway - Production Startup
# This starts the MCP HTTP gateway that proxies to your .NET engine

set -e

echo "🚀 Starting Compliance Engine MCP Gateway"
echo "========================================"

# Kill existing processes
pkill -f "node.*mcp-server" 2>/dev/null || echo "✅ Clean startup"

# Set environment variables
export COMPLIANCE_API_URL="http://localhost:5071"
export API_KEY="sgX6SSIhaQcC8MhOPcWW85QalWZXUA1B"  # Demo tenant API key
export PORT=3000

echo "🔧 Configuration:"
echo "   📍 .NET Engine: $COMPLIANCE_API_URL"
echo "   🏷️  Port: $PORT"
echo "   🗝️  Auth: Bearer token configured"
echo ""

# Navigate to MCP server directory
cd /Users/aniketsaha/Desktop/ComplianceEngine/mcp-server

# Test .NET engine connection
echo "🔍 Testing .NET engine connection..."
if curl -s -f -X GET "${COMPLIANCE_API_URL}/portfolios" \
  -H "Authorization: Bearer ${API_KEY}" > /dev/null; then
  echo "✅ .NET engine connection: OK"
else
  echo "⚠️  .NET engine not responding - continue anyway"
fi

# Start MCP gateway
echo ""
echo "🚀 Starting MCP Gateway..."
cd src
node index.js &

sleep 3

# Test MCP gateway
echo ""
echo "🧪 Testing MCP Gateway..."

# Test health
if curl -s -f http://localhost:3000/health > /dev/null; then
  echo "✅ MCP Gateway health: OK"
else
  echo "❌ MCP Gateway health: FAILED"
  exit 1
fi

# Test MCP protocol
if curl -s -X POST http://localhost:3000/mcp/message \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}' > /dev/null; then
  echo "✅ MCP Protocol: OK"
else
  echo "❌ MCP Protocol: FAILED"
  exit 1
fi

echo ""
echo "🎯 MCP Gateway Ready!"
echo "==================="
echo "📍 Health: http://localhost:3000/health"
echo "🔗 MCP: http://localhost:3000/mcp/message"
echo "🛠️  Tools: 8 available (no hardcoded data)"
echo ""
echo "✅ Ready for Claude Desktop!"
echo ""
echo "🔗 CLAUDE DESKTOP CONFIG:"
echo '{'
echo '  "mcpServers": {'
echo '    "compliance-engine": {'
echo '      "command": "npx",'
echo '      "args": ["mcp-remote", "http://localhost:3000/mcp/message"]'
echo '    }'
echo '  }'
echo '}'
echo ""
echo "🛑 Stop server: pkill -f 'node.*index.js'"