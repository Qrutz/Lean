/*
 * Custom API Connection for Cloud Server Infrastructure
 * Modified from QuantConnect's ApiConnection to work with custom cloud servers
 */

using System;
using RestSharp;
using Newtonsoft.Json;
using QuantConnect.Orders;
using QuantConnect.Logging;
using System.Threading.Tasks;
using RestSharp.Authenticators;
using System.Security.Cryptography;
using System.Text;

namespace QuantConnect.Api
{
    /// <summary>
    /// Custom API Connection for Cloud Server Infrastructure
    /// </summary>
    public class CustomApiConnection
    {
        private readonly static JsonSerializerSettings _jsonSettings = new() { Converters = { new LiveAlgorithmResultsJsonConverter(), new OrderJsonConverter() } };

        /// <summary>
        /// Authorized client to use for requests.
        /// </summary>
        public RestClient Client { get; set; }

        // Authorization Credentials
        private readonly string _userId;
        private readonly string _token;
        private readonly string _apiKey;
        private readonly string _apiSecret;

        private CustomAuthenticator _authenticator;

        /// <summary>
        /// Create a new Custom API Connection Class.
        /// </summary>
        /// <param name="userId">User Id number from your cloud server account</param>
        /// <param name="token">Access token for your cloud server account</param>
        /// <param name="apiKey">API Key for authentication (optional, for API key based auth)</param>
        /// <param name="apiSecret">API Secret for authentication (optional, for API key based auth)</param>
        public CustomApiConnection(int userId, string token, string apiKey = null, string apiSecret = null)
        {
            _token = token;
            _userId = userId.ToStringInvariant();
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            Client = new RestClient(Globals.Api);
        }

        /// <summary>
        /// Return true if connected successfully.
        /// </summary>
        public bool Connected
        {
            get
            {
                var request = new RestRequest("authenticate", Method.GET);
                AuthenticationResponse response;
                if (TryRequest(request, out response))
                {
                    return response.Success;
                }
                return false;
            }
        }

        /// <summary>
        /// Place a secure request and get back an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="result">Result object from the </param>
        /// <returns>T typed object response</returns>
        public bool TryRequest<T>(RestRequest request, out T result)
            where T : RestResponse
        {
            var resultTuple = TryRequestAsync<T>(request).SynchronouslyAwaitTaskResult();
            result = resultTuple.Item2;
            return resultTuple.Item1;
        }

        /// <summary>
        /// Place a secure request and get back an object of type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <returns>T typed object response</returns>
        public async Task<Tuple<bool, T>> TryRequestAsync<T>(RestRequest request)
            where T : RestResponse
        {
            var responseContent = string.Empty;
            T result;
            try
            {
                SetAuthenticator(request);
                var response = await Client.ExecuteAsync(request);
                responseContent = response.Content;

                if (response.IsSuccessful)
                {
                    result = JsonConvert.DeserializeObject<T>(responseContent, _jsonSettings);
                    return Tuple.Create(true, result);
                }
                else
                {
                    Log.Error($"CustomApiConnection.TryRequestAsync(): Request failed: {response.StatusCode} - {responseContent}");
                    result = JsonConvert.DeserializeObject<T>(responseContent, _jsonSettings);
                    return Tuple.Create(false, result);
                }
            }
            catch (Exception err)
            {
                Log.Error($"CustomApiConnection.TryRequestAsync(): Error: {err.Message}");
                result = JsonConvert.DeserializeObject<T>(responseContent, _jsonSettings);
                return Tuple.Create(false, result);
            }
        }

        /// <summary>
        /// Set the authenticator for the request based on your authentication method
        /// </summary>
        /// <param name="request">The request to authenticate</param>
        private void SetAuthenticator(RestRequest request)
        {
            // Choose your authentication method:

            // Method 1: Bearer Token (recommended for most cloud APIs)
            if (!string.IsNullOrEmpty(_token))
            {
                request.AddHeader("Authorization", $"Bearer {_token}");
            }

            // Method 2: API Key + Secret (for services like AWS, etc.)
            else if (!string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_apiSecret))
            {
                request.AddHeader("X-API-Key", _apiKey);
                request.AddHeader("X-API-Secret", _apiSecret);
            }

            // Method 3: Basic Auth (if your server uses username/password)
            else
            {
                // Note: RestSharp doesn't support Authenticator property in this version
                // Basic auth is handled via headers instead
                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_userId}:{_token}"));
                request.AddHeader("Authorization", $"Basic {credentials}");
            }

            // Add common headers
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("User-Agent", "CustomLeanEngine/1.0");
        }

        /// <summary>
        /// Create a secure hash for timestamp-based authentication (if needed)
        /// </summary>
        /// <param name="timestamp">Unix timestamp</param>
        /// <param name="secret">Secret key</param>
        /// <returns>SHA256 hash</returns>
        public static string CreateSecureHash(int timestamp, string secret)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{secret}:{timestamp}"));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Custom authenticator class for different authentication methods
        /// </summary>
        private class CustomAuthenticator
        {
            public int TimeStamp { get; }
            public string TimeStampStr { get; }
            public HttpBasicAuthenticator Authenticator { get; }

            public CustomAuthenticator(HttpBasicAuthenticator authenticator, int timeStamp)
            {
                Authenticator = authenticator;
                TimeStamp = timeStamp;
                TimeStampStr = timeStamp.ToStringInvariant();
            }
        }
    }

    /// <summary>
    /// Authentication response model
    /// </summary>
    public class AuthenticationResponse : RestResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}