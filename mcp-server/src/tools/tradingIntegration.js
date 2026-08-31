// Trading Integration Tools for Compliance Engine MCP Server

import { v4 as uuidv4 } from 'uuid';

// Initialize trading connection tool
export async function initializeTradingConnection(tradingUrl, apiKey) {
  if (!tradingUrl || !apiKey) {
    return {
      status: "CONFIGURATION_INVALID",
      error: "Both tradingUrl and apiKey are required",
      recommendation: "Provide valid Trading MCP server URL and API key"
    };
  }
  
  return {
    status: "CONFIGURATION_COMPLETE",
    tradingUrl,
    initializedAt: new Date().toISOString(),
    nextStep: "Use test_trading_connection to verify the setup"
  };
}

// Test trading connection tool
export async function testTradingConnection(serverUrl, apiKey) {
  try {
    const toolName = "Get profile"; // Common tool for testing connection
    
    const response = await fetch(`${serverUrl}/tools/${toolName}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`
      },
      body: JSON.stringify({})
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    const profile = await response.json();
    
    return {
      status: "CONNECTION_SUCCESS",
      profile: {
        broker: profile.broker || profile.brokerName,
        accountType: profile.accountType,
        tradingEnabled: profile.tradingEnabled !== false,
        userId: profile.userId || profile.clientId,
        serverType: profile.serverType || "Trading MCP"
      },
      message: `Successfully connected to ${profile.broker || 'Trading MCP server'}`,
      capabilities: [
        "Get holdings",
        "Get positions", 
        "Get quotes",
        "Get orders",
        "Get trades",
        "Place orders", // If available
        "Cancel orders" // If available
      ]
    };
  } catch (error) {
    return {
      status: "CONNECTION_FAILED",
      error: error.message,
      troubleshooting: [
        "Verify Trading MCP server URL is correct",
        "Confirm API key is valid and not expired", 
        "Check that the server is running and accessible",
        "Review network/firewall settings"
      ],
      recommendation: "Double-check your Trading MCP server configuration and try again"
    };
  }
}

// Sync holdings from trading platform
export async function syncHoldingsFromTrading(TradingMCPServer, compliancePortfolioId) {
  try {
    const syncId = uuidv4();
    const startTime = Date.now();
    
    // Fetch holdings from trading platform
    console.log("Fetching holdings from Trading MCP server...");
    const tradingHoldings = await TradingMCPServer.getHoldings();
    
    // Fetch positions for additional context
    let positions = null;
    try {
      positions = await TradingMCPServer.getPositions();
    } catch (error) {
      console.log("Could not fetch positions:", error.message);
    }
    
    const syncTime = Date.now() - startTime;
    
    return {
      status: "SYNC_COMPLETE",
      syncId: syncId,
      syncTime: syncTime,
      holdingsCount: tradingHoldings.length,
      totalValue: tradingHoldings.reduce((sum, h) => sum + (h.currentValue || h.value || 0), 0),
      sectors: getSectorBreakdown(tradingHoldings),
      positions: positions ? `${positions.length} positions` : "Positions not available",
      holdings: tradingHoldings,
      lastSync: new Date().toISOString(),
      nextStep: `Use analyze_portfolio_risk to assess these holdings or create_portfolios to organize them`
    };
  } catch (error) {
    return {
      status: "SYNC_FAILED",
      error: error.message,
      recommendation: "Check Trading MCP server connection and ensure you have holdings access"
    };
  }
}

// Get real-time market quotes
export async function getRealtimeMarketQuotes(TradingMCPServer, tickers) {
  try {
    if (!tickers || tickers.length === 0) {
      throw new Error("No tickers provided for quote lookup");
    }
    
    const quoteResults = [];
    
    // Get quotes for each ticker
    for (const ticker of tickers) {
      try {
        const quote = await TradingMCPServer.getLTP(ticker); // Last traded price
        quoteResults.push({
          ticker: ticker,
          price: quote.price || quote.ltp,
          timestamp: quote.timestamp || new Date().toISOString(),
          source: "Trading MCP Server",
          availability: quote.price ? "available" : "price_not_available"
        });
      } catch (error) {
        quoteResults.push({
          ticker: ticker,
          error: error.message,
          availability: "failed_to_fetch"
        });
      }
    }
    
    // Get batch quotes if supported
    try {
      const batchQuotes = await TradingMCPServer.getQuotes(tickers);
      return {
        status: "QUOTES_COMPLETE", 
        quotes: batchQuotes, // Use batch quotes if available
        individualResults: quoteResults,
        timestamp: new Date().toISOString(),
        source: "Trading MCP Server",
        note: "Using batch quote API for efficiency"
      };
    } catch (error) {
      // Fallback to individual quotes
      return {
        status: "QUOTES_COMPLETE",
        quotes: quoteResults,
        timestamp: new Date().toISOString(),
        source: "Trading MCP Server",
        note: "Using individual quote calls - consider enabling batch quotes for efficiency"
      };
    }
  } catch (error) {
    return {
      status: "QUOTES_FAILED",
      error: error.message,
      recommendation: "Check Trading MCP server connectivity and ticker validity"
    };
  }
}

// Analyze trading patterns
export async function analyzeTradingPatterns(TradingMCPServer, days = 30) {
  try {
    console.log(`Analyzing trading patterns for last ${days} days...`);
    
    // Get order history
    const orders = await TradingMCPServer.getOrderHistory();
    const trades = await TradingMCPServer.getTrades(); // If available
    
    // Parse last N days of activity
    const cutoff = new Date(Date.now() - days * 24 * 60 * 60 * 1000);
    const recentOrders = orders.filter(order => new Date(order.orderDate) >= cutoff);
    const recentTrades = trades ? trades.filter(trade => new Date(trade.tradeDate) >= cutoff) : [];
    
    // Analyze patterns
    const analysis = {
      totalOrders: recentOrders.length,
      totalTrades: recentTrades.length,
      tradingDays: new Set(recentOrders.map(o => new Date(o.orderDate).toDateString())).size,
      avgOrdersPerDay: recentOrders.length / Math.max(1, days),
      sectors: {},
      tickers: {},
      tradingBehavior: {},
      riskAssessment: {}
    };
    
    // Sector analysis
    for (const order of recentOrders) {
      const sector = order.sector || "Unknown";
      if (!analysis.sectors[sector]) analysis.sectors[sector] = 0;
      analysis.sectors[sector]++;
      
      const ticker = order.ticker || "Unknown";
      if (!analysis.tickers[ticker]) analysis.tickers[ticker] = 0;
      analysis.tickers[ticker]++;
    }
    
    // Trading behavior assessment
    const avgOrderSize = recentTrades.length > 0 ? 
      recentTrades.reduce((sum, t) => sum + t.quantity * t.price, 0) / recentTrades.length : 0;
    
    analysis.tradingBehavior = {
      avgOrderValue: Math.round(avgOrderSize),
      mostTradedSector: Object.keys(analysis.sectors).length > 0 ? 
        Object.keys(analysis.sectors).reduce((a, b) => analysis.sectors[a] > analysis.sectors[b] ? a : b) : "N/A",
      mostTradedTicker: Object.keys(analysis.tickers).length > 0 ? 
        Object.keys(analysis.tickers).reduce((a, b) => analysis.tickers[a] > analysis.tickers[b] ? a : b) : "N/A",
      diversification: Object.keys(analysis.sectors).length
    };
    
    // Risk assessment
    analysis.riskAssessment = {
      concentrationRisk: analysis.tradingBehavior.mostTradedTicker === "N/A" || 
        (analysis.tickers[analysis.tradingBehavior.mostTradedTicker] || 0) > (recentOrders.length * 0.3),
      sectorDiversification: analysis.tradingBehavior.diversification >= 3 ? "good" : "limited",
      tradingFrequency: analysis.avgOrdersPerDay >= 0.5 ? "active" : analysis.avgOrdersPerDay >= 0.1 ? "moderate" : "low"
    };
    
    // Compliance rule suggestions based on patterns
    const ruleSuggestions = [];
    if (analysis.riskAssessment.concentrationRisk) {
      ruleSuggestions.push({
        type: "max_position_pct",
        suggestion: "Limit single position to prevent over-concentration",
        reasoning: "Analysis shows high activity in single ticker"
      });
    }
    if (analysis.riskAssessment.sectorDiversification === "limited") {
      ruleSuggestions.push({
        type: "max_sector_pct", 
        suggestion: "Limit sector exposure for diversification",
        reasoning: "Trading concentrated in few sectors"
      });
    }
    if (analysis.avgOrdersPerDay >= 2) {
      ruleSuggestions.push({
        type: "trading_volume_limit",
        suggestion: "Set daily trading volume limits for risk management",
        reasoning: "High frequency trading detected"
      });
    }
    
    return {
      status: "ANALYSIS_COMPLETE",
      analysisPeriod: days,
      dataPeriod: `Last ${days} days`,
      tradingPatterns: analysis,
      ruleSuggestions: ruleSuggestions,
      recommendations: [
        `${analysis.avgOrdersPerDay.toFixed(1)} orders per day on average`,
        `Trading primarily in ${analysis.tradingBehavior.mostTradedSector} sector`,
        analysis.riskAssessment.concentrationRisk ? 
          "⚠️ Consider rules to limit position concentration" : "✅ Good position diversification",
        analysis.riskAssessment.sectorDiversification === "good" ?
          "✅ Adequate sector diversification" : "⚠️ Consider rules to improve sector diversification"
      ],
      nextStep: "Use interactive_rule_setup to create compliance rules based on these patterns"
    };
  } catch (error) {
    return {
      status: "ANALYSIS_FAILED",
      error: error.message,
      recommendation: "Ensure Trading MCP server provides order and trade history data"
    };
  }
}

// Helper function for sector breakdown
function getSectorBreakdown(holdings) {
  const sectors = {};
  let totalValue = 0;
  
  for (const holding of holdings) {
    const sector = holding.sector || 'Unknown';
    const value = holding.currentValue || holding.value || 0;
    
    if (!sectors[sector]) {
      sectors[sector] = { value: 0, count: 0 };
    }
    
    sectors[sector].value += value;
    sectors[sector].count += 1;
    totalValue += value;
  }
  
  // Convert to percentages
  const breakdown = {};
  for (const [sector, data] of Object.entries(sectors)) {
    breakdown[sector] = {
      ...data,
      percentageOfPortfolio: (data.value / totalValue) * 100
    };
  }
  
  return breakdown;
}

export default {
  initializeTradingConnection,
  testTradingConnection,
  syncHoldingsFromTrading,
  getRealtimeMarketQuotes,
  analyzeTradingPatterns
};