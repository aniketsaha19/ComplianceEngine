# 📋 Compliance Engine - Complete Code Map & Documentation

**Architecture Overview**: .NET Core Compliance Engine API + Node.js MCP Server + Claude Desktop Integration

---

## 🏗️ **PROJECT STRUCTURE**

```
ComplianceEngine/
├── 📄 README.md                                    # Main project documentation
├── ⚙️  global.json                               # .NET global settings
├── 📂 engine/                                    # .NET Core Compliance Engine API
├── 📂 mcp-server/                                # Node.js MCP Server
├── 📂 data/                                      # Data files and loaders
```

---

## 🚀 **ROOT LEVEL FILES**

### **Core Configuration & Scripts**



#### **`global.json`** (45B)
- **Purpose**: .NET global settings
- **Content**: SDK version and rollForward configuration

---

## 🔧 **ENGINE DIRECTORY** (.NET Core 7.0 Web API)

### **Purpose**: Main Compliance Engine API providing portfolio and rule management services

### **Core Application Files**

#### **`Program.cs`** (833B)
- **Purpose**: Application startup and dependency injection configuration
- **Functionality**:
  - WebApplication builder setup
  - Entity Framework Core configuration
  - Service registration (RuleEvaluationService)
  - Custom middleware configuration
  - Swagger documentation setup
  - Routing configuration

#### **`ComplianceEngine.csproj`** (1.1KB)
- **Purpose**: .NET Core project configuration file
- **Dependencies**:
  - Entity Framework Core
  - ASP.NET Core
  - SQL Server connectivity
  - Swagger/SwaggerUI
  - Custom middleware

#### **`appsettings.json`** (313B)
- **Purpose**: Application configuration settings
- **Configuration**:
  - Connection strings for SQL Server
  - Environment-specific settings
  - Logging configuration

#### **`appsettings.Development.json`** (127B)
- **Purpose**: Development environment overrides
- **Content**: Development-specific configuration

### **Controllers** (API Endpoints)

#### **`Controllers/PortfolioController.cs`** (8.8KB)
- **Purpose**: Portfolio management API endpoints
- **Key Endpoints**:
  - `GET /api/portfolios` - List all portfolios
  - `POST /api/portfolios` - Create new portfolio
  - `GET /api/portfolios/{id}/holdings` - Get portfolio holdings
  - `POST /api/portfolios/{id}/trades` - Record trade
  - `GET /api/portfolios/{id}/compliance-summary` - Compliance check
  - `GET /api/portfolios/{id}/trade-check` - Pre-trade compliance check
- **Authentication**: Tenant-based authentication via middleware

#### **`Controllers/RulesController.cs`** (6KB)
- **Purpose**: Compliance rules management API
- **Key Endpoints**:
  - `GET /api/rules` - List all rules
  - `POST /api/rules` - Create new rule
  - `GET /api/rules/{id}` - Get specific rule
  - `PUT /api/rules/{id}` - Update rule
  - `DELETE /api/rules/{id}` - Delete rule
- **Rule Types**: max_position_pct, max_sector_pct, min_holdings_count

#### **`Controllers/TenantsController.cs`** (3KB)
- **Purpose**: Tenant management API
- **Key Endpoints**:
  - `POST /api/tenants/authenticate` - Tenant authentication
  - `POST /api/tenants/register` - Register new tenant
  - `GET /api/tenants/profile` - Get tenant profile

### **Data Layer**

#### **`Data/` Directory**
- **Purpose**: Data access layer with Entity Framework Core
- **Key Components**:
  - `ComplianceDbContext.cs` - Main database context
  - Migration files for database schema updates

#### **`Models/` Directory**
- **Purpose**: Entity models for database and API
- **Key Models**:
  - **`Portfolio.cs`** (337B) - Portfolio entity with holdings
  - **`Holding.cs`** (506B) - Individual holding within portfolio
  - **`Rule.cs`** (511B) - Compliance rule definition
  - **`Trade.cs`** (576B) - Trade transaction record
  - **`Tenant.cs`** (432B) - Multi-tenant user/customer
  - **`RuleEvaluation.cs`** (542B) - Rule evaluation results

### **Services Layer**

#### **`Services/RuleEvaluationService.cs`** (21.5KB)
- **Purpose**: Core compliance logic and rule evaluation engine
- **Key Functionality**:
  - Rule evaluation against current holdings
  - Real-time compliance checking for proposed trades
  - Violation detection and reporting
  - Threshold enforcement
- **Integration**: Used by controllers for compliance operations

### **Middleware**

#### **`Middleware/` Directory**
- **Purpose**: Custom middleware for tenant authentication
- **Key Components**:
  - Tenant authentication middleware
  - API key validation
  - Multi-tenant data isolation

---

## 🔗 **MCP-SERVER DIRECTORY** (Node.js MCP Server)

### **Purpose**: HTTP MCP Server providing Claude Desktop integration

### **Core Implementation**

#### **`mcp-server/src/simple-mcp-server.js`** (13KB)
- **Purpose**: Primary working MCP server implementation
- **Purpose**: HTTP transport MCP server for Claude Desktop
- **Dependencies**: `express@4.18.0` only
- **Key Features**:
  - JSON-RPC 2.0 protocol compliance
  - HTTP/SSE transport for mcp-remote
  - 8 compliance management tools
  - Proper error handling with standard codes

#### **Available MCP Tools:**

1. **`create_portfolio`** - Create new portfolio
   - Input: `{ name: "string" }`
   - Returns: Portfolio creation confirmation

2. **`list_portfolios`** - List tenant portfolios
   - Input: `{}` (no parameters)
   - Returns: Array of portfolio objects

3. **`add_rule`** - Add compliance rule
   - Input: `{ name, description, rule_type, threshold }`
   - Returns: Rule creation confirmation

4. **`list_rules`** - List tenant rules
   - Input: `{}` (no parameters)
   - Returns: Array of rule objects

5. **`get_holdings`** - Get portfolio holdings
   - Input: `{ portfolio_id: number }`
   - Returns: Holdings array with detailed information

6. **`check_compliance`** - Check portfolio compliance
   - Input: `{ portfolio_id: number }`
   - Returns: Compliance summary with violations

7. **`check_trade_compliance`** - Pre-trade compliance check
   - Input: `{ portfolio_id, ticker, sector, action, quantity, price }`
   - Returns: Trade feasibility assessment

8. **`record_trade`** - Record executed trade
   - Input: `{ portfolio_id, ticker, action, quantity, price }`
   - Returns: Trade confirmation with compliance check

### **MCP Server Endpoints**

#### **`GET /health`**
- **Purpose**: Health check endpoint
- **Returns**: Server status, version, timestamp

#### **`GET /sse`** (Server-Sent Events)
- **Purpose**: SSE connection for mcp-remote
- **Functionality**: Maintains persistent connection for bidirectional communication

#### **`POST /sse`** 
- **Purpose**: MCP protocol initialization and tool calls
- **Handles**: initialize, tools/list, tools/call, ping

#### **`POST /mcp/message`**
- **Purpose**: Direct MCP message endpoint
- **Format**: JSON-RPC 2.0 requests and responses

### **Legacy Implementations** (Not active)
- **`compliance_engine_mcp.py`** (13KB) - Python MCP server
- **`server.py`** (13KB) - Python HTTP server
- **`src/index.js`**, **`src/index-mcp.js`**, **`src/http-mcp-server.js`**, **`src/sdk-http-mcp-server.js`**, **`src/stdio-mcp-server.js`** - Multiple Node.js implementations

#### **`mcp-server/package.json`** (393B)
- **Purpose**: Minimal Node.js package configuration
- **Dependencies**: `"express": "^4.18.0"` only
- **Scripts**: Start and development scripts

#### **`mcp-server/README.md`** (8KB)
- **Purpose**: MCP server-specific documentation
- **Content**: Setup instructions, API documentation, troubleshooting

#### **`mcp-server/.mcp.json`** (1.8KB)
- **Purpose**: MCP server configuration metadata
- **Content**: Server capabilities, tool definitions, protocol version

#### **`mcp-server/start-server.sh`** (978B)
- **Purpose**: Standalone server startup script (legacy)
- **Usage**: Single server startup without compliance engine

---

## 📊 **DATA & SUPPORTING FILES**

### **`data/` Directory**
- **Purpose**: Data files and potential data loaders
- **Content**: Configuration data, sample data, or data migration files

### **`docs/` Directory**
- **Purpose**: Additional project documentation
- **Content**: Extended documentation, API references, deployment guides

### **`.vscode/` Directory**
- **Purpose**: Visual Studio Code workspace configuration
- **Files**:
  - `settings.json` - Workspace settings
  - `tasks.json` - Build and run tasks

### **`.reasonix/` Directory**
- **Purpose**: Reasonix development tool configuration and data
- **Content**: Tool configurations, task data, development metadata

---

## 🔄 **SYSTEM ARCHITECTURE**

### **Data Flow**

1. **Claude Desktop** → **mcp-remote** via HTTP/SSE
2. **mcp-remote** → **Node.js MCP Server** on port 3000
3. **Node.js MCP Server** → **.NET Compliance Engine** on port 5071
4. **.NET Compliance Engine** → **SQL Server** database

### **Technology Stack**

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Backend API** | .NET Core 7.0 | Main business logic, portfolio management |
| **Database** | SQL Server | Persistent storage for portfolios, rules, holds |
| **MCP Server** | Node.js + Express | Protocol bridge for Claude Desktop |
| **Client Connection** | HTTP/SSE | Transport layer for MCP protocol |
| **AI Integration** | Claude Desktop | User interface and AI reasoning |

### **Port Configuration**

- **5071**: .NET Compliance Engine API
- **3000**: Node.js MCP Server
- **Claude Desktop**: Connects via local HTTP/SSE

---

## 🔧 **SETUP & DEPLOYMENT**

### **Prerequisites**
- .NET Core 7.0+
- Node.js 18+
- SQL Server (development/LocalDB)
- Claude Desktop application

### **Quick Start**
```bash
# 1. Start both servers
./START-BOTH-SERVERS.sh

# 2. Configure Claude Desktop
# Copy WORKING-CLAUDE-DESKTOP-CONFIG.json to Claude Desktop config

# 3. Test endpoints
curl http://localhost:3000/health  # MCP Server
curl http://localhost:5071/health  # Compliance Engine
```

### **Development Workflow**
```bash
# API Development
cd engine && dotnet run

# MCP Server Development  
cd mcp-server && npm start

# Health Checks
curl http://localhost:3000/health
curl http://localhost:5071/health
```

---

## 🧪 **TESTING & VALIDATION**

### **MCP Tools Testing**
- All 8 tools implement proper JSON-RPC 2.0
- Input validation through defined schemas  
- Comprehensive error handling
- Mock responses for development/demo mode

### **API Testing**
- Swagger documentation at `/swagger`
- Health check endpoints on both servers
- Authentication middleware validation
- Database connectivity verification

---

## 📈 **SCALABILITY & PERFORMANCE**

### **Current Implementation**
- Single-process API server
- In-memory session management
- Direct database connections
- HTTP/SSE for real-time communication

### **Scalability Considerations**
- Multi-tenant architecture ready
- Database connection pooling possible
- Horizontal scaling of MCP servers
- Load balancing for API endpoints

---

## 🛡️ **SECURITY FEATURES**

### **Authentication**
- Multi-tenant authentication middleware
- API key validation for MCP connections
- Tenant isolation in database queries

### **Data Protection**
- Encrypted database connections
- Input validation on all endpoints
- SQL injection prevention via Entity Framework

---

## 🎯 **CURRENT STATUS**

| Component | Status | Purpose |
|-----------|---------|---------|
| **Compliance Engine (.NET)** | ✅ Complete | Full portfolio & rule management |
| **MCP Server (Node.js)** | ✅ Complete | Ready for Claude Desktop |
| **Database Schema** | ✅ Complete | All entities defined |
| **API Endpoints** | ✅ Complete | Swagger-documented |
| **MCP Tools** | ✅ Complete | 8 tools functional |
| **Claude Desktop Integration** | ✅ Complete | Configured & tested |

**Total Implementation**: 50+ files, 2-server architecture, production-ready Compliance Engine with MCP protocol integration.

---

## 📝 **MAINTENANCE NOTES**

- **Active Server**: `simple-mcp-server.js` (single-file implementation)
- **Legacy Files**: Multiple experimental implementations (safe to ignore)
- **Configuration**: Environment-based configuration for deployment
- **Documentation**: Comprehensive API docs via Swagger UI
- **Monitoring**: Health check endpoints for both servers