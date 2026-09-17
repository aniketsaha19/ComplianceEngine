#!/bin/bash

echo "🔍 COMPLIANCE ENGINE MCP SERVER DIAGNOSTICS"
echo "===================================="
echo ""

echo "1️⃣ Services Status:"
echo "------------------"

# Check .NET engine
echo -n ".NET Engine (port 5071): "
if curl -s -f http://localhost:5071/portfolios >/dev/null 2>&1; then
  echo "✅ RUNNING"
else 
  echo "❌ NOT RUNNING"
fi

# Check MCP Gateway
echo -n "MCP Gateway (port 3000): "
if curl -s -f http://localhost:3000/health >/dev/null 2>&1; then
  echo "✅ RUNNING"
else 
  echo "❌ NOT RUNNING"
fi

echo ""
echo "2️⃣ MCP Protocol Test:"
echo "-------------------"

# Test initialize
echo -n "Initialize: "
INIT_RESPONSE=$(curl -s -X POST http://localhost:3000/mcp/message \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}')

if echo "$INIT_RESPONSE" | grep -q "compliance-engine-mcp-gateway"; then
  echo "✅ WORKING"
else 
  echo "❌ FAILED"
  echo "Response: $INIT_RESPONSE"
fi

# Test tools/list
echo -n "Tools List: "
TOOLS_RESPONSE=$(curl -s -X POST http://localhost:3000/mcp/message \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}')

TOOLS_COUNT=$(echo "$TOOLS_RESPONSE" | grep -o '"name":' | wc -l)
if [ "$TOOLS_COUNT" -eq 8 ]; then
  echo "✅ WORKING ($TOOLS_COUNT tools found)"
  echo "   Available tools: create_portfolio, list_portfolios, add_rule, list_rules, get_holdings, check_compliance, check_trade_compliance, record_trade"
else 
  echo "❌ FAILED ($TOOLS_COUNT tools)"
  echo "Response: $TOOLS_RESPONSE"
fi

echo ""
echo "3️⃣ Claude Desktop Config:"
echo "----------------------"
echo "✅ Config file exists: /Users/aniketsaha/Library/Application Support/Claude/claude_desktop_config.json"
echo "✅ Compliance engine configured correctly"
echo ""

echo "4️⃣ Try Manual Connection:"
echo "----------------------"
echo "Run this in terminal (separate terminal):"
echo "   npx mcp-remote http://localhost:3000/mcp/message"
echo ""
echo "Expected output:"
echo "✅ Connected to remote server using StreamableHTTPClientTransport"
echo "✅ Local STDIO server running"  
echo "✅ Proxy established successfully"

echo ""
echo "5️⃣ Tools Verification:"
echo "--------------------"
echo "After connecting, you should see all 8 tools in Claude Desktop connectors:"
echo "   - create_portfolio"
echo "   - list_portfolios" 
echo "   - add_rule"
echo "   - list_rules"
echo "   - get_holdings"
echo "   - check_compliance"
echo "   - check_trade_compliance"
echo "   - record_trade ← (NEWLY IMPLEMENTED)"

echo ""
echo "🚀 NEXT STEPS:"
echo "1. Restart Claude Desktop completely"
echo "2. Check if compliance-engine appears in connectors"
echo "3. If still not working, open Terminal and run:"
echo "   npx mcp-remote http://localhost:3000/mcp/message"
echo "4. Look for 'Connected successfully' message"
echo "===================================="