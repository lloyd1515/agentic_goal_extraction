# Resources, Research Papers, and Reference Repositories

This document compiles all foundational open-source repositories, academic research papers, evaluation benchmarks, enterprise meeting intelligence systems, and architectural standards referenced throughout the development, testing, and verification of the **Agentic Goal Extraction & Persistence Canvas** project.

---

## 1. Local Reference Repositories (`references/`)

The following open-source codebases served as design and architectural references for transcript processing, local AI agent orchestration, and human-in-the-loop review patterns:

| Repository / Project | Upstream URL | Description & Key Architectural Takeaways |
| :--- | :--- | :--- |
| **LangGraph** (LangChain AI) | [github.com/langchain-ai/langgraph](https://github.com/langchain-ai/langgraph) | Stateful multi-actor orchestration framework with native support for Human-in-the-Loop (`interrupt()`), durable state checkpointing, and tool execution boundaries. |
| **Logue** (Bitwize AI) | [github.com/bitwize-ai/Logue](https://github.com/bitwize-ai/Logue) | Native macOS on-device audio transcription and conversation intelligence application leveraging local models and structured agent tools. |
| **Meetily** (Zackriya Solutions) | [github.com/Zackriya-Solutions/meeting-minutes](https://github.com/Zackriya-Solutions/meeting-minutes) | Privacy-first offline meeting minutes generator built with Tauri, Rust, Whisper.cpp, Next.js, and local ONNX models. |
| **Summeet** (lyzgeorge) | [github.com/lyzgeorge/summeet](https://github.com/lyzgeorge/summeet) | Open-source meeting transcript summarizer supporting OpenAI-compatible endpoints (Groq, DeepSeek, Ollama, LocalAI). |
| **Smart AI Meeting Assistant** | Local reference | Mobile Flutter application reference for meeting audio recording, transcription parsing, and UI action item checklist rendering. |

---

## 2. Academic Research Papers & Industry Benchmarks

The 6 testing and evaluation dimensions developed for this system were designed based on empirical findings from the following academic research papers and benchmarks:

### Dimension 1: Multi-Speaker Attribution & Speech Acts
* **Purver et al. (ACL 2007)**: *"Detecting Action Items in Multi-Party Dialogue Using Speech Acts and Syntactic Patterns"*  
  Seminal research demonstrating why identifying action items requires distinguishing between **Directive** speech acts (manager assigns) and **Commissive** speech acts (employee commits), preventing false extractions when tasks are declined.  
  Link: [aclanthology.org/P07-1111](https://aclanthology.org/P07-1111/)
* **Alimeeting4MUG (ICASSP 2023)**: *"The Alimeeting Corpus and Benchmark for Multi-Speaker Dialogue Analysis and Action Item Detection"*  
  Multi-speaker conversational benchmark addressing speaker attribution handoffs and ownership transfer.  
  Link: [arxiv.org/abs/2301.03798](https://arxiv.org/abs/2301.03798)
* **AMI Meeting Corpus**: Standard multimodal research corpus with comprehensive dialogue act and action item annotations.  
  Link: [groups.inf.ed.ac.uk/ami/corpus](https://groups.inf.ed.ac.uk/ami/corpus/)

### Dimension 2: Indirect Prompt Injection & Tool Boundaries
* **InjecAgent (ACL 2024)**: *"Benchmarking Indirect Prompt Injections in Tool-Integrated Large Language Model Agents"*  
  Standardized benchmark evaluating how adversarial prompt injections embedded in external data attempt to hijack read-only database tools or exfiltrate private context.  
  Paper: [arxiv.org/abs/2403.02691](https://arxiv.org/abs/2403.02691) | GitHub: [github.com/uiuc-kang-lab/Injecagent](https://github.com/uiuc-kang-lab/Injecagent)
* **BIPIA (Microsoft Research, 2023)**: *"Benchmark for Indirect Prompt Injection Attacks"*  
  Evaluates instruction-data segregation and defensive boundary performance when parsing untrusted transcripts.  
  Paper: [arxiv.org/abs/2312.14197](https://arxiv.org/abs/2312.14197) | GitHub: [github.com/microsoft/BIPIA](https://github.com/microsoft/BIPIA)
* **ToolBench (OpenBMB, 2023)**: *"Tool Learning with Foundation Models"*  
  Evaluates parameter accuracy and tool boundary discipline in complex agent workflows.  
  Paper: [arxiv.org/abs/2307.16789](https://arxiv.org/abs/2307.16789) | GitHub: [github.com/OpenBMB/ToolBench](https://github.com/OpenBMB/ToolBench)
* **OWASP Top 10 for Large Language Model Applications**:  
  - `LLM01: Prompt Injection`  
  - `LLM02: Sensitive Information Disclosure`  
  - `LLM08: Excessive Agency` (addressed via physical CQRS zero-write isolation).  
  Link: [owasp.org/www-project-top-10-for-large-language-model-applications](https://owasp.org/www-project-top-10-for-large-language-model-applications/)

### Dimension 3: Ambiguity & SMART Commitments
* **SUTime / TimeML (Stanford NLP)**: *"Temporal Expression Extraction and Normalization in Natural Language Dialogue"*  
  Resolves relative and deictic temporal expressions ("in two sprints", "by end of Q3") to anchor valid timeframe deadlines.  
  Link: [nlp.stanford.edu/software/sutime.shtml](https://nlp.stanford.edu/software/sutime.shtml)
* **QMSum (NAACL 2021)**: *"A New Benchmark for Query-based Multi-domain Meeting Summarization"*  
  Evaluates extraction of concrete commitments versus aspirational brainstorming.  
  Paper: [arxiv.org/abs/2106.01229](https://arxiv.org/abs/2106.01229) | GitHub: [github.com/Yale-LILY/QMSum](https://github.com/Yale-LILY/QMSum)

### Dimension 4: Dialogue State Tracking & Deduplication
* **MultiWOZ 2.4 (2021)**: *"A Multi-Domain Wizard-of-Oz Dataset for Dialogue State Tracking and Goal Refinement"*  
  Standard benchmark for state delta tracking, incremental slot progress, and preventing duplicate entity creation.  
  Paper: [arxiv.org/abs/2104.00773](https://arxiv.org/abs/2104.00773) | GitHub: [github.com/smartyfh/MultiWOZ2.4](https://github.com/smartyfh/MultiWOZ2.4)
* **MeetingBank (ACL 2023)**: *"A Benchmark Dataset for Meeting Summarization and Action Item Tracking"*  
  Evaluates longitudinal tracking across recurring 1:1 and team meetings.  
  Link: [arxiv.org/abs/2305.14529](https://arxiv.org/abs/2305.14529)

### Dimension 5: Multilingual Code-Switching
* **ASCEND (HKUST, 2021)**: *"A Chinese-English Code-Switching Natural Dialogue Dataset"*  
  Spontaneous technical code-switching dialogue benchmark.  
  Paper: [arxiv.org/abs/2112.06223](https://arxiv.org/abs/2112.06223) | GitHub: [github.com/HLTCHKUST/ASCEND](https://github.com/HLTCHKUST/ASCEND)
* **LinCE Benchmark**: Language-in-Contrast Evaluation benchmark for linguistic code-switching.  
  Link: [ritual.uh.edu/lince](https://ritual.uh.edu/lince/)
* **Bangor-Miami Bilingual Corpus**: Natural conversational code-switching database.  
  Link: [biling.bangor.ac.uk](https://biling.bangor.ac.uk/)

### Dimension 6: Schema Integrity & General Agent Benchmarks
* **GAIA Benchmark (2023)**: *"General AI Assistants Benchmark"*  
  Evaluates agent tool-use accuracy, multimodal handling, and instruction following.  
  Paper: [arxiv.org/abs/2311.12983](https://arxiv.org/abs/2311.12983) | Hub: [huggingface.co/gaia-benchmark](https://huggingface.co/gaia-benchmark)
* **ChatEval (2023)**: *"Towards Multi-Agent Evaluation of Dialogue and Summarization"*  
  Link: [arxiv.org/abs/2308.07201](https://arxiv.org/abs/2308.07201)

---

## 3. Commercial Meeting Intelligence Systems Studied

| Product | URL | Core Capabilities & Workflow Analyzed |
| :--- | :--- | :--- |
| **Granola.ai** | [granola.ai](https://www.granola.ai/) | AI meeting notepad emphasizing human-in-the-loop review: extracts proposed bullet points and requires human approval before syncing. |
| **Otter.ai** | [otter.ai](https://otter.ai/) | Real-time speech transcription, speaker diarization, automated meeting summary, and action item detection. |
| **Fellow.app** | [fellow.app](https://fellow.app/) | Specialized 1:1 and team meeting software with bi-directional goal alignment, action items, and performance review tracking. |
| **Fireflies.ai** | [fireflies.ai](https://fireflies.ai/) | Automated conversation intelligence bot integrating with video conferencing to log action items and analytics. |

---

## 4. Architectural RFCs & Technical Standards

* **RFC 7807 (Problem Details for HTTP APIs)**: Standardized format for API error responses across the backend Web API.  
  Link: [datatracker.ietf.org/doc/html/rfc7807](https://datatracker.ietf.org/doc/html/rfc7807)
* **OAuth 2.1 RFC Specification (IETF Draft)**: Used in the sample test transcripts and seed goals (PKCE enforcement, deprecation of implicit grants).  
  Link: [datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1-10](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1-10)
* **OpenAI Chat Completions & Tool Calling Protocol**: OpenAI-compatible JSON schema for tool declarations and function calling.  
  Link: [platform.openai.com/docs/api-reference/chat](https://platform.openai.com/docs/api-reference/chat)
* **Chrome DevTools Protocol (CDP)**: Browser automation protocol utilized by the `chrome-devtools-mcp` suite for live UI end-to-end verification.  
  Link: [chromedevtools.github.io/devtools-protocol](https://chromedevtools.github.io/devtools-protocol/)
* **SQLite PRAGMA Statements**: Physical runtime sandboxing via `PRAGMA query_only = ON;` and `Mode=ReadOnly`.  
  Link: [sqlite.org/pragma.html](https://www.sqlite.org/pragma.html)

---

## 5. Core Libraries & Dependencies

### Backend (.NET 8 Clean Architecture)
* **Microsoft.AspNetCore.OpenApi** / **Swashbuckle**: Swagger UI and OpenAPI documentation generation.
* **Microsoft.EntityFrameworkCore.Sqlite**: SQLite database engine with isolated read-only and read-write connection pools.
* **MediatR**: CQRS pattern decoupling HTTP endpoints from query and command handlers.
* **FluentValidation.AspNetCore**: Strongly typed command validation pipeline.
* **Polly**: Resilient HTTP pipeline handling exponential backoff and circuit breaker protection (`CircuitBreakerAsync`).
* **xUnit**, **Moq**, **FluentAssertions**, **coverlet.collector**: Automated unit and integration testing suite.

### Frontend (React 18 + TypeScript + Vite)
* **React 18** & **Vite**: Single-page application build toolchain and component framework.
* **Tailwind CSS**: Dark slate responsive theme design.
* **Lucide React**: Iconography suite.
* **Vitest** & **React Testing Library**: Frontend component and reactive validation test suite.

---

## 6. Remote GitHub Repository

* **GitHub Remote Repository**: [https://github.com/lloyd1515/agentic_goal_extraction.git](https://github.com/lloyd1515/agentic_goal_extraction.git)
