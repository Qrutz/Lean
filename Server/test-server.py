#!/usr/bin/env python3
"""
Test script for the minimal custom cloud server
This script tests the basic API endpoints to ensure they're working correctly.
"""

import requests
import json
import time

# Server configuration
BASE_URL = "http://localhost:5001/api/v2"
TOKEN = "demo-token-123"
HEADERS = {"Authorization": f"Bearer {TOKEN}", "Content-Type": "application/json"}


def test_authentication():
    """Test authentication endpoint"""
    print("🔐 Testing authentication...")
    response = requests.get(f"{BASE_URL}/authenticate")
    if response.status_code == 200:
        print("✅ Authentication successful")
        return True
    else:
        print(f"❌ Authentication failed: {response.status_code}")
        return False


def test_create_project():
    """Test project creation"""
    print("\n📁 Testing project creation...")
    data = {"name": "Test Algorithm", "language": "C#"}
    response = requests.post(f"{BASE_URL}/projects/create", headers=HEADERS, json=data)
    if response.status_code == 200:
        result = response.json()
        if result.get("success"):
            project = result["projects"][0]
            print(f"✅ Project created: {project['name']} (ID: {project['projectId']})")
            return project["projectId"]
        else:
            print(f"❌ Project creation failed: {result.get('errors')}")
            return None
    else:
        print(f"❌ Project creation failed: {response.status_code}")
        return None


def test_create_file(project_id):
    """Test file creation"""
    print(f"\n📄 Testing file creation for project {project_id}...")
    data = {
        "projectId": project_id,
        "name": "TestAlgorithm.cs",
        "content": """
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    public class TestAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            SetStartDate(2020, 1, 1);
            SetEndDate(2020, 12, 31);
            SetCash(100000);
            AddEquity("SPY");
        }

        public override void OnData(TradeBars data)
        {
            // Simple test algorithm
            if (!Portfolio.Invested)
            {
                SetHoldings("SPY", 1.0);
            }
        }
    }
}""",
    }
    response = requests.post(f"{BASE_URL}/files/create", headers=HEADERS, json=data)
    if response.status_code == 200:
        result = response.json()
        if result.get("success"):
            print(f"✅ File created successfully")
            return True
        else:
            print(f"❌ File creation failed: {result.get('errors')}")
            return False
    else:
        print(f"❌ File creation failed: {response.status_code}")
        return False


def test_create_compile(project_id):
    """Test compilation"""
    print(f"\n🔨 Testing compilation for project {project_id}...")
    data = {"projectId": project_id}
    response = requests.post(f"{BASE_URL}/compile/create", headers=HEADERS, json=data)
    if response.status_code == 200:
        result = response.json()
        if result.get("success"):
            compile_id = result["compileId"]
            print(f"✅ Compilation started: {compile_id}")
            return compile_id
        else:
            print(f"❌ Compilation failed: {result.get('errors')}")
            return None
    else:
        print(f"❌ Compilation failed: {response.status_code}")
        return None


def test_read_compile(project_id, compile_id):
    """Test reading compile status"""
    print(f"\n📖 Testing compile status for {compile_id}...")
    data = {"projectId": project_id, "compileId": compile_id}

    # Poll for completion
    for i in range(10):
        response = requests.post(f"{BASE_URL}/compile/read", headers=HEADERS, json=data)
        if response.status_code == 200:
            result = response.json()
            state = result.get("state", "Unknown")
            print(f"   Compile state: {state}")

            if state == "BuildSuccess":
                print("✅ Compilation completed successfully")
                return True
            elif state == "BuildError":
                print("❌ Compilation failed")
                return False

        time.sleep(1)

    print("⏰ Compilation timed out")
    return False


def test_create_backtest(project_id, compile_id):
    """Test backtest creation"""
    print(f"\n📊 Testing backtest creation...")
    data = {
        "projectId": project_id,
        "compileId": compile_id,
        "backtestName": "Test Backtest",
    }
    response = requests.post(f"{BASE_URL}/backtests/create", headers=HEADERS, json=data)
    if response.status_code == 200:
        result = response.json()
        if result.get("success"):
            backtest_id = result["backtestId"]
            print(f"✅ Backtest created: {backtest_id}")
            return backtest_id
        else:
            print(f"❌ Backtest creation failed: {result.get('errors')}")
            return None
    else:
        print(f"❌ Backtest creation failed: {response.status_code}")
        return None


def test_read_backtest(project_id, backtest_id):
    """Test reading backtest results"""
    print(f"\n📈 Testing backtest results for {backtest_id}...")
    data = {"projectId": project_id, "backtestId": backtest_id}

    # Poll for completion
    for i in range(15):
        response = requests.post(
            f"{BASE_URL}/backtests/read", headers=HEADERS, json=data
        )
        if response.status_code == 200:
            result = response.json()
            completed = result.get("completed", False)
            progress = result.get("progress", 0.0)
            print(f"   Backtest progress: {progress:.1%}")

            if completed:
                trades = (
                    result.get("result", {})
                    .get("TotalPerformance", {})
                    .get("TradeStatistics", {})
                    .get("TotalNumberOfTrades", 0)
                )
                win_rate = (
                    result.get("result", {})
                    .get("TotalPerformance", {})
                    .get("TradeStatistics", {})
                    .get("WinRate", 0.0)
                )
                print(
                    f"✅ Backtest completed: {trades} trades, {win_rate:.1%} win rate"
                )
                return True

        time.sleep(1)

    print("⏰ Backtest timed out")
    return False


def test_live_operations(project_id, compile_id):
    """Test live algorithm operations"""
    print(f"\n🚀 Testing live algorithm operations...")

    # Create live algorithm
    data = {
        "projectId": project_id,
        "compileId": compile_id,
        "serverType": "test-server",
    }
    response = requests.post(f"{BASE_URL}/live/create", headers=HEADERS, json=data)
    if response.status_code == 200:
        result = response.json()
        if result.get("success"):
            deploy_id = result["deployId"]
            print(f"✅ Live algorithm created: {deploy_id}")

            # Read live algorithm
            data = {"projectId": project_id, "deployId": deploy_id}
            response = requests.post(
                f"{BASE_URL}/live/read", headers=HEADERS, json=data
            )
            if response.status_code == 200:
                result = response.json()
                if result.get("success"):
                    print("✅ Live algorithm read successfully")
                    return True

            print("❌ Failed to read live algorithm")
            return False
        else:
            print(f"❌ Live algorithm creation failed: {result.get('errors')}")
            return False
    else:
        print(f"❌ Live algorithm creation failed: {response.status_code}")
        return False


def test_health_check():
    """Test health check endpoint"""
    print("\n💚 Testing health check...")
    response = requests.get("http://localhost:5001/health")
    if response.status_code == 200:
        result = response.json()
        print(f"✅ Server healthy: {result.get('status')}")
        print(f"   Projects: {result.get('projects_count')}")
        print(f"   Backtests: {result.get('backtests_count')}")
        return True
    else:
        print(f"❌ Health check failed: {response.status_code}")
        return False


def main():
    """Run all tests"""
    print("🧪 Testing Minimal Custom Cloud Server")
    print("=" * 50)

    # Test server health
    if not test_health_check():
        print("❌ Server is not running. Please start the server first.")
        return

    # Test authentication
    if not test_authentication():
        print("❌ Authentication failed. Check server and token.")
        return

    # Test project creation
    project_id = test_create_project()
    if not project_id:
        print("❌ Project creation failed.")
        return

    # Test file creation
    if not test_create_file(project_id):
        print("❌ File creation failed.")
        return

    # Test compilation
    compile_id = test_create_compile(project_id)
    if not compile_id:
        print("❌ Compilation failed.")
        return

    # Test compile status
    if not test_read_compile(project_id, compile_id):
        print("❌ Compile status check failed.")
        return

    # Test backtest creation
    backtest_id = test_create_backtest(project_id, compile_id)
    if not backtest_id:
        print("❌ Backtest creation failed.")
        return

    # Test backtest results
    if not test_read_backtest(project_id, backtest_id):
        print("❌ Backtest results check failed.")
        return

    # Test live operations
    if not test_live_operations(project_id, compile_id):
        print("❌ Live operations failed.")
        return

    print("\n" + "=" * 50)
    print("🎉 All tests passed! The minimal server is working correctly.")
    print("✅ You can now use this server with the LEAN framework.")


if __name__ == "__main__":
    main()
