# Minimal Custom Cloud Server

This is a minimal server implementation that demonstrates how to run your own cloud infrastructure for the LEAN algorithmic trading framework instead of using QuantConnect's services.

## 🚀 Quick Start

### 1. Install Dependencies

```bash
# Install Python dependencies
pip install -r requirements.txt
```

### 2. Start the Server

```bash
# Start the minimal server
python minimal-server.py
```

You should see output like:
```
🚀 Starting Minimal Custom Cloud Server...
📡 Server will be available at: http://localhost:5001
🔑 Use one of these tokens for authentication:
   - demo-token-123
   - test-token-456
📖 API documentation available at: http://localhost:5001
💚 Health check available at: http://localhost:5001/health
```

### 3. Test the Server

```bash
# Test all API endpoints
python test-server.py
```

This will run a comprehensive test of all endpoints to ensure everything is working correctly.

### 4. Test with LEAN Framework

1. **Copy the test configuration**:
   ```bash
   cp Launcher/config.minimal-test.json Launcher/config.json
   ```

2. **Run LEAN with the custom server**:
   ```bash
   dotnet run --project Launcher --config config.json
   ```

## 🔧 Server Features

The minimal server implements these key endpoints:

- **Authentication**: `/api/v2/authenticate`
- **Project Management**: Create, read, update, delete projects
- **File Management**: Upload, download, update algorithm files
- **Compilation**: Compile projects and check status
- **Backtesting**: Create and monitor backtests
- **Live Trading**: Create and manage live algorithms
- **Data Access**: Download market data

## 🔑 Authentication

The server uses simple Bearer token authentication:

- **Token**: `demo-token-123` or `test-token-456`
- **Header**: `Authorization: Bearer <token>`

## 📊 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/v2/authenticate` | GET | Authentication check |
| `/api/v2/projects/create` | POST | Create new project |
| `/api/v2/projects/read` | POST | Read project(s) |
| `/api/v2/files/create` | POST | Add file to project |
| `/api/v2/files/read` | POST | Read project files |
| `/api/v2/compile/create` | POST | Start compilation |
| `/api/v2/compile/read` | POST | Check compile status |
| `/api/v2/backtests/create` | POST | Create backtest |
| `/api/v2/backtests/read` | POST | Read backtest results |
| `/api/v2/live/create` | POST | Create live algorithm |
| `/api/v2/live/read` | POST | Read live algorithms |
| `/health` | GET | Server health check |

## 🧪 Testing

### Manual Testing

You can test individual endpoints using curl:

```bash
# Test authentication
curl -X GET http://localhost:5001/api/v2/authenticate

# Create a project
curl -X POST http://localhost:5001/api/v2/projects/create \
  -H "Authorization: Bearer demo-token-123" \
  -H "Content-Type: application/json" \
  -d '{"name": "Test Project", "language": "C#"}'

# Check server health
curl -X GET http://localhost:5001/health
```

### Automated Testing

Run the comprehensive test suite:

```bash
python test-server.py
```

This will test:
- ✅ Authentication
- ✅ Project creation
- ✅ File upload
- ✅ Compilation
- ✅ Backtest creation
- ✅ Live algorithm operations
- ✅ Health monitoring

## 🔍 Monitoring

### Health Check

Visit `http://localhost:5001/health` to see server status:

```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00",
  "projects_count": 5,
  "backtests_count": 3
}
```

### Server Info

Visit `http://localhost:5001/` to see server information and available endpoints.

## 🛠️ Customization

### Adding New Endpoints

To add new endpoints, edit `minimal-server.py`:

```python
@app.route('/api/v2/your-endpoint', methods=['POST'])
def your_endpoint():
    data = request.get_json()
    # Your logic here
    return jsonify({
        "success": True,
        "data": "your response"
    })
```

### Modifying Authentication

To change authentication, modify the `authenticate()` function:

```python
def authenticate():
    # Your custom authentication logic
    return True  # or False
```

### Adding Database Storage

Replace the in-memory storage with a database:

```python
# Instead of:
projects = {}

# Use:
import sqlite3
# or
import psycopg2
# or your preferred database
```

## 🚨 Important Notes

1. **This is a demo server** - It uses in-memory storage and will lose data when restarted
2. **No real compilation** - Compilation is simulated for demonstration
3. **No real backtesting** - Backtest results are mocked
4. **No real live trading** - Live algorithms are simulated
5. **Simple authentication** - Uses basic token-based auth for demo purposes

## 🔄 Next Steps

Once you've verified the minimal server works with LEAN, you can:

1. **Implement real compilation** using .NET Core
2. **Add database storage** (PostgreSQL, MongoDB, etc.)
3. **Implement real backtesting** with actual market data
4. **Add real live trading** with broker integrations
5. **Enhance security** with proper JWT tokens
6. **Add monitoring** and logging
7. **Scale horizontally** with load balancing

## 📚 Documentation

- **API Specification**: See `Api/Custom-Cloud-Server-API.yaml`
- **LEAN Integration**: See `CUSTOM_CLOUD_SETUP.md`
- **Configuration**: See `Launcher/config.minimal-test.json`

## 🆘 Troubleshooting

### Server won't start
- Check if port 5001 is available
- Install dependencies: `pip install -r requirements.txt`
- Check Python version (3.7+ required)

### Authentication fails
- Verify token is correct: `demo-token-123` or `test-token-456`
- Check Authorization header format: `Bearer <token>`

### LEAN can't connect
- Ensure server is running on `http://localhost:5001`
- Check configuration file points to correct URL
- Verify authentication token in config

### Tests fail
- Ensure server is running before running tests
- Check network connectivity to localhost:5001
- Review server logs for error messages

## 📞 Support

If you encounter issues:

1. Check the server logs for error messages
2. Verify all dependencies are installed
3. Ensure the server is running on the correct port
4. Test individual endpoints manually
5. Review the configuration files

This minimal server proves that LEAN can communicate with your own cloud infrastructure instead of QuantConnect's services! 