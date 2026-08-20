-- Create a compliant portfolio for breach demonstration
-- Query to identify a safe baseline for portfolio creation

SELECT TOP 1 Ticker, Sector, ClosePrice
FROM MarketPriceHistory h
INNER JOIN (
    SELECT Ticker, MAX(TradeDate) AS MaxDate
    FROM MarketPriceHistory  
    WHERE Ticker NOT IN (SELECT DISTINCT Ticker FROM Holdings WHERE PortfolioId IN (20,29,32,35))
    GROUP BY Ticker
) latest ON h.Ticker = latest.Ticker AND h.TradeDate = latest.MaxDate
WHERE h.Ticker NOT IN (SELECT Ticker FROM Holdings)
ORDER BY h.Ticker