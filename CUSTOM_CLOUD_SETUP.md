# Custom Cloud Server Framework Setup

This guide explains how to modify the QuantConnect LEAN framework to work with your own cloud server infrastructure instead of QuantConnect's services.

## Overview

The framework has been modified to support custom cloud servers while maintaining compatibility with the original QuantConnect API structure. This allows you to:

1. **Use your own cloud infrastructure** for project management, backtesting, and live trading
2. **Maintain the same API interface** so existing algorithms work without modification
3. **Customize authentication** to work with your security requirements
4. **Scale independently** without relying on QuantConnect's services

## Key Changes Made

### 1. API Endpoint Configuration
- **File**: `Common/Globals.cs`
- **Change**: Updated default API URL from `https://www.quantconnect.com/api/v2/` to `https://your-cloud-server.com/api/v2/`

### 2. Custom API Connection
- **File**: `Api/CustomApiConnection.cs`
- **Purpose**: Handles authentication and communication with your cloud server
- **Features**:
  - Multiple authentication methods (Bearer Token, API Key/Secret, Basic Auth)
  - Flexible header configuration
  - Error handling and logging

### 3. Custom API Implementation
- **File**: `Api/CustomApi.cs`
- **Purpose**: Implements the IApi interface for your cloud server
- **Features**:
  - Project management (create, read, update, delete)
  - File management (upload, download, update)
  - Backtesting operations
  - Live trading operations
  - Data download capabilities

### 4. Configuration Template
- **File**: `Launcher/config.custom-cloud.json`
- **Purpose**: Template configuration for your cloud server setup

### 5. API Specification
- **File**: `Api/Custom-Cloud-Server-API.yaml`
- **Purpose**: OpenAPI specification for implementing your server-side API

## Setup Instructions

### Step 1: Configure Your Cloud Server

1. **Set up your cloud infrastructure** (AWS, Azure, GCP, or your own servers)
2. **Implement the API endpoints** using the provided OpenAPI specification
3. **Choose your authentication method**:
   - **Bearer Token** (recommended): JWT-based authentication
   - **API Key/Secret**: For services like AWS API Gateway
   - **Basic Auth**: Username/password authentication

### Step 2: Update Configuration

1. **Copy the configuration template**:
   ```bash
   cp Launcher/config.custom-cloud.json Launcher/config.json
   ```

2. **Edit the configuration** with your server details:
   ```json
   {
     "api-url": "https://your-cloud-server.com/api/v2/",
     "api-key": "your-api-key",
     "api-secret": "your-api-secret",
     "job-user-id": "your-user-id",
     "api-access-token": "your-access-token"
   }
   ```

### Step 3: Choose Authentication Method

#### Option A: Bearer Token (Recommended)
```csharp
// In your CustomApiConnection.cs
request.AddHeader("Authorization", $"Bearer {_token}");
```

#### Option B: API Key + Secret
```csharp
// In your CustomApiConnection.cs
request.AddHeader("X-API-Key", _apiKey);
request.AddHeader("X-API-Secret", _apiSecret);
```

#### Option C: Basic Auth
```csharp
// In your CustomApiConnection.cs
var authenticator = new HttpBasicAuthenticator(_userId, _token);
request.Authenticator = authenticator;
```

### Step 4: Implement Server-Side API

Use the provided OpenAPI specification (`Api/Custom-Cloud-Server-API.yaml`) to implement your server-side API. The specification includes:

- **Authentication endpoints**
- **Project management** (CRUD operations)
- **File management** (upload, download, update)
- **Compilation services**
- **Backtesting operations**
- **Live trading operations**
- **Data download services**

### Step 5: Test Your Setup

1. **Run a simple backtest**:
   ```bash
   dotnet run --project Launcher --config config.json
   ```

2. **Check the logs** for any connection issues
3. **Verify API calls** are reaching your server

## Server Implementation Examples

### Node.js/Express Example
```javascript
const express = require('express');
const jwt = require('jsonwebtoken');
const app = express();

// Authentication middleware
const authenticateToken = (req, res, next) => {
  const authHeader = req.headers['authorization'];
  const token = authHeader && authHeader.split(' ')[1];
  
  if (!token) return res.sendStatus(401);
  
  jwt.verify(token, process.env.JWT_SECRET, (err, user) => {
    if (err) return res.sendStatus(403);
    req.user = user;
    next();
  });
};

// Projects endpoint
app.post('/api/v2/projects/create', authenticateToken, (req, res) => {
  const { name, language } = req.body;
  // Create project logic here
  res.json({
    success: true,
    projects: [{ projectId: 1, name, language }]
  });
});

app.listen(3000, () => {
  console.log('Custom Cloud Server running on port 3000');
});
```

### Python/FastAPI Example
```python
from fastapi import FastAPI, HTTPException, Depends
from fastapi.security import HTTPBearer
import jwt

app = FastAPI()
security = HTTPBearer()

async def verify_token(token: str = Depends(security)):
    try:
        payload = jwt.decode(token, "your-secret-key", algorithms=["HS256"])
        return payload
    except jwt.ExpiredSignatureError:
        raise HTTPException(status_code=401, detail="Token expired")
    except jwt.InvalidTokenError:
        raise HTTPException(status_code=401, detail="Invalid token")

@app.post("/api/v2/projects/create")
async def create_project(request: dict, user: dict = Depends(verify_token)):
    # Create project logic here
    return {
        "success": True,
        "projects": [{"projectId": 1, "name": request["name"], "language": request["language"]}]
    }
```

## Data Storage Considerations

### Project Storage
- **Database**: PostgreSQL, MySQL, or MongoDB for project metadata
- **File Storage**: AWS S3, Azure Blob Storage, or local file system for algorithm files
- **Version Control**: Git integration for code versioning

### Backtest Results
- **Database**: Store backtest metadata and results
- **File Storage**: Store detailed results, charts, and reports
- **Caching**: Redis for frequently accessed data

### Live Trading
- **Real-time Data**: WebSocket connections for live data feeds
- **Order Management**: Integration with your preferred brokerages
- **Monitoring**: Real-time monitoring and alerting systems

## Security Best Practices

1. **Use HTTPS** for all API communications
2. **Implement rate limiting** to prevent abuse
3. **Validate all inputs** to prevent injection attacks
4. **Use secure authentication** (JWT tokens with expiration)
5. **Implement proper logging** for audit trails
6. **Use environment variables** for sensitive configuration
7. **Regular security updates** for all dependencies

## Monitoring and Logging

### Application Logs
```csharp
// In your CustomApiConnection.cs
Log.Error($"CustomApiConnection.TryRequestAsync(): Request failed: {response.StatusCode} - {responseContent}");
```

### Server Monitoring
- **Health checks** for API endpoints
- **Performance monitoring** (response times, error rates)
- **Resource monitoring** (CPU, memory, disk usage)
- **Alerting** for critical issues

## Troubleshooting

### Common Issues

1. **Authentication Failures**
   - Check API keys and tokens
   - Verify authentication method in configuration
   - Check server logs for authentication errors

2. **Connection Timeouts**
   - Verify server URL and port
   - Check network connectivity
   - Review server load and capacity

3. **API Endpoint Errors**
   - Verify endpoint implementation matches specification
   - Check request/response format
   - Review server logs for detailed error messages

### Debug Mode
Enable detailed logging by setting:
```json
{
  "debugging": true,
  "show-missing-data-logs": true
}
```

## Migration from QuantConnect

If you're migrating from QuantConnect to your own infrastructure:

1. **Export existing projects** using QuantConnect's API
2. **Import projects** to your custom server
3. **Update configuration** to point to your server
4. **Test thoroughly** before going live
5. **Monitor performance** and adjust as needed

## Support and Maintenance

### Regular Tasks
- **Security updates** for all components
- **Performance monitoring** and optimization
- **Backup verification** for data integrity
- **Capacity planning** for growth

### Documentation
- **API documentation** for your team
- **Deployment guides** for new environments
- **Troubleshooting guides** for common issues
- **User training** materials

## Conclusion

This modified framework provides a solid foundation for running your own algorithmic trading infrastructure. The key benefits include:

- **Complete control** over your trading environment
- **Cost optimization** by using your preferred cloud providers
- **Customization flexibility** for your specific needs
- **Scalability** to handle your growth requirements
- **Security control** over your data and algorithms

Remember to thoroughly test your implementation before deploying to production, and maintain regular backups of your data and configuration. 