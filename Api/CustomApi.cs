/*
 * Custom API for Cloud Server Infrastructure
 * Modified from QuantConnect's Api to work with custom cloud servers
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Extensions;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Optimizer.Objectives;
using QuantConnect.Optimizer.Parameters;
using QuantConnect.Orders;
using QuantConnect.Statistics;
using QuantConnect.Util;
using QuantConnect.Notifications;
using Python.Runtime;
using System.Threading;
using System.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace QuantConnect.Api
{
    /// <summary>
    /// Custom Cloud Server Interaction Via API.
    /// </summary>
    public class CustomApi : IApi, IDownloadProvider
    {
        private readonly BlockingCollection<Lazy<HttpClient>> _clientPool;
        private string _dataFolder;

        /// <summary>
        /// Serializer settings to use
        /// </summary>
        protected JsonSerializerSettings SerializerSettings { get; set; } = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    OverrideSpecifiedNames = true
                }
            }
        };

        /// <summary>
        /// Returns the underlying API connection
        /// </summary>
        protected CustomApiConnection ApiConnection { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="CustomApi"/>
        /// </summary>
        public CustomApi()
        {
            _clientPool = new BlockingCollection<Lazy<HttpClient>>(new ConcurrentQueue<Lazy<HttpClient>>(), 5);
            for (int i = 0; i < _clientPool.BoundedCapacity; i++)
            {
                _clientPool.Add(new Lazy<HttpClient>());
            }
        }

        /// <summary>
        /// Initialize the API with the given variables
        /// </summary>
        public virtual void Initialize(int userId, string token, string dataFolder)
        {
            ApiConnection = new CustomApiConnection(userId, token, null, null);
            _dataFolder = dataFolder?.Replace("\\", "/", StringComparison.InvariantCulture);

            //Allow proper decoding of orders from the API.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };
        }

        /// <summary>
        /// Initialize the API with the given variables including API credentials
        /// </summary>
        public virtual void Initialize(int userId, string token, string dataFolder, string apiKey = null, string apiSecret = null)
        {
            ApiConnection = new CustomApiConnection(userId, token, apiKey, apiSecret);
            _dataFolder = dataFolder?.Replace("\\", "/", StringComparison.InvariantCulture);

            //Allow proper decoding of orders from the API.
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = { new OrderJsonConverter() }
            };
        }

        /// <summary>
        /// Check if Api is successfully connected with correct credentials
        /// </summary>
        public bool Connected => ApiConnection?.Connected == true;

        /// <summary>
        /// Create a new project with the specified name and language
        /// </summary>
        public ProjectResponse CreateProject(string name, Language language, string organizationId = null)
        {
            var request = new RestRequest("projects/create", Method.POST);
            request.AddJsonBody(new
            {
                name = name,
                language = language.ToString()
            });

            return MakeRequestOrThrow<ProjectResponse>(request, nameof(CreateProject));
        }

        /// <summary>
        /// Read details about a single project
        /// </summary>
        public ProjectResponse ReadProject(int projectId)
        {
            var request = new RestRequest("projects/read", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId
            });

            return MakeRequestOrThrow<ProjectResponse>(request, nameof(ReadProject));
        }

        /// <summary>
        /// List details of all projects
        /// </summary>
        public ProjectResponse ListProjects()
        {
            var request = new RestRequest("projects/read", Method.POST);
            // No body needed for listing all projects

            return MakeRequestOrThrow<ProjectResponse>(request, nameof(ListProjects));
        }

        /// <summary>
        /// Add a file to a project
        /// </summary>
        public RestResponse AddProjectFile(int projectId, string name, string content)
        {
            var request = new RestRequest("files/create", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                name = name,
                content = content
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(AddProjectFile));
        }

        /// <summary>
        /// Update the name of a file
        /// </summary>
        public RestResponse UpdateProjectFileName(int projectId, string oldFileName, string newFileName)
        {
            var request = new RestRequest("files/update", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                oldFileName = oldFileName,
                newFileName = newFileName
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(UpdateProjectFileName));
        }

        /// <summary>
        /// Update the contents of a file
        /// </summary>
        public RestResponse UpdateProjectFileContent(int projectId, string fileName, string newFileContents)
        {
            var request = new RestRequest("files/update", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                fileName = fileName,
                newFileContents = newFileContents
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(UpdateProjectFileContent));
        }

        /// <summary>
        /// Read all files in a project
        /// </summary>
        public ProjectFilesResponse ReadProjectFiles(int projectId)
        {
            var request = new RestRequest("files/read", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId
            });

            return MakeRequestOrThrow<ProjectFilesResponse>(request, nameof(ReadProjectFiles));
        }

        /// <summary>
        /// Read a specific file in a project
        /// </summary>
        public ProjectFilesResponse ReadProjectFile(int projectId, string fileName)
        {
            var request = new RestRequest("files/read", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                fileName = fileName
            });

            return MakeRequestOrThrow<ProjectFilesResponse>(request, nameof(ReadProjectFile));
        }

        /// <summary>
        /// Delete a file in a project
        /// </summary>
        public RestResponse DeleteProjectFile(int projectId, string name)
        {
            var request = new RestRequest("files/delete", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                name = name
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(DeleteProjectFile));
        }

        /// <summary>
        /// Delete a project
        /// </summary>
        public RestResponse DeleteProject(int projectId)
        {
            var request = new RestRequest("projects/delete", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(DeleteProject));
        }

        /// <summary>
        /// Create a compile job request for a project
        /// </summary>
        public Compile CreateCompile(int projectId)
        {
            var request = new RestRequest("compile/create", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId
            });

            return MakeRequestOrThrow<Compile>(request, nameof(CreateCompile));
        }

        /// <summary>
        /// Read a compile job result
        /// </summary>
        public Compile ReadCompile(int projectId, string compileId)
        {
            var request = new RestRequest("compile/read", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                compileId = compileId
            });

            return MakeRequestOrThrow<Compile>(request, nameof(ReadCompile));
        }

        /// <summary>
        /// Send a notification
        /// </summary>
        public virtual RestResponse SendNotification(Notification notification, int projectId)
        {
            var request = new RestRequest("notifications/send", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                notification = notification
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(SendNotification));
        }

        /// <summary>
        /// Create a new backtest
        /// </summary>
        public Backtest CreateBacktest(int projectId, string compileId, string backtestName)
        {
            var request = new RestRequest("backtests/create", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                compileId = compileId,
                backtestName = backtestName
            });

            return MakeRequestOrThrow<Backtest>(request, nameof(CreateBacktest));
        }

        /// <summary>
        /// Read a backtest
        /// </summary>
        public Backtest ReadBacktest(int projectId, string backtestId, bool getCharts = true)
        {
            var request = new RestRequest("backtests/read", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                backtestId = backtestId
            });

            return MakeRequestOrThrow<Backtest>(request, nameof(ReadBacktest));
        }

        /// <summary>
        /// List all backtests for a project
        /// </summary>
        public BacktestSummaryList ListBacktests(int projectId, bool includeStatistics = true)
        {
            var request = new RestRequest("backtests/read", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId
            });

            return MakeRequestOrThrow<BacktestSummaryList>(request, nameof(ListBacktests));
        }

        /// <summary>
        /// Delete a backtest
        /// </summary>
        public RestResponse DeleteBacktest(int projectId, string backtestId)
        {
            var request = new RestRequest("backtests/delete", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                backtestId = backtestId
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(DeleteBacktest));
        }

        /// <summary>
        /// Create a live algorithm
        /// </summary>
        public CreateLiveAlgorithmResponse CreateLiveAlgorithm(int projectId, string compileId, string nodeId,
            Dictionary<string, object> brokerageSettings, string versionId = "-1",
            Dictionary<string, object> dataProviders = null)
        {
            var request = new RestRequest("live/create", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                compileId = compileId,
                serverType = nodeId,
                baseLiveAlgorithmSettings = brokerageSettings,
                versionId = versionId,
                dataProviders = dataProviders
            });

            return MakeRequestOrThrow<CreateLiveAlgorithmResponse>(request, nameof(CreateLiveAlgorithm));
        }

        /// <summary>
        /// List live algorithms
        /// </summary>
        public LiveList ListLiveAlgorithms(AlgorithmStatus? status = null)
        {
            var request = new RestRequest("live/read", Method.POST);
            request.AddJsonBody(new
            {
                status = status?.ToString()
            });

            return MakeRequestOrThrow<LiveList>(request, nameof(ListLiveAlgorithms));
        }

        /// <summary>
        /// Read live algorithm results
        /// </summary>
        public LiveAlgorithmResults ReadLiveAlgorithm(int projectId, string deployId)
        {
            var request = new RestRequest("live/read", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                deployId = deployId
            });

            return MakeRequestOrThrow<LiveAlgorithmResults>(request, nameof(ReadLiveAlgorithm));
        }

        /// <summary>
        /// Stop a live algorithm
        /// </summary>
        public RestResponse StopLiveAlgorithm(int projectId)
        {
            var request = new RestRequest("live/update/stop", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(StopLiveAlgorithm));
        }

        /// <summary>
        /// Liquidate a live algorithm
        /// </summary>
        public RestResponse LiquidateLiveAlgorithm(int projectId)
        {
            var request = new RestRequest("live/update/liquidate", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId
            });

            return MakeRequestOrThrow<RestResponse>(request, nameof(LiquidateLiveAlgorithm));
        }

        /// <summary>
        /// Read live logs
        /// </summary>
        public LiveLog ReadLiveLogs(int projectId, string algorithmId, int startLine, int endLine)
        {
            var request = new RestRequest("live/read/log", Method.POST);
            request.AddJsonBody(new
            {
                projectId = projectId,
                algorithmId = algorithmId,
                start = startLine,
                end = endLine
            });

            return MakeRequestOrThrow<LiveLog>(request, nameof(ReadLiveLogs));
        }

        /// <summary>
        /// Download data from your cloud server
        /// </summary>
        public virtual string Download(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password)
        {
            try
            {
                var client = BorrowClient();
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.Value.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                var response = client.Value.GetAsync(address).SynchronouslyAwaitTaskResult();
                response.EnsureSuccessStatusCode();
                var result = response.Content.ReadAsStringAsync().SynchronouslyAwaitTaskResult();
                ReturnClient(client);
                return result;
            }
            catch (Exception err)
            {
                Log.Error($"CustomApi.Download(): Error downloading {address}: {err.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// Download data as bytes from your cloud server
        /// </summary>
        public virtual byte[] DownloadBytes(string address, IEnumerable<KeyValuePair<string, string>> headers, string userName, string password)
        {
            try
            {
                var client = BorrowClient();
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.Value.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                var response = client.Value.GetAsync(address).SynchronouslyAwaitTaskResult();
                response.EnsureSuccessStatusCode();
                var result = response.Content.ReadAsByteArrayAsync().SynchronouslyAwaitTaskResult();
                ReturnClient(client);
                return result;
            }
            catch (Exception err)
            {
                Log.Error($"CustomApi.DownloadBytes(): Error downloading {address}: {err.Message}");
            }

            return Array.Empty<byte>();
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public virtual void Dispose()
        {
            try
            {
                _clientPool?.Dispose();
            }
            catch (Exception err)
            {
                Log.Error($"CustomApi.Dispose(): Error disposing: {err.Message}");
            }
        }

        /// <summary>
        /// Make a request and throw if it fails
        /// </summary>
        private T MakeRequestOrThrow<T>(RestRequest request, string callerName)
            where T : RestResponse
        {
            T result;
            if (!ApiConnection.TryRequest(request, out result))
            {
                throw new Exception($"CustomApi.{callerName}(): Request failed");
            }
            return result;
        }

        /// <summary>
        /// Borrow a client from the pool
        /// </summary>
        private Lazy<HttpClient> BorrowClient()
        {
            return _clientPool.Take();
        }

        /// <summary>
        /// Return a client to the pool
        /// </summary>
        private void ReturnClient(Lazy<HttpClient> client)
        {
            _clientPool.Add(client);
        }

        // Implement other interface methods as needed...
        // These are placeholder implementations - you'll need to implement them based on your server's API

        public virtual AlgorithmControl GetAlgorithmStatus(string algorithmId) => new AlgorithmControl();
        public virtual void SetAlgorithmStatus(string algorithmId, AlgorithmStatus status, string message = "") { }
        public virtual void SendStatistics(string algorithmId, decimal unrealized, decimal fees, decimal netProfit, decimal holdings, decimal equity, decimal netReturn, decimal volume, int trades, double sharpe) { }
        public virtual void SendUserEmail(string algorithmId, string subject, string body) { }
        public virtual bool DownloadData(string filePath, string organizationId) => false;
        public virtual Account ReadAccount(string organizationId = null) => new Account();
        public virtual Organization ReadOrganization(string organizationId = null) => new Organization();
        public virtual Estimate EstimateOptimization(int projectId, string name, string target, string targetTo, decimal? targetValue, string strategy, string compileId, HashSet<OptimizationParameter> parameters, IReadOnlyList<Constraint> constraints) => new Estimate();
        public virtual OptimizationSummary CreateOptimization(int projectId, string name, string target, string targetTo, decimal? targetValue, string strategy, string compileId, HashSet<OptimizationParameter> parameters, IReadOnlyList<Constraint> constraints, decimal estimatedCost, string nodeType, int parallelNodes) => new OptimizationSummary();
        public virtual List<OptimizationSummary> ListOptimizations(int projectId) => new List<OptimizationSummary>();
        public virtual Optimization ReadOptimization(string optimizationId) => new Optimization();
        public virtual RestResponse AbortOptimization(string optimizationId) => new RestResponse();
        public virtual RestResponse UpdateOptimization(string optimizationId, string name = null) => new RestResponse();
        public virtual RestResponse DeleteOptimization(string optimizationId) => new RestResponse();
        public virtual bool GetObjectStore(string organizationId, List<string> keys, string destinationFolder = null) => false;
        public virtual PropertiesObjectStoreResponse GetObjectStoreProperties(string organizationId, string key) => new PropertiesObjectStoreResponse();
        public virtual RestResponse SetObjectStore(string organizationId, string key, byte[] objectData) => new RestResponse();
        public virtual RestResponse DeleteObjectStore(string organizationId, string key) => new RestResponse();
        public virtual ListObjectStoreResponse ListObjectStore(string organizationId, string path) => new ListObjectStoreResponse();
        public virtual DataLink ReadDataLink(string filePath, string organizationId) => new DataLink();
        public virtual DataList ReadDataDirectory(string filePath) => new DataList();
        public virtual DataPricesList ReadDataPrices(string organizationId) => new DataPricesList();
        public virtual BacktestReport ReadBacktestReport(int projectId, string backtestId) => new BacktestReport();
        public virtual VersionsResponse ReadLeanVersions() => new VersionsResponse();
        public virtual ProjectNodesResponse ReadProjectNodes(int projectId) => new ProjectNodesResponse();
        public virtual ProjectNodesResponse UpdateProjectNodes(int projectId, string[] nodes) => new ProjectNodesResponse();
        public virtual PortfolioResponse ReadLivePortfolio(int projectId) => new PortfolioResponse();
        public virtual List<ApiOrderResponse> ReadLiveOrders(int projectId, int start = 0, int end = 100) => new List<ApiOrderResponse>();
        public virtual List<ApiOrderResponse> ReadBacktestOrders(int projectId, string backtestId, int start = 0, int end = 100) => new List<ApiOrderResponse>();
        public virtual ReadChartResponse ReadBacktestChart(int projectId, string name, int start, int end, uint count, string backtestId) => new ReadChartResponse();
        public virtual ReadChartResponse ReadLiveChart(int projectId, string name, int start, int end, uint count) => new ReadChartResponse();
        public virtual RestResponse UpdateBacktest(int projectId, string backtestId, string name = "", string note = "") => new RestResponse();
        public virtual RestResponse UpdateBacktestTags(int projectId, string backtestId, IReadOnlyCollection<string> tags) => new RestResponse();
        public virtual InsightResponse ReadBacktestInsights(int projectId, string backtestId, int start = 0, int end = 0) => new InsightResponse();
        public virtual InsightResponse ReadLiveInsights(int projectId, int start = 0, int end = 0) => new InsightResponse();
        public virtual RestResponse CreateLiveCommand(int projectId, object command) => new RestResponse();
        public virtual RestResponse BroadcastLiveCommand(string organizationId, int? excludeProjectId, object command) => new RestResponse();
    }
}