# Agentic Goal Extraction & Persistence Canvas

> **A Production-Grade Human-in-the-Loop CQRS System for Extracting SMART Goals from 1:1 Discussion Transcripts with Strict Read-Only AI Sandboxing.**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![React 18](https://img.shields.io/badge/React-18.3-61DAFB.svg)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.5-3178C6.svg)](https://www.typescriptlang.org/)
[![Vite](https://img.shields.io/badge/Vite-5.4-646CFF.svg)](https://vitejs.dev/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-3.4-38B2AC.svg)](https://tailwindcss.com/)
[![Tests Passing](https://img.shields.io/badge/Tests-227%20Passed%20(100%25)-brightgreen.svg)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Overview

This repository implements an enterprise-ready, agentic system that ingests unstructured manager–employee 1:1 conversation transcripts, identifies actionable commitments via an OpenAI-compatible agent equipped with read-only database query tools, and presents proposed SMART goals on an interactive review canvas for human inspection, modification, and atomic batch persistence.

### Key Architectural Highlights

1. **Strict CQRS Decoupling**:
   - **AI Extraction Path**: `ExtractGoalsCommand` queries existing employee goals via an isolated, physically read-only database connection (`Mode=ReadOnly`, `PRAGMA query_only = ON;`) to deduplicate or refine deliverables. **Zero database write operations** are permitted during extraction.
   - **Persistence Path**: `SaveGoalsBatchCommand` handles human-approved or human-edited goals through an atomic transaction (`BeginTransactionAsync`). **Zero AI/LLM calls** are permitted during persistence.
2. **Physical Database Sandboxing (AC 4)**:
   - Separate connection pools for read-only vs. read-write access.
   - Interface segregation (`IReadOnlyGoalQueryRepository` contains zero mutating methods).
   - Read-only interceptor enforcing SQLite `PRAGMA query_only = ON;`.
3. **Multi-Tier AI Provider Cascade (AC 3)**:
   - Primary support for OpenAI-compatible Titan URL + Qwen model per specification.
   - Automatic fallback cascade: `TITAN_*` &rarr; `AI_LLM_*` &rarr; `GEMINI_*` &rarr; `appsettings.json`.
   - Resilience pipeline configured with Polly exponential retry and circuit breaker (`CircuitBreakerAsync(5, 30s)`).
4. **Human-in-the-Loop Review Canvas (AC 5)**:
   - Inline card editing for Title, Description, Category, Priority, Metric, and Timeframe.
   - Live provenance badge tracking: `AI Suggested` &rarr; `AI Suggested (Edited)` &rarr; `Manually Added`.
   - Keyboard `Escape` revert to undo unsaved field edits.
   - Responsive validation, deletion undo toast, and `Idempotency-Key` header generation.

---

## Project Structure

```
.
├── AcceptanceCriteria.md               # 6 Acceptance Criteria & Non-Functional Requirements
├── Description.md                      # High-level architecture and requirements
├── RESOURCES_AND_REFERENCES.md         # Academic papers, benchmarks & reference repos
├── .env.example                        # Template for environment configuration
├── implementation/
│   ├── backend/                        # .NET 8 Clean Architecture Solution
│   │   ├── src/
│   │   │   ├── GoalExtraction.Domain/          # Entities (Goal, Employee), Enums, Interfaces
│   │   │   ├── GoalExtraction.Application/     # CQRS Commands, Handlers, DTOs, Validators
│   │   │   ├── GoalExtraction.Infrastructure/  # AI Client, Tool Calling, DbContexts, Repos
│   │   │   └── GoalExtraction.Api/             # Web API Controllers, Middleware, Program.cs
│   │   └── tests/
│   │       ├── GoalExtraction.UnitTests/       # 122 Unit Tests (Handlers, Domain, Agent)
│   │       └── GoalExtraction.IntegrationTests/# 32 Integration Tests (E2E API, Security, Rollback)
│   ├── frontend/                       # React 18 + Vite + TypeScript Application
│   │   ├── src/
│   │   │   ├── components/                     # Header, TranscriptInput, ReviewCanvas, GoalCard
│   │   │   ├── services/                       # apiClient.ts, goalService.ts
│   │   │   └── types/                          # goal.ts (SMART validation logic)
│   │   └── tests/                              # 52 Vitest component and logic tests
│   ├── comprehensive_test_transcripts.json     # 21 multi-dimensional test transcripts
│   └── run_comprehensive_test_suite.py         # Automated test runner across all 21 scenarios
└── references/                         # Open-source references (LangGraph, Logue, Meetily, Summeet)
```

---

## Quick Start

### 1. Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js 18+](https://nodejs.org/) & `npm`
* Python 3.10+ (for edge case suite runner)

### 2. Configure Environment
Copy the example environment file:
```bash
cp .env.example .env
```
Provide your preferred OpenAI-compatible endpoint credentials in `.env` (Titan/Qwen, OpenAI, or Gemini).

### 3. Run Backend API
```bash
cd implementation/backend
dotnet run --project src/GoalExtraction.Api --launch-profile http
# API starts at http://localhost:5000 (Swagger UI at http://localhost:5000/swagger)
```

### 4. Run Frontend Canvas
```bash
cd implementation/frontend
npm install
npm run dev
# Frontend runs at http://localhost:5173
```

---

## Verification & Test Suites

The project features a **100% automated pass rate across 227 tests**:

```bash
# 1. Run Backend Unit Tests (122 tests)
dotnet test implementation/backend/tests/GoalExtraction.UnitTests

# 2. Run Backend Integration Tests (32 tests)
dotnet test implementation/backend/tests/GoalExtraction.IntegrationTests

# 3. Run Frontend Vitest Component Tests (52 tests)
cd implementation/frontend && npm test -- --run

# 4. Run 21-Scenario Edge Case & Adversarial Benchmark (21 tests)
python3 implementation/run_comprehensive_test_suite.py
```

### 6 Evaluation Dimensions Covered
1. **Multi-Speaker Attribution & Rejection**: Manager directives vs explicit employee decline; third-party task exclusions.
2. **Adversarial Prompt Injection & Tool Boundaries**: Smuggled SQL injection in error logs, system prompt jailbreaks defeated.
3. **Ambiguity & SMART Commitments**: Vague aspirations ignored; concrete metrics and deadlines extracted.
4. **Incremental Progress & Deduplication**: Database tool queries active employee goals to suppress duplicates.
5. **Multilingual Technical Code-Switching**: Evaluated on Denglish, Spanglish, and Franglais engineering discussions.
6. **Boundary & Delimiter Stress**: 30,802-character transcript near maximum token boundary (32,000 chars) successfully extracted and persisted.

---

## References & Research

For a detailed bibliography of academic papers (Purver et al., InjecAgent, BIPIA, MultiWOZ), benchmarks, and reference architectures, see [RESOURCES_AND_REFERENCES.md](RESOURCES_AND_REFERENCES.md).
