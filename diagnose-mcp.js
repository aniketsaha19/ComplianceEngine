#!/usr/bin/env node

/**
 * MCP Server Diagnosis Tool
 * Tests the full MCP flow to identify why tools aren't showing in Claude Desktop
 */

const http = require('http');

async function testMcpFlow() {
  console.log('🔍 MCP Server Diagnosis Tool');
  console.log('==============================\n');

  // Test 1: Health Check
  console.log('1️⃣ Testing server health...');
  try {
    const health = await curl('http://localhost:3000/health');
    console.log('✅ Server is healthy');
    console.log(`   📦 Tools available: ${health.tools}`);
    console.log(`🔗 API URL: ${health.apiUrl}\n`);
  } catch (e) {
    console.log('❌ Server not responding on localhost:3000');
    console.log('   Make sure your MCP server is running\n');
    return;
  }

  // Test 2: MCP Initialize
  console.log('2️⃣ Testing MCP initialize...');
  try {
    const initResult = await mcpCall({
      jsonrpc: "2.0",
      id: 0,
      method: "initialize",
      params: {
        protocolVersion: "2024-11-05",
        capabilities: {},
        clientInfo: { name: "test-client", version: "1.0.0" }
      }
    });
    console.log('✅ MCP initialize successful');
    console.log(`   🏷️  Server: ${initResult.result.serverInfo.name} v${initResult.result.serverInfo.version}\n`);
  } catch (e) {
    console.log('❌ MCP initialize failed:', e.message + '\n');
    return;
  }

  // Test 3: MCP Tools List (Critical Test)
  console.log('3️⃣ Testing MCP tools/list (Critical)...');
  try {
    const toolsResult = await mcpCall({
      jsonrpc: "2.0", 
      id: 1,
      method: "tools/list"
    });

    if (!toolsResult.result || !toolsResult.result.tools) {
      console.log('❌ tools/list returned invalid response');
      console.log('   Response:', JSON.stringify(toolsResult, null, 2));
      return;
    }

    const tools = toolsResult.result.tools;
    console.log('✅ tools/list successful!');
    console.log(`   📦 Found ${tools.length} tools:\n`);
    
    tools.forEach((tool, i) => {
      console.log(`   ${String(i + 1).padStart(2)}. ${tool.name}`);
      console.log(`      ${tool.description}`);
      if (tool.inputSchema && tool.inputSchema.properties) {
        const required = tool.inputSchema.required || [];
        console.log(`      📝 Args: ${Object.keys(tool.inputSchema.properties).length} properties (${required.length} required)`);
      }
      console.log();
    });

    console.log('🎉 DIAGNOSIS RESULT:');
    console.log('===================');
    console.log('✅ Your MCP server is WORKING CORRECTLY');
    console.log('✅ Tools are properly exposed and documented');
    console.log('✅ Protocol implementation is solid');
    console.log('\n❓ Why aren\'t tools showing in Claude Desktop?');
    console.log('\nPossible causes:');
    console.log('1. 🔄 Connection timing - proxy connects but disconnects before Claude caches tools');
    console.log('2. 🔧 Claude Desktop cache - needs restart to refresh available tools');  
    console.log('3. 🌐 Network issues - mcp-remote proxy not staying connected');
    console.log('4. 📋 Configuration - wrong mcp-remote parameters or headers');
    
    console.log('\n🔧 SOLUTION STEPS:');
    console.log('1. Restart Claude Desktop completely');
    console.log('2. Use the updated CLAUDE_DESKTOP_CONFIG.json provided');
    console.log('3. Test connection with the mcp-remote command shown below');
    console.log('\n💡 MANUAL MCP-REMOTE TEST:');
    console.log('npx mcp-remote http://localhost:3000/mcp/message');
    
  } catch (e) {
    console.log('❌ MCP tools/list failed:', e.message);
    console.log('This is likely why tools aren\'t showing in Claude Desktop\n');
    
    if (e.message.includes('ECONNREFUSED')) {
      console.log('🔧 Fix: Make sure your MCP server is running on port 3000');
    }
  }
}

// Helper: HTTP POST request
function mcpCall(data) {
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify(data);
    
    const req = http.request({
      hostname: 'localhost',
      port: 3000,
      path: '/mcp/message',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(postData)
      }
    }, (res) => {
      let response = '';
      res.on('data', chunk => response += chunk);
      res.on('end', () => {
        try {
          const parsed = JSON.parse(response);
          resolve(parsed);
        } catch (e) {
          reject(new Error('Invalid JSON response: ' + response));
        }
      });
    });

    req.on('error', reject);
    req.write(postData);
    req.end();
  });
}

// Helper: GET request  
function curl(url) {
  return new Promise((resolve, reject) => {
    const urlObj = new URL(url);
    const req = http.request({
      hostname: urlObj.hostname,
      port: urlObj.port,
      path: urlObj.pathname,
      method: 'GET'
    }, (res) => {
      let response = '';
      res.on('data', chunk => response += chunk);
      res.on('end', () => {
        try {
          resolve(JSON.parse(response));
        } catch (e) {
          reject(e);
        }
      });
    });

    req.on('error', reject);
    req.end();
  });
}

testMcpFlow().catch(console.error);