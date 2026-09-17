const http = require('http');

// Test MCP tools/list call
function testMcpToolsList() {
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify({
      jsonrpc: "2.0",
      id: 1,
      method: "tools/list"
    });

    const options = {
      hostname: 'localhost',
      port: 3000,
      path: '/mcp/message',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(postData),
        'Connection': 'close'
      }
    };

    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => {
        try {
          const result = JSON.parse(data);
          resolve(result);
        } catch (e) {
          reject(e);
        }
      });
    });

    req.on('error', reject);
    req.write(postData);
    req.end();
  });
}

// Test MCP initialize call  
function testMcpInitialize() {
  return new Promise((resolve, reject) => {
    const postData = JSON.stringify({
      jsonrpc: "2.0",
      id: 0,
      method: "initialize",
      params: {
        protocolVersion: "2024-11-05",
        capabilities: {},
        clientInfo: {
          name: "test-client",
          version: "1.0.0"
        }
      }
    });

    const options = {
      hostname: 'localhost',
      port: 3000,
      path: '/mcp/message',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(postData),
        'Connection': 'close'
      }
    };

    const req = http.request(options, (res) => {
      let data = '';
      res.on('data', (chunk) => { data += chunk; });
      res.on('end', () => {
        try {
          const result = JSON.parse(data);
          resolve(result);
        } catch (e) {
          reject(e);
        }
      });
    });

    req.on('error', reject);
    req.write(postData);
    req.end();
  });
}

async function runTests() {
  console.log('🧪 Testing MCP Server...\n');
  
  try {
    console.log('1️⃣ Testing MCP initialize...');
    const initResult = await testMcpInitialize();
    console.log('✅ Initialize successful');
    console.log('Server info:', JSON.stringify(initResult.result.serverInfo, null, 2));
    console.log('');
    
    console.log('2️⃣ Testing MCP tools/list...');
    const toolsResult = await testMcpToolsList();
    console.log('✅ Tools list successful');
    console.log('📦 Found', toolsResult.result.tools.length, 'tools:');
    
    toolsResult.result.tools.forEach((tool, i) => {
      console.log(`   ${i+1}. ${tool.name} - ${tool.description}`);
    });
    
    console.log('\n🎉 MCP Server is working correctly!');
    console.log('🔧 Tools are properly exposed for Claude Desktop');
    
  } catch (error) {
    console.error('❌ MCP Test failed:', error.message);
  }
}

runTests();
