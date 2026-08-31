import express from 'express';

const COMPLIANCE_API_URL = process.env.COMPLIANCE_API_URL || 'http://localhost:5071';
const PORT = process.env.PORT || 3000;

const app = express();
app.use(express.json());

// Available tools
const tools = [
  {
    name: "create_portfolio",
    description: "Create a new portfolio for the authenticated tenant",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Name of the portfolio to create" }
      },
      required: ["name"]
    }
  },
  {
    name: "list_portfolios", 
    description: "List all portfolios for the authenticated tenant",
    inputSchema: { type: "object", properties: {} }
  },
  {
    name: "add_rule",
    description: "Add a new compliance rule for the tenant",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Human-readable name for the rule" },
        description: { type: "string", description: "Description of what the rule enforces" },
        rule_type: { 
          type: "string", 
          enum: ["max_position_pct", "max_sector_pct", "min_holdings_count"],
          description: "Type of compliance rule" 
        },
        threshold: { type: "number", description: "Threshold value for the rule" }
      },
      required: ["name", "rule_type", "threshold"]
    }
  },
  {
    name: "list_rules",
    description: "List all rules for the authenticated tenant", 
    inputSchema: { type: "object", properties: {} }
  },
  {
    name: "get_holdings",
    description: "Get current holdings for a specific portfolio",
    inputSchema: {
      type: "object",
      properties: {
        portfolio_id: { type: "integer", description: "Portfolio ID to get holdings for" }
      },
      required: ["portfolio_id"]
    }
  },
  {
    name: "check_compliance", 
    description: "Get compliance summary for a specific portfolio",
    inputSchema: {
      type: "object",
      properties: {
        portfolio_id: { type: "integer", description: "Portfolio ID to check compliance for" }
      },
      required: ["portfolio_id"]
    }
  },
  {
    name: "check_trade_compliance",
    description: "Check if a proposed trade would violate any compliance rules.",
    inputSchema: {
      type: "object",
      properties: {
        portfolio_id: { type: "integer", description: "Portfolio ID to check trade against" },
        ticker: { type: "string", description: "Stock ticker symbol" },
        sector: { type: "string", description: "Sector of the security" },
        action: { type: "string", enum: ["BUY", "SELL"], description: "Trade action type" },
        quantity: { type: "number", description: "Number of shares" },
        price: { type: "number", description: "Price per share" }
      },
      required: ["portfolio_id", "ticker", "action", "quantity", "price"]
    }
  },
  {
    name: "record_trade", 
    description: "Record an executed trade and update portfolio.",
    inputSchema: {
      type: "object",
      properties: {
        portfolio_id: { type: "integer", description: "Portfolio ID to record trade in" },
        ticker: { type: "string", description: "Stock ticker symbol" },
        sector: { type: "string", description: "Sector of the security" },
        action: { type: "string", enum: ["BUY", "SELL"], description: "Trade action type" },
        quantity: { type: "number", description: "Number of shares" },
        price: { type: "number", description: "Price per share" }
      },
      required: ["portfolio_id", "ticker", "action", "quantity", "price"]
    }
  }
];

// Health check
app.get('/health', (req, res) => {
  console.log(`🔍 Health check at ${new Date().toISOString()}`);
  res.json({ 
    status: 'healthy',
    timestamp: new Date().toISOString(),
    version: '1.0.0',
    server: 'Compliance Engine MCP (Node.js)'
  });
});

// SSE endpoint (both GET and POST for mcp-remote compatibility)
app.all('/sse', (req, res) => {
  console.log(`🔗 ${req.method} /sse request`);
  
  if (req.method === 'GET') {
    // SET headers for SSE
    res.writeHead(200, {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache',
      'Connection': 'keep-alive',
      'Access-Control-Allow-Origin': '*'
    });

    // Send initial message
    res.write(`data: ${JSON.stringify({
      jsonrpc: "2.0",
      method: "notifications/initialized",
      params: {}
    })}\n\n`);

    // Keep alive
    const keepAlive = setInterval(() => {
      res.write(`event: ping\ndata: ${JSON.stringify({timestamp: Date.now()})}\n\n`);
    }, 25000);

    req.on('close', () => {
      clearInterval(keepAlive);
      console.log('🔚 SSE connection closed');
    });
    
  } else if (req.method === 'POST') {
    // Handle JSON-RPC 2.0 requests
    console.log('📨 SSE POST request:', JSON.stringify(req.body, null, 2));
    
    const { jsonrpc, id, method, params } = req.body;

    if (method === 'initialize') {
      console.log('🤝 MCP Initialize via SSE/POST');
      res.json({
        jsonrpc: "2.0",
        id,
        result: {
          protocolVersion: "2024-11-05",
          serverInfo: {
            name: "compliance-engine-mcp",
            version: "1.0.0"
          },
          capabilities: {
            tools: { listChanged: false },
            logging: {}
          }
        }
      });
    } 
    else if (method === 'tools/list') {
      console.log('🔧 List tools via SSE/POST');
      res.json({
        jsonrpc: "2.0",
        id,
        result: { tools }
      });
    }
    else if (method === 'tools/call') {
      console.log(`🔧 Tool call via SSE/POST: ${params?.name}`, params?.arguments);
      
      const { name, arguments: args = {} } = params;
      const apiKey = process.env.COMPLIANCE_API_KEY || 'demo-token';
      
      executeTool(name, args, apiKey)
        .then(result => {
          console.log('✅ Tool result via SSE/POST:', JSON.stringify(result, null, 2));
          res.json({
            jsonrpc: "2.0",
            id,
            result: {
              content: [{
                type: "text",
                text: JSON.stringify(result, null, 2)
              }],
              isError: !result.success
            }
          });
        })
        .catch(error => {
          console.error('Tool execution failed via SSE/POST:', error);
          res.json({
            jsonrpc: "2.0",
            id,
            error: {
              code: -32603,
              message: `Tool execution failed: ${error.message}`
            }
          });
        });
    }
    else if (method === 'ping') {
      console.log('🏓 Ping handled via SSE/POST');
      res.json({
        jsonrpc: "2.0",
        id,
        result: { pong: true }
      });
    }
    else {
      console.log(`❓ Unknown method via SSE/POST: ${method}`);
      res.json({
        jsonrpc: "2.0",
        id,
        error: {
          code: -32601,
          message: `Method not found: ${method}`
        }
      });
    }
  }
});

app.post('/mcp/message', (req, res) => {
  console.log('📨 MCP message:', JSON.stringify(req.body, null, 2));
  
  const { jsonrpc, id, method, params } = req.body;

  try {
    if (method === 'initialize') {
      console.log('🤝 MCP Initialize');
      res.json({
        jsonrpc: "2.0",
        id,
        result: {
          protocolVersion: "2024-11-05",
          serverInfo: {
            name: "compliance-engine-mcp",
            version: "1.0.0"
          },
          capabilities: {
            tools: { listChanged: false },
            logging: {}
          }
        }
      });
    }
    else if (method === 'tools/list') {
      console.log('🔧 List tools');
      res.json({
        jsonrpc: "2.0", 
        id,
        result: { tools }
      });
    }
    else if (method === 'tools/call') {
      console.log(`🔧 Tool call: ${params?.name}`, params?.arguments);
      
      const { name, arguments: args = {} } = params;
      const apiKey = process.env.COMPLIANCE_API_KEY || 'demo-token';
      
      executeTool(name, args, apiKey)
        .then(result => {
          console.log('✅ Tool result:', JSON.stringify(result, null, 2));
          res.json({
            jsonrpc: "2.0",
            id,
            result: {
              content: [{
                type: "text",
                text: JSON.stringify(result, null, 2)
              }],
              isError: !result.success
            }
          });
        })
        .catch(error => {
          console.error('Tool execution failed:', error);
          res.json({
            jsonrpc: "2.0",
            id,
            error: {
              code: -32603,
              message: `Tool execution failed: ${error.message}`
            }
          });
        });
    }
    else if (method === 'ping') {
      console.log('🏓 Ping received');
      res.json({
        jsonrpc: "2.0",
        id,
        result: { pong: true, timestamp: Date.now() }
      });
    }
    else {
      console.log(`❓ Unknown method: ${method}`);
      res.json({
        jsonrpc: "2.0",
        id,
        error: {
          code: -32601,
          message: `Method not found: ${method}`
        }
      });
    }
  } catch (error) {
    console.error('MCP processing error:', error);
    res.json({
      jsonrpc: "2.0",
      id,
      error: {
        code: -32603,
        message: `Internal error: ${error.message}`
      }
    });
  }
});

// Tool execution
async function executeTool(toolName, args, apiKey) {
  console.log(`🔧 Executing ${toolName}`);
  
  try {
    const headers = {
      'Authorization': `Bearer ${apiKey}`,
      'Content-Type': 'application/json'
    };

    // Mock responses for demonstration
    switch (toolName) {
      case 'create_portfolio':
        return {
          success: true,
          data: {
            portfolio_id: Math.floor(Math.random() * 1000) + 1,
            name: args.name,
            created: new Date().toISOString()
          }
        };
        
      case 'list_portfolios':
        return {
          success: true,
          data: [
            { portfolio_id: 1, name: "Demo Portfolio", created: "2024-01-01" }
          ]
        };
        
      case 'add_rule':
        return {
          success: true,
          data: {
            rule_id: Math.floor(Math.random() * 100) + 1,
            name: args.name,
            rule_type: args.rule_type,
            threshold: args.threshold
          }
        };
        
      case 'list_rules':
        return {
          success: true,
          data: [
            {
              rule_id: 1,
              name: "Max 10% position",
              rule_type: "max_position_pct",
              threshold: 10,
              active: true
            }
          ]
        };
        
      case 'get_holdings':
        return {
          success: true,
          data: {
            portfolio_id: args.portfolio_id,
            holdings: [
              { symbol: "AAPL", shares: 100, price: 150.00, value: 15000.00, sector: "Technology" },
              { symbol: "MSFT", shares: 50, price: 300.00, value: 15000.00, sector: "Technology" }
            ]
          }
        };
        
      case 'check_compliance':
        return {
          success: true,
          data: {
            portfolio_id: args.portfolio_id,
            compliant: true,
            breaches: [],
            violations: []
          }
        };
        
      case 'check_trade_compliance':
        return {
          success: true,
          data: {
            allowed: true,
            breaches: [],
            would_be_compliant: true,
            new_position_value: args.quantity * args.price
          }
        };
        
      case 'record_trade':
        return {
          success: true,
          data: {
            trade_id: Math.floor(Math.random() * 10000) + 1,
            portfolio_id: args.portfolio_id,
            ticker: args.ticker,
            action: args.action,
            quantity: args.quantity,
            price: args.price,
            status: "EXECUTED",
            timestamp: new Date().toISOString()
          }
        };
        
      default:
        return {
          success: false,
          error: `Unknown tool: ${toolName}`,
          available_tools: tools.map(t => t.name)
        };
    }
  } catch (error) {
    return {
      success: false,
      error: `Tool execution failed: ${error.message}`,
      tool: toolName
    };
  }
}

// Start server
const server = app.listen(PORT, () => {
  console.log(`🚀 Compliance Engine MCP Server running on port ${PORT}`);
  console.log(`📍 Health: http://localhost:${PORT}/health`);
  console.log(`🔗 MCP SSE: http://localhost:${PORT}/sse`);
  console.log(`📨 MCP Messages: http://localhost:${PORT}/mcp/message`);
  console.log(`🛠️  Available tools: ${tools.length}`);
  console.log(`🎯 Ready for mcp-remote connection\n`);
});

// Graceful shutdown
process.on('SIGTERM', () => {
  console.log('🛑 Shutting down server...');
  server.close(() => {
    process.exit(0);
  });
});

export default app;