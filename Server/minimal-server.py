#!/usr/bin/env python3
"""
Minimal Custom Cloud Server for LEAN Framework Testing
This is a simple Flask server that implements the basic API endpoints
needed to test the custom cloud framework with LEAN.
"""

from flask import Flask, request, jsonify
from flask_cors import CORS
import json
import uuid
import datetime
import threading
import time
from typing import Dict, List, Any

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

# In-memory storage for demo purposes
projects = {}
files = {}
compiles = {}
backtests = {}

# Simple authentication (for demo purposes)
VALID_TOKENS = ["demo-token-123", "test-token-456"]


def authenticate():
    """Simple token-based authentication"""
    auth_header = request.headers.get("Authorization")
    if not auth_header:
        return False

    if auth_header.startswith("Bearer "):
        token = auth_header[7:]  # Remove 'Bearer ' prefix
        return token in VALID_TOKENS

    return False


@app.before_request
def before_request():
    """Check authentication for all requests except /authenticate"""
    if request.endpoint == "authenticate":
        return

    if not authenticate():
        return jsonify({"success": False, "errors": ["Unauthorized"]}), 401


@app.route("/api/v2/authenticate", methods=["GET"])
def authenticate():
    """Authentication endpoint"""
    return jsonify({"success": True, "message": "Authentication successful"})


@app.route("/api/v2/projects/create", methods=["POST"])
def create_project():
    """Create a new project"""
    data = request.get_json()
    name = data.get("name", "Untitled Project")
    language = data.get("language", "C#")

    project_id = len(projects) + 1
    project = {
        "projectId": project_id,
        "name": name,
        "language": language,
        "created": datetime.datetime.now().isoformat(),
        "modified": datetime.datetime.now().isoformat(),
    }

    projects[project_id] = project
    files[project_id] = []

    return jsonify({"success": True, "projects": [project], "errors": []})


@app.route("/api/v2/projects/read", methods=["POST"])
def read_projects():
    """Read project details or list all projects"""
    data = request.get_json() or {}
    project_id = data.get("projectId")

    if project_id:
        # Return specific project
        if project_id in projects:
            return jsonify(
                {"success": True, "projects": [projects[project_id]], "errors": []}
            )
        else:
            return jsonify(
                {"success": False, "projects": [], "errors": ["Project not found"]}
            )
    else:
        # Return all projects
        return jsonify(
            {"success": True, "projects": list(projects.values()), "errors": []}
        )


@app.route("/api/v2/files/create", methods=["POST"])
def create_file():
    """Add a file to a project"""
    data = request.get_json()
    project_id = data.get("projectId")
    name = data.get("name")
    content = data.get("content", "")

    if project_id not in projects:
        return jsonify({"success": False, "files": [], "errors": ["Project not found"]})

    file_info = {
        "name": name,
        "content": content,
        "modified": datetime.datetime.now().isoformat(),
    }

    files[project_id].append(file_info)

    return jsonify({"success": True, "files": files[project_id], "errors": []})


@app.route("/api/v2/files/read", methods=["POST"])
def read_files():
    """Read files from a project"""
    data = request.get_json()
    project_id = data.get("projectId")
    file_name = data.get("fileName")

    if project_id not in projects:
        return jsonify({"success": False, "files": [], "errors": ["Project not found"]})

    project_files = files[project_id]

    if file_name:
        # Return specific file
        for file_info in project_files:
            if file_info["name"] == file_name:
                return jsonify({"success": True, "files": [file_info], "errors": []})
        return jsonify({"success": False, "files": [], "errors": ["File not found"]})
    else:
        # Return all files
        return jsonify({"success": True, "files": project_files, "errors": []})


@app.route("/api/v2/compile/create", methods=["POST"])
def create_compile():
    """Create a compile job"""
    data = request.get_json()
    project_id = data.get("projectId")

    if project_id not in projects:
        return jsonify({"success": False, "errors": ["Project not found"]})

    compile_id = str(uuid.uuid4())
    compile_info = {
        "compileId": compile_id,
        "state": "InQueue",
        "logs": ["Compilation started"],
        "success": True,
        "errors": [],
    }

    compiles[compile_id] = compile_info

    # Simulate compilation process
    def simulate_compilation():
        time.sleep(2)  # Simulate compilation time
        compiles[compile_id]["state"] = "BuildSuccess"
        compiles[compile_id]["logs"].append("Compilation completed successfully")

    threading.Thread(target=simulate_compilation).start()

    return jsonify(compile_info)


@app.route("/api/v2/compile/read", methods=["POST"])
def read_compile():
    """Read compile result"""
    data = request.get_json()
    project_id = data.get("projectId")
    compile_id = data.get("compileId")

    if compile_id not in compiles:
        return jsonify({"success": False, "errors": ["Compile job not found"]})

    return jsonify(compiles[compile_id])


@app.route("/api/v2/backtests/create", methods=["POST"])
def create_backtest():
    """Create a backtest"""
    data = request.get_json()
    project_id = data.get("projectId")
    compile_id = data.get("compileId")
    backtest_name = data.get("backtestName", "Untitled Backtest")

    if project_id not in projects:
        return jsonify({"success": False, "errors": ["Project not found"]})

    backtest_id = str(uuid.uuid4())
    backtest_info = {
        "name": backtest_name,
        "note": "",
        "backtestId": backtest_id,
        "completed": False,
        "progress": 0.0,
        "result": {
            "TotalPerformance": {
                "TradeStatistics": {"TotalNumberOfTrades": 0, "WinRate": 0.0},
                "PortfolioStatistics": {"TotalNetProfit": 0.0, "SharpeRatio": 0.0},
            },
            "Charts": {},
            "Orders": {},
            "Statistics": {},
        },
        "error": "",
        "stacktrace": "",
        "created": datetime.datetime.now().isoformat(),
        "success": True,
        "errors": [],
    }

    backtests[backtest_id] = backtest_info

    # Simulate backtest process
    def simulate_backtest():
        for i in range(10):
            time.sleep(1)
            backtests[backtest_id]["progress"] = (i + 1) / 10.0

        # Final results
        backtests[backtest_id]["completed"] = True
        backtests[backtest_id]["progress"] = 1.0
        backtests[backtest_id]["result"]["TotalPerformance"]["TradeStatistics"][
            "TotalNumberOfTrades"
        ] = 25
        backtests[backtest_id]["result"]["TotalPerformance"]["TradeStatistics"][
            "WinRate"
        ] = 0.68
        backtests[backtest_id]["result"]["TotalPerformance"]["PortfolioStatistics"][
            "TotalNetProfit"
        ] = 0.15
        backtests[backtest_id]["result"]["TotalPerformance"]["PortfolioStatistics"][
            "SharpeRatio"
        ] = 1.2

    threading.Thread(target=simulate_backtest).start()

    return jsonify(backtest_info)


@app.route("/api/v2/backtests/read", methods=["POST"])
def read_backtest():
    """Read backtest results"""
    data = request.get_json()
    project_id = data.get("projectId")
    backtest_id = data.get("backtestId")

    if backtest_id:
        # Return specific backtest
        if backtest_id in backtests:
            return jsonify(backtests[backtest_id])
        else:
            return jsonify({"success": False, "errors": ["Backtest not found"]})
    else:
        # Return all backtests for project
        project_backtests = [
            bt for bt in backtests.values() if bt.get("projectId") == project_id
        ]
        return jsonify({"success": True, "backtests": project_backtests, "errors": []})


@app.route("/api/v2/live/create", methods=["POST"])
def create_live_algorithm():
    """Create a live algorithm (simplified)"""
    data = request.get_json()
    project_id = data.get("projectId")
    compile_id = data.get("compileId")

    if project_id not in projects:
        return jsonify({"success": False, "errors": ["Project not found"]})

    deploy_id = str(uuid.uuid4())
    live_info = {
        "projectId": project_id,
        "deployId": deploy_id,
        "status": "Running",
        "launched": datetime.datetime.now().isoformat(),
        "stopped": None,
        "success": True,
        "errors": [],
    }

    return jsonify(live_info)


@app.route("/api/v2/live/read", methods=["POST"])
def read_live_algorithms():
    """Read live algorithm details"""
    data = request.get_json()
    project_id = data.get("projectId")
    deploy_id = data.get("deployId")
    status = data.get("status")

    # For demo purposes, return a mock live algorithm
    mock_live = {
        "projectId": project_id or 1,
        "deployId": deploy_id or "demo-deploy-123",
        "status": status or "Running",
        "launched": datetime.datetime.now().isoformat(),
        "stopped": None,
        "success": True,
        "errors": [],
    }

    return jsonify({"success": True, "Algorithms": [mock_live], "errors": []})


@app.route("/api/v2/live/update/stop", methods=["POST"])
def stop_live_algorithm():
    """Stop a live algorithm"""
    data = request.get_json()
    project_id = data.get("projectId")

    return jsonify({"success": True, "errors": []})


@app.route("/api/v2/live/update/liquidate", methods=["POST"])
def liquidate_live_algorithm():
    """Liquidate a live algorithm"""
    data = request.get_json()
    project_id = data.get("projectId")

    return jsonify({"success": True, "errors": []})


@app.route("/api/v2/live/read/log", methods=["POST"])
def read_live_logs():
    """Read live algorithm logs"""
    data = request.get_json()
    project_id = data.get("projectId")
    algorithm_id = data.get("algorithmId")

    mock_logs = [
        "Algorithm initialized successfully",
        "Connected to data feed",
        "Processing market data...",
        "Order submitted: BUY 100 SPY @ $150.25",
    ]

    return jsonify({"success": True, "LiveLogs": mock_logs, "errors": []})


@app.route("/api/v2/data/read", methods=["POST"])
def read_data():
    """Get data download link"""
    data = request.get_json()

    return jsonify(
        {"success": True, "link": "https://example.com/data/sample.csv", "errors": []}
    )


@app.route("/api/v2/backtests/read/report", methods=["POST"])
def read_backtest_report():
    """Read backtest report"""
    data = request.get_json()
    project_id = data.get("projectId")
    backtest_id = data.get("backtestId")

    mock_report = """
    <html>
    <head><title>Backtest Report</title></head>
    <body>
        <h1>Backtest Report</h1>
        <p>This is a mock backtest report for demonstration purposes.</p>
        <p>Total Trades: 25</p>
        <p>Win Rate: 68%</p>
        <p>Total Return: 15%</p>
        <p>Sharpe Ratio: 1.2</p>
    </body>
    </html>
    """

    return jsonify({"success": True, "report": mock_report, "errors": []})


@app.route("/health", methods=["GET"])
def health_check():
    """Health check endpoint"""
    return jsonify(
        {
            "status": "healthy",
            "timestamp": datetime.datetime.now().isoformat(),
            "projects_count": len(projects),
            "backtests_count": len(backtests),
        }
    )


@app.route("/", methods=["GET"])
def root():
    """Root endpoint with server info"""
    return jsonify(
        {
            "message": "Minimal Custom Cloud Server for LEAN Framework",
            "version": "1.0.0",
            "endpoints": [
                "/api/v2/authenticate",
                "/api/v2/projects/create",
                "/api/v2/projects/read",
                "/api/v2/files/create",
                "/api/v2/files/read",
                "/api/v2/compile/create",
                "/api/v2/compile/read",
                "/api/v2/backtests/create",
                "/api/v2/backtests/read",
                "/api/v2/live/create",
                "/api/v2/live/read",
                "/health",
            ],
            "authentication": "Bearer token required (demo-token-123, test-token-456)",
        }
    )


if __name__ == "__main__":
    print("🚀 Starting Minimal Custom Cloud Server...")
    print("📡 Server will be available at: http://localhost:5000")
    print("🔑 Use one of these tokens for authentication:")
    print("   - demo-token-123")
    print("   - test-token-456")
    print("📖 API documentation available at: http://localhost:5000")
    print("💚 Health check available at: http://localhost:5000/health")
    print("\nPress Ctrl+C to stop the server")

    app.run(host="0.0.0.0", port=5001, debug=True)
