#!/usr/bin/env bash
# ==============================================================================
# run_all_tests.sh
# Runs all backend unit tests, integration tests, and frontend vitest suite
# ==============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
REPO_DIR="$(cd "${BACKEND_DIR}/.." && pwd)"
FRONTEND_DIR="${REPO_DIR}/frontend"

echo "=========================================================="
echo "Running All Backend and Frontend Test Suites"
echo "=========================================================="

echo ">> 1. Running Backend Unit Tests..."
dotnet test "${BACKEND_DIR}/tests/GoalExtraction.UnitTests/GoalExtraction.UnitTests.csproj" --configuration Release --nologo

echo ">> 2. Running Backend Integration Tests..."
dotnet test "${BACKEND_DIR}/tests/GoalExtraction.IntegrationTests/GoalExtraction.IntegrationTests.csproj" --configuration Release --nologo

echo ">> 3. Running Frontend Vitest Suite..."
cd "${FRONTEND_DIR}"
npm test -- --run

echo "=========================================================="
echo "ALL TESTS COMPLETED SUCCESSFULLY"
echo "=========================================================="
