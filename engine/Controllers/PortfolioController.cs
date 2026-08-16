using ComplianceEngine.Data;
using ComplianceEngine.Models;
using ComplianceEngine.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComplianceEngine.Controllers;

[ApiController]
[Route("portfolio")]
public class PortfolioController : ControllerBase
{
    private readonly ComplianceDbContext _db;
    private readonly RuleEvaluationService _evaluator;

    public PortfolioController(ComplianceDbContext db, RuleEvaluationService evaluator)
    {
        _db = db;
        _evaluator = evaluator;
    }

    [HttpGet("{id}/holdings")]
    public async Task<IActionResult> GetHoldings(int id)
    {
        var portfolio = await _db.Portfolios.FindAsync(id);
        if (portfolio == null) return NotFound(new { error = $"Portfolio {id} not found." });

        var holdings = await _db.Holdings.Where(h => h.PortfolioId == id).ToListAsync();
        return Ok(new { portfolioId = id, portfolioName = portfolio.Name, holdings });
    }

    [HttpGet("{id}/compliance-summary")]
    public async Task<IActionResult> GetComplianceSummary(int id)
    {
        var portfolio = await _db.Portfolios.FindAsync(id);
        if (portfolio == null) return NotFound(new { error = $"Portfolio {id} not found." });

        var results = await _evaluator.EvaluatePortfolioAsync(id);
        return Ok(new
        {
            portfolioId = id,
            portfolioName = portfolio.Name,
            evaluatedAt = DateTime.UtcNow,
            compliant = !results.Any(r => r.Breached),
            rules = results
        });
    }
}

public class PositionChangeRequest
{
    public int PortfolioId { get; set; }
    public string Ticker { get; set; } = "";
    public string Sector { get; set; } = "";
    public string Action { get; set; } = ""; // BUY | SELL
    public decimal Quantity { get; set; }
}

[ApiController]
[Route("")]
public class PositionController : ControllerBase
{
    private readonly ComplianceDbContext _db;
    private readonly RuleEvaluationService _evaluator;

    public PositionController(ComplianceDbContext db, RuleEvaluationService evaluator)
    {
        _db = db;
        _evaluator = evaluator;
    }

    [HttpPost("position-change")]
    public async Task<IActionResult> PostPositionChange([FromBody] PositionChangeRequest request)
    {
        var portfolio = await _db.Portfolios.FindAsync(request.PortfolioId);
        if (portfolio == null) return NotFound(new { error = $"Portfolio {request.PortfolioId} not found." });

        var holding = await _db.Holdings
            .FirstOrDefaultAsync(h => h.PortfolioId == request.PortfolioId && h.Ticker == request.Ticker);

        if (holding == null)
        {
            if (request.Action != "BUY")
                return BadRequest(new { error = "Cannot SELL a position that doesn't exist." });
            holding = new Holding
            {
                PortfolioId = request.PortfolioId,
                Ticker = request.Ticker,
                Sector = request.Sector,
                Quantity = request.Quantity
            };
            _db.Holdings.Add(holding);
        }
        else
        {
            holding.Quantity += request.Action == "BUY" ? request.Quantity : -request.Quantity;
            if (holding.Quantity < 0)
                return BadRequest(new { error = "Resulting quantity cannot be negative." });
        }

        await _db.SaveChangesAsync();
        var results = await _evaluator.EvaluatePortfolioAsync(request.PortfolioId);

        return Ok(new
        {
            portfolioId = request.PortfolioId,
            updatedTicker = request.Ticker,
            compliant = !results.Any(r => r.Breached),
            rules = results
        });
    }
}