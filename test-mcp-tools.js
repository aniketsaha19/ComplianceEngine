#!/usr/bin/env node

/**
 * Test script to verify MCP server tools discovery
 */

const { spawn } = require('child_process');

console.log('🔍 Testing MCP Tools Discovery...\n');

// Test mcp-remote connection
const mcpRemote = spawn('npx', ['mcp-remote', 'http://localhost:3000/mcp/message'], {
  stdio: ['pipe', 'pipe', 'pipe']
});

let output = '';
let error = '';

mcpRemote.stdout.on('data', (data) => {
  output += data.toString();
  process.stdout.write(data);
});

mcpRemote.stderr.on('data', (data) => {
  error += data.toString();
  process.stderr.write(data);
});

// Send initialize
setTimeout(() => {
  console.log('\n📤 Sending initialize...');
  mcpRemote.stdin.write(JSON.stringify({
    jsonrpc: "2.0",
    id: 1,
    method: "initialize",
    params: {
      protocolVersion: "2024-11-05",
      capabilities: {}
    }
  }) + '\n');
}, 1000);

// Send tools/list
setTimeout(() => {
  console.log('\n📤 Sending tools/list...');
  mcpRemote.stdin.write(JSON.stringify({
    jsonrpc: "2.0",
    id: 2,
    method: "tools/list"
  }) + '\n');
}, 2000);

// Exit after test
setTimeout(() => {
  console.log('\n🧪 Test completed');
  mcpRemote.kill();
  process.exit(0);
}, 5000);

mcpRemote.on('close', (code) => {
  console.log(`\n🔗 Process exited with code: ${code}`);
});