using System;
using QuantConnect.Api;
using QuantConnect.Configuration;

namespace QuantConnect.Examples
{
    /// <summary>
    /// Example demonstrating how to use the Custom Cloud Server Framework
    /// </summary>
    public class CustomCloudExample
    {
        public static void Main(string[] args)
        {
            // Initialize the custom API
            var api = new CustomApi();

            // Initialize with your cloud server credentials
            api.Initialize(
                userId: 12345,                    // Your user ID
                token: "your-access-token",       // Your access token
                dataFolder: "../../../Data/",     // Data folder path
                apiKey: "your-api-key",           // Optional: API key
                apiSecret: "your-api-secret"      // Optional: API secret
            );

            // Check if connected successfully
            if (!api.Connected)
            {
                Console.WriteLine("Failed to connect to custom cloud server");
                return;
            }

            Console.WriteLine("Successfully connected to custom cloud server!");

            // Example: Create a new project
            try
            {
                var projectResponse = api.CreateProject("My Custom Algorithm", Language.CSharp);
                if (projectResponse.Success)
                {
                    Console.WriteLine($"Created project: {projectResponse.Projects[0].Name}");

                    // Example: Add a file to the project
                    var algorithmCode = @"
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class CustomAlgorithm : QCAlgorithm
    {
        private ExponentialMovingAverage _ema;

        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 12, 31);
            SetCash(100000);

            var symbol = AddEquity(""SPY"").Symbol;
            _ema = EMA(symbol, 20);
        }

        public override void OnData(TradeBars data)
        {
            if (!_ema.IsReady) return;

            var symbol = ""SPY"";
            if (data.ContainsKey(symbol))
            {
                var price = data[symbol].Close;
                var emaValue = _ema.Current.Value;

                if (price > emaValue && !Portfolio[symbol].Invested)
                {
                    SetHoldings(symbol, 1.0);
                }
                else if (price < emaValue && Portfolio[symbol].Invested)
                {
                    Liquidate(symbol);
                }
            }
        }
    }
}";

                    var fileResponse = api.AddProjectFile(
                        projectId: projectResponse.Projects[0].ProjectId,
                        name: "CustomAlgorithm.cs",
                        content: algorithmCode
                    );

                    if (fileResponse.Success)
                    {
                        Console.WriteLine("Added algorithm file to project");

                        // Example: Compile the project
                        var compileResponse = api.CreateCompile(projectResponse.Projects[0].ProjectId);
                        if (compileResponse.Success)
                        {
                            Console.WriteLine($"Compilation started with ID: {compileResponse.CompileId}");

                            // Wait for compilation to complete (in real usage, you'd poll for status)
                            System.Threading.Thread.Sleep(5000);

                            var compileResult = api.ReadCompile(projectResponse.Projects[0].ProjectId, compileResponse.CompileId);
                            if (compileResult.Success && compileResult.State == "BuildSuccess")
                            {
                                Console.WriteLine("Compilation successful!");

                                // Example: Create a backtest
                                var backtestResponse = api.CreateBacktest(
                                    projectId: projectResponse.Projects[0].ProjectId,
                                    compileId: compileResult.CompileId,
                                    backtestName: "My First Custom Backtest"
                                );

                                if (backtestResponse.Success)
                                {
                                    Console.WriteLine($"Backtest created with ID: {backtestResponse.BacktestId}");

                                    // Wait for backtest to complete (in real usage, you'd poll for status)
                                    System.Threading.Thread.Sleep(10000);

                                    var backtestResult = api.ReadBacktest(
                                        projectId: projectResponse.Projects[0].ProjectId,
                                        backtestId: backtestResponse.BacktestId
                                    );

                                    if (backtestResult.Success && backtestResult.Completed)
                                    {
                                        Console.WriteLine("Backtest completed successfully!");
                                        Console.WriteLine($"Total Trades: {backtestResult.Result.TotalPerformance.TradeStatistics.TotalNumberOfTrades}");
                                        Console.WriteLine($"Win Rate: {backtestResult.Result.TotalPerformance.TradeStatistics.WinRate:P2}");
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Compilation failed: {compileResult.Logs}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            // Example: List all projects
            try
            {
                var projectsResponse = api.ListProjects();
                if (projectsResponse.Success)
                {
                    Console.WriteLine("\nAll Projects:");
                    foreach (var project in projectsResponse.Projects)
                    {
                        Console.WriteLine($"- {project.Name} (ID: {project.ProjectId}, Language: {project.Language})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing projects: {ex.Message}");
            }

            Console.WriteLine("\nCustom Cloud Server Example completed!");
        }
    }
}