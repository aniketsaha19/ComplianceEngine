#!/bin/bash

echo "🚀 FINAL MCP SETUP - Complete the deployment"
echo "=========================================="

echo ""
echo "📋 STEP 1: Update Claude Desktop config"
echo "Copy this to: ~/Library/Application Support/Claude/claude_desktop_config.json"
echo ""
cat << 'EOF'
{
  "mcpServers": {
    "compliance-engine": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "http://localhost:3000/mcp/message",
        "--header", "Authorization: Bearer DEMO_TENANT:sgX6SSIhaQcC8MhOPcWW85QalWZXUA1B"
      ],
      "env": {
        "MCP_DEBUG": "true"
      }
    }
  }
}
EOF

echo ""
echo "📋 STEP 2: Ensure MCP server is running"
if curl -s http://localhost:3000/health | grep -q "ok"; then
    echo "✅ MCP server is running on port 3000"
else
    echo "❌ MCP server not running. Start it with:"
    echo "   cd /Users/aniketsaha/Desktop/ComplianceEngine/mcp-server/src && node index.js"
fi

echo ""
echo "📋 STEP 3: Test the connection manually"
echo "Run this command to test:"
echo "   node diagnose-mcp.js"

echo ""
echo "📋 STEP 4: Restart Claude Desktop"
echo "After updating the config file, completely quit and restart Claude Desktop"

echo ""
echo "📋 STEP 5: Verify tools are available"
echo "Once restarted, look for 'compliance-engine' tools in the MCP tools panel"

echo ""
echo "🔧 AVAILABLE TOOL COMMANDS TO TEST:"
echo "- 'Create a portfolio called Test Portfolio'"  
echo "- 'Show my compliance rules'"
echo "- 'Check compliance for my portfolios'"
echo "- 'Add a max 10% position size rule'"

echo ""
echo "✅ Your compliance engine should now be accessible worldwide!"