#!/bin/bash

echo "🔄 Restarting MCP connection..."

# Kill any existing mcp-remote processes
pkill -f "mcp-remote" 2>/dev/null || true
sleep 2

echo "🚀 Starting new mcp-remote connection..."
npx mcp-remote http://localhost:3000/mcp/message