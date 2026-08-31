// Portfolio Management Tools for Compliance Engine MCP Server

import { v4 as uuidv4 } from 'uuid';

// Create portfolio tool
export async function createPortfolio(ComplianceEngineAPI, portfolioData) {
  try {
    const result = await ComplianceEngineAPI.createPortfolio(portfolioData.name);
    
    return {
      status: "PORTFOLIO_CREATED",
      portfolio: {
        id: result.portfolioId,
        name: result.name,
        createdAt: new Date().toISOString()
      },
      nextStep: "Use add_holdings_to_portfolio or connect_trading_data to populate holdings"
    };
  } catch (error) {
    return {
      status: "PORTFOLIO_CREATION_FAILED",
      error: error.message,
      recommendation: "Ensure compliance engine is running and you have proper API key"
    };
  }
}

// Bulk create portfolios
export async function createPortfolios(ComplianceEngineAPI, portfolioStructures) {
  const createdPortfolios = [];
  const errors = [];
  
  for (const structure of portfolioStructures) {
    try {
      const result = await ComplianceEngineAPI.createPortfolio(structure.name);
      createdPortfolios.push({
        id: result.portfolioId,
        name: structure.name,
        description: structure.description,
        strategy: structure.strategy
      });
    } catch (error) {
      errors.push({
        name: structure.name,
        error: error.message
      });
    }
  }
  
  return {
    status: createdPortfolios.length > 0 ? "PORTFOLIOS_CREATED" : "PORTFOLIO_CREATION_FAILED",
    createdPortfolios: createdPortfolios,
    errors: errors,
    summary: `${createdPortfolios.length} portfolios created successfully`
  };
}

// Organize holdings into portfolios
export async function organizeHoldingsToPortfolios(ComplianceEngineAPI, TradingMCPServer, portfolioMappings) {
  try {
    const holdings = await TradingMCPServer.getHoldings();
    const organizedHoldings = {};
    
    // Initialize portfolio arrays
    for (const mapping of portfolioMappings) {
      organizedHoldings[mapping.portfolioName] = [];
    }
    
    // Organize holdings according to mappings
    for (const holding of holdings) {
      for (const mapping of portfolioMappings) {
        if (shouldIncludeHolding(holding, mapping.criteria)) {
          organizedHoldings[mapping.portfolioName].push(holding);
        }
      }
    }
    
    // Create portfolios and assign holdings
    const results = [];
    for (const [portfolioName, portfolioHoldings] of Object.entries(organizedHoldings)) {
      try {
        const portfolio = await ComplianceEngineAPI.createPortfolio(portfolioName);
        
        // Here you would add logic to actually assign holdings to the portfolio
        // This depends on how the original trading platform structures this data
        
        results.push({
          portfolioId: portfolio.portfolioId,
          portfolioName: portfolioName,
          holdingsCount: portfolioHoldings.length,
          estimatedValue: portfolioHoldings.reduce((sum, h) => sum + (h.currentValue || 0), 0)
        });
      } catch (error) {
        results.push({
          portfolioName: portfolioName,
          status: "FAILED",
          error: error.message
        });
      }
    }
    
    return {
      status: "ORGANIZATION_COMPLETE",
      portfolios: results,
      originalHoldings: holdings.length,
      allocation: Object.fromEntries(
        Object.entries(organizedHoldings).map(([name, holdings]) => [name, holdings.length])
      )
    };
  } catch (error) {
    return {
      status: "ORGANIZATION_FAILED",
      error: error.message
    };
  }
}

// Get portfolio overview
export async function getPortfolioOverview(ComplianceEngineAPI, TradingMCPServer, portfolioId) {
  try {
    // Get portfolio from compliance engine
    const portfolios = await ComplianceEngineAPI.listPortfolios();
    const portfolio = portfolios.find(p => p.id === portfolioId);
    
    if (!portfolio) {
      throw new Error(`Portfolio ${portfolioId} not found`);
    }
    
    // Get holdings from compliance engine
    const complianceHoldings = await ComplianceEngineAPI.getHoldings(portfolioId);
    
    // Get live market data
    let liveMarketData = null;
    if (TradingMCPServer.baseUrl) {
      try {
        const tickers = complianceHoldings.holdings?.map(h => h.ticker) || [];
        if (tickers.length > 0) {
          liveMarketData = await TradingMCPServer.getQuotes(tickers);
        }
      } catch (error) {
        console.log("Could not fetch live market data:", error);
      }
    }
    
    // Calculate analysis
    const analysis = calculatePortfolioHealth(complianceHoldings.holdings, liveMarketData);
    
    return {
      status: "OVERVIEW_COMPLETE",
      portfolio: {
        id: portfolioId,
        name: portfolio.name,
        createdAt: portfolio.createdAt,
        holdingsCount: complianceHoldings.holdings?.length || 0
      },
      health: analysis.health,
      risks: analysis.risks,
      opportunities: analysis.opportunities,
      marketData: liveMarketData ? "Live data available" : "Using cached values",
      nextAction: analysis.actionRequired
    };
  } catch (error) {
    return {
      status: "OVERVIEW_FAILED",
      error: error.message
    };
  }
}

// Helper function to determine if holding should be included in portfolio
function shouldIncludeHolding(holding, criteria) {
  if (criteria.minValue && holding.currentValue < criteria.minValue) return false;
  if (criteria.maxValue && holding.currentValue > criteria.maxValue) return false;
  if (criteria.sectors && !criteria.sectors.includes(holding.sector)) return false;
  if (criteria.tickers && !criteria.tickers.includes(holding.ticker)) return false;
  
  return true;
}

// Calculate portfolio health metrics
function calculatePortfolioHealth(holdings, liveMarketData) {
  if (!holdings || holdings.length === 0) {
    return {
      health: "empty",
      risks: ["No holdings found"],
      opportunities: ["Add positions to begin monitoring"],
      actionRequired: "Populate portfolio with holdings"
    };
  }
  
  const totalValue = holdings.reduce((sum, h) => sum + h.marketValue, 0);
  const largestPositionPercent = Math.max(...holdings.map(h => (h.marketValue / totalValue) * 100));
  const sectorCount = new Set(holdings.map(h => h.sector)).size;
  
  const risks = [];
  const opportunities = [];
  
  // Risk assessment
  if (largestPositionPercent > 10) {
    risks.push("High concentration risk - largest position > 10%");
  }
  if (sectorCount < 3) {
    risks.push("Low sector diversification");
  }
  if (holdings.length < 10) {
    risks.push("Limited position count - consider adding more holdings");
  }
  
  // Opportunity assessment
  if (largestPositionPercent < 5) {
    opportunities.push("Well diversified portfolio");
  }
  if (sectorCount >= 5) {
    opportunities.push("Good sector diversity");
  }
  
  // Determine overall health
  let healthScore = 100;
  healthScore -= risks.length * 15;
  healthScore += opportunities.length * 5;
  
  let health = "good";
  if (healthScore < 70) health = "moderate";
  if (healthScore < 50) health = "poor";
  
  return {
    health,
    healthScore,
    risks,
    opportunities,
    actionRequired: risks.length > 2 ? "Consider rebalancing" : "Portfolio in good shape"
  };
}

export default {
  createPortfolio,
  createPortfolios,
  organizeHoldingsToPortfolios,
  getPortfolioOverview
};