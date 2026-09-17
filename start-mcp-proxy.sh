#!/bin/bash

echo "🚀 Starting persistent MCP proxy connection..."

cd /Users/aniketsaha/Desktop/ComplianceEngine

# Kill any existing connections
pkill -f "mcp-remote" 2>/dev/null || true
sleep 3

# Start the proxy connection and keep it alive
echo "🔗 Establishing MCP proxy connection..."
(
  exec npx mcp-remote http://localhost:3000/mcp/message
) &

MCP_PID=$!
echo "📋 MCP Proxy PID: $MCP_PID"

# Wait a moment for connection to establish
sleep 5

# Test if connection is working
if curl -s http://localhost:3000/health | grep -q "ok"; then
    echo "✅ MCP Server is running"
    echo "🔧 Tools available: $(curl -s http://localhost:3000/health | jq -r '.tools')"
    echo ""
    echo "🎯 To restart this connection manually, run:"
    echo "   ./start-mcp-proxy.sh"
    echo ""
    echo "🔄 Proxy process: $MCP_PID"
    echo "⏹️  To stop: kill $MCP_PID"
else
    echo "❌ MCP Server not responding"
    kill $MCP_PID 2>/dev/null
fi