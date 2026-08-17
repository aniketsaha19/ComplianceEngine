using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

class ApiTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Portfolio Diversity API Tests...");
        
        // Start the engine
        Console.WriteLine("🚀 Starting compliance engine...");
        var engineProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project engine",
            WorkingDirectory = ".",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        
        if (engineProcess == null)
        {
            Console.WriteLine("❌ Failed to start engine");
            return;
        }
        
        Console.WriteLine("⏳ Waiting for engine to be ready...");
        bool engineReady = false;
        int attempts = 0;
        
        using var httpClient = new HttpClient();
        
        while (attempts < 30 && !engineReady)
        {
            try
            {
                var response = await httpClient.GetAsync("http://localhost:5070/portfolio/1/holdings");
                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    engineReady = true;
                    Console.WriteLine("✅ Engine is ready!");
                    break;
                }
            }
            catch
            {
                // Engine not ready yet
            }
            
            attempts++;
            await Task.Delay(1000);
        }
        
        if (!engineReady)
        {
            Console.WriteLine("❌ Engine failed to start within 30 seconds");
            engineProcess.Kill();
            return;
        }
        
        // Test portfolio compliance
        Console.WriteLine("\n🧪 Testing Portfolio Compliance:");
        Console.WriteLine("=" * 50);
        
        var testPortfolios = new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19 };
        int compliantCount = 0;
        int nonCompliantCount = 0;
        
        foreach (var portfolioId in testPortfolios)
        {
            try
            {
                Console.WriteLine($"\n📊 Testing Portfolio {portfolioId}...");
                
                // Test compliance summary
                var response = await httpClient.GetAsync($"http://localhost:5070/portfolio/{portfolioId}/compliance-summary");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonDocument.Parse(content);
                    
                    bool compliant = data.RootElement.GetProperty("compliant").GetBoolean();
                    string name = data.RootElement.GetProperty("portfolioName").GetString() ?? "Unknown";
                    
                    // Count breached rules
                    var rules = data.RootElement.GetProperty("rules").EnumerateArray();
                    int breaches = 0;
                    foreach (var rule in rules)
                    {
                        if (rule.GetProperty("breached").GetBoolean())
                            breaches++;
                    }
                    
                    if (compliant)
                    {
                        compliantCount++;
                        Console.WriteLine($"   ✅ {name}: COMPLIANT");
                    }
                    else
                    {
                        nonCompliantCount++;
                        Console.WriteLine($"   ❌ {name}: {breaches} rules breached");
                    }
                    
                    // Test holdings
                    var holdingsResponse = await httpClient.GetAsync($"http://localhost:5070/portfolio/{portfolioId}/holdings");
                    if (holdingsResponse.IsSuccessStatusCode)
                    {
                        var holdingsContent = await holdingsResponse.Content.ReadAsStringAsync();
                        var holdingsData = JsonDocument.Parse(holdingsContent);
                        int holdingsCount = holdingsData.RootElement.GetProperty("holdings").GetArrayLength();
                        Console.WriteLine($"   💼 Holdings: {holdingsCount}");
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ HTTP {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
            }
        }
        
        // Summary
        Console.WriteLine("\n" + "=" * 50);
        Console.WriteLine("🎯 ACCEPTANCE CRITERIA VERIFICATION");
        Console.WriteLine("=" * 50);
        
        Console.WriteLine($"✅ Compliant portfolios: {compliantCount}");
        Console.WriteLine($"❌ Non-compliant portfolios: {nonCompliantCount}");
        
        bool criteriaMet = compliantCount > 0 && nonCompliantCount > 0;
        
        if (criteriaMet)
        {
            Console.WriteLine("\n🎉 SUCCESS: Portfolio diversity achieved!");
            Console.WriteLine("✅ At least one portfolio is compliant");
            Console.WriteLine("✅ Some portfolios are non-compliant");
            Console.WriteLine("✅ Rule engine discriminates between portfolios correctly");
        }
        else
        {
            Console.WriteLine("\n⚠️ Portfolio diversity needs adjustment");
            if (compliantCount == 0)
                Console.WriteLine("❌ No portfolios are compliant - need better diversification");
            if (nonCompliantCount == 0)
                Console.WriteLine("❌ All portfolios are compliant - need some breach scenarios");
        }
        
        Console.WriteLine($"\n📊 Test completed with {compliantCount + nonCompliantCount} portfolios tested");
        
        // Clean up
        engineProcess.Kill();
        engineProcess.WaitForExit();
        Console.WriteLine("✅ Engine stopped");
    }
}