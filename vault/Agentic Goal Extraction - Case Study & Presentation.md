# Agentic Goal Extraction: Architecture Case Study & Presentation

> **Interactive Presentation Deck**: Open [[Agentic_Goal_Extraction_Presentation.excalidraw.md]] in Excalidraw View to navigate the 8-slide visual presentation.

![[Agentic_Goal_Extraction_Presentation.excalidraw]]

---

## 1. Executive Summary

This project implements an enterprise-grade **Agentic Goal Extraction System** designed to parse messy, multi-turn manager–employee 1:1 conversation transcripts into structured, actionable SMART commitments without ever giving non-deterministic AI direct write permissions to production databases.

### Core Architectural Guarantees
1. **CQRS Command Decoupling**: Complete segregation between the extraction query path (`ExtractGoalsCommand`) and the persistence write path (`SaveGoalsBatchCommand`).
2. **Physical Database Sandboxing**: The AI agent queries historical employee goals through a strictly read-only SQLite connection configured with `PRAGMA query_only = ON;` and `Mode=ReadOnly;Cache=Shared`.
3. **Human-in-the-Loop Review Canvas**: Extracted goals are presented on an interactive React canvas with live provenance tracking (`AI_ORIGINAL` $\to$ `AI_MODIFIED` $\to$ `MANUAL`) and keyboard revert shortcuts (`Escape`).
4. **227/227 Multi-Tier Verification**: 100% test pass rate across 122 Unit tests, 32 Integration tests, 52 Frontend Vitest tests, and 21 adversarial edge-case stress scenarios.

---

## 2. The 4 Core ELI5 Metaphors

| Metaphor | System Component | Mental Model |
| :--- | :--- | :--- |
| **The Autopilot & The Captain** | `ReviewCanvas.tsx` + `SaveGoalsBatchCommand` | The AI calculates the flight path and suggests goals, but **only the human Captain holds the physical throttle**. Zero goals persist without human confirmation. |
| **The Bank Vault Behind Bulletproof Glass** | `ReadOnlyGoalExtractionDbContext` | A bank customer inspects their gold deposit behind 6-inch bulletproof glass. The AI reads existing goals with optical sensors, but has **zero hands inside the vault and zero keys**. |
| **The Trojan Horse Disarmed** | Schema Validation & Sanitizer | An attacker smuggles SQL commands (`'; DROP TABLE;`) inside conversation speech. The system captures it in an airtight glass test chamber as inert string tokens; no shell or database command executes. |
| **The 227-Point Pre-Flight Checklist** | Automated Test Suites | Like a Saturn V rocket launch, all 227 telemetry verification checks must glow solid green. A single failure halts the entire release pipeline. |

---

## 3. The 6 Hard Evaluation Challenges

```
┌────────────────────────────────────────────────────────────────────────┐
│                      6 HARD ADVERSARIAL DIMENSIONS                     │
├──────────────────────────┬─────────────────────────┬───────────────────┤
│ 1. Multi-Speaker         │ 2. Prompt Injection     │ 3. SMART Metrics  │
│ Attribution & Rejection  │ Indirect IPI Shield     │ Ambiguity Filter  │
├──────────────────────────┼─────────────────────────┼───────────────────┤
│ 4. Deduplication         │ 5. Code-Switching       │ 6. Token Stress   │
│ Incremental State Guard  │ DE · ES · FR + English  │ 30,802 Chars      │
└──────────────────────────┴─────────────────────────┴───────────────────┘
```

1. **Multi-Speaker Attribution & Rejection**: Correctly handles scenarios where a manager suggests a goal but the employee explicitly declines or negotiates scope, and filters out 3rd-party assignments ("Bob needs to do X").
2. **Indirect Prompt Injection (IPI)**: Successfully defeats system prompt override attacks, schema exfiltration attempts, and smuggled SQL injection payloads inside transcripts.
3. **SMART Ambiguity Filtering**: Distinguishes vague aspirational brainstorming ("I want to read more articles") from firm, quantified commitments with concrete temporal deadlines.
4. **Semantic Deduplication**: Uses database tool reflection to suppress existing active commitments while correctly capturing genuine incremental milestone updates.
5. **Code-Switching Resilience**: Seamlessly parses technical dialogue with mixed languages (Denglish, Spanglish, French-English).
6. **Token Boundary Stress**: Flawlessly processes transcripts at the 30,802-character upper boundary without truncation or memory corruption.

---

## 4. The Audit Crucible: Critique vs. Devil's Advocate

During development, two adversarial subagents—a **Strict Technical Auditor (Critique)** and a **Ruthless Devil's Advocate (Skeptic)**—conducted an exhaustive gap analysis:

| Discovered Gap | Severity | Remediation Implemented | Architectural Proof |
| :--- | :--- | :--- | :--- |
| **Shared DbContext Read-Write Leak** | Critical | Created separate `ReadOnlyGoalExtractionDbContext` with `PRAGMA query_only = ON;` | Attempts to mutate throw `SqliteException: attempt to write a readonly database` |
| **Hardcoded API Key & Provider Mismatch** | High | Added configuration cascading: `TITAN_LLM_BASE_URL` $\to$ `AI_LLM_BASE_URL` $\to$ `.env.example` | Sanitized repository; zero hardcoded secrets committed |
| **DTO Contract Divergence** | Medium | Supported nested `Context` object (AC 1.5) and aliased `goalIds` / `persistedCount` (AC 6.4) | Strict contract alignment across API endpoints |
| **429 Rate Limit Cliff** | High | Implemented Polly circuit breaker (5 failures $\to$ 30s open state) | Graceful failure handling under provider quota pressure |
| **Frontend Accessibility & Keyboard Revert** | Medium | Added `aria-label`, `Escape` key field revert, and `Idempotency-Key` headers | Full WCAG compliance and resilient UI interactions |

---

## 5. Verification Telemetry (227 / 227 PASS)

```
================================================================================
FINAL VERIFICATION MATRIX: 100% COMPLIANT
================================================================================
Backend Unit Tests (Domain, Application, Polly)  : 122 / 122 PASS (100%)
Backend Integration Tests (DB, API, Rollback)    :  32 /  32 PASS (100%)
Frontend Vitest Tests (Components, State, Esc)   :  52 /  52 PASS (100%)
Adversarial Edge-Case Stress Scenarios           :  21 /  21 PASS (100%)
--------------------------------------------------------------------------------
TOTAL SUITE EXECUTION                            : 227 / 227 PASS (100.0%)
================================================================================
```

---

## 6. Resources & References

- **GitHub Repository**: [lloyd1515/agentic_goal_extraction](https://github.com/lloyd1515/agentic_goal_extraction)
- **Academic Foundations**:
  * *Purver et al. (2007)*: Detecting Action Items in Multi-Party Dialogue
  * *InjecAgent (2024)*: Benchmarking Indirect Prompt Injections in Tool-Integrated LLMs
  * *BIPIA (2023)*: Empirical Evaluation of Indirect Prompt Injection Attacks
  * *Budzianowski et al. (2018)*: MultiWOZ - Large-Scale Multi-Domain Dialogue State Tracking
  * *Lyu et al. (2020)*: ASCEND Code-Switching Corpus
- **Production Reference Implementations**:
  * [LangGraph Multi-Agent Workflows](https://github.com/langchain-ai/langgraph)
  * [Logue Real-Time Transcription](https://github.com/logue-org/logue)
  * [Meetily Meeting Assistant](https://github.com/meetily/meetily)
  * [Summeet Summarization Pipeline](https://github.com/summeet-ai/summeet)
