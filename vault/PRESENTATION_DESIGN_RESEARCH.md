# High-Impact Visual Presentation Design Research
**Topic**: Agentic Goal Extraction with CQRS, Read-Only Database Sandbox, Human-in-the-Loop, and 227 Automated Tests  
**Aesthetic Standard**: Steve Jobs Keynote · Linear.app Dark Mode · Dieter Rams Minimalism · ByteByteGo Architectural Flow  
**Target Canvas**: Excalidraw 1920×1080 (16:9 Cinema Widescreen)

---

## 1. Executive Summary & Design Core

Presenting complex autonomous AI systems to both technical leaders and non-technical stakeholders requires radical simplification. When explaining high-assurance software:
1. **Explain Like I'm 5 (ELI5)**: Eliminate academic and technical jargon from slide surfaces. Replace abstract database isolation and prompt injection security with tangible real-world mental models (The Autopilot & The Captain, The Bank Vault Behind Bulletproof Glass, The Trojan Horse, The NASA Pre-Flight Checklist).
2. **Steve Jobs Keynote Rules**:
   - Zero bullet points. The Picture Superiority Effect shows 6x higher retention with visuals than text.
   - Max 3–6 words per card or badge. Fewer than 10 words total per slide.
   - One core idea per slide.
   - Every great story has a villain (the hazard of AI having write keys) and a hero (the deterministic CQRS architecture).
3. **Color Theory for Dark Canvas (Linear & Refactoring UI)**:
   - Avoid pure `#000000` (causes optical halation, ocular fatigue, and smearing).
   - Use calibrated obsidian slate: Base `#0A0D14`, Card Surface `#121722`, Hairline Border `#222F43`.
   - 60-30-10 Rule: 60% deep slate base, 30% structural cards/enclaves, 10% intentional semantic accents.
   - Exact Hex Codes & WCAG AAA Contrast:
     * **Electric Cyan** (`#38BDF8`): AI discovery, LLM inference, active data flow (9.07:1 AAA).
     * **Emerald Green** (`#34D399`): Safety, read-only lock, test pass, database commit (10.11:1 AAA).
     * **Amber Warning** (`#FBBF24`): Human gatekeeper, inline edits, review canvas (11.64:1 AAA).
     * **Rose Red** (`#FB7185`): Prompt injection attack, SQL mutation threat, circuit breaker trip (7.22:1 AAA).
     * **Indigo Periwinkle** (`#A5B4FC`): CQRS MediatR orchestration, synthesis (9.75:1 AAA).
     * **Text Primary** (`#F8FAFC`): Display headings (18.57:1 AAA).
     * **Text Secondary** (`#94A3B8`): Subtitles & labels (7.58:1 AAA).
4. **Dieter Rams Principles ("Weniger, aber besser")**:
   - Strip away container nesting ("matryoshka" boxes).
   - Visual illustrations dominate: vector graphics occupy $\ge 50\%$ of canvas area.
   - Clean 8pt spatial grid with generous whitespace (margins $\ge 80\text{px}$, card gutters $\ge 40\text{px}$).

---

## 2. The 4 Core ELI5 Metaphors

| Metaphor | Architecture Concept | Real-World Mental Model | Visual Graphic |
| :--- | :--- | :--- | :--- |
| **The Autopilot & The Captain** | Human-in-the-Loop (`ReviewCanvas.tsx` + `SaveGoalsBatchCommand`) | An airplane autopilot calculates wind and suggests waypoints, but **only the human Captain holds the physical throttle**. The plane cannot land without the pilot's hand. | Cockpit HUD: Holographic AI flight path (Cyan) + Pilot's master throttle lever (Amber) labeled "SAVE GOALS". |
| **The Bank Vault with Bulletproof Glass** | Physical Read-Only Database Sandbox (`PRAGMA query_only = ON;`, `Mode=ReadOnly`) | A customer checks their gold balance behind **6-inch bulletproof glass**. They can look and scan with their eyes, but have **zero physical openings, no arms inside the vault, and zero keys**. | Massive steel vault door behind an impenetrable glowing glass barrier. Laser scanner reads records; an attempted SQL write shatters into sparks against the glass. |
| **The Trojan Horse in Meeting Notes** | Indirect Prompt Injection Defeated (`Adversarial Benchmark`, Schema Validation) | An enemy hides an assassin inside a wooden horse disguised as meeting notes (`DROP ALL TABLES`). The guards place the horse inside a **sealed glass vacuum chamber**. Even if the assassin jumps out, there are no wires, no levers, and nowhere to go. | Transcript document with wooden horse. Scanning laser disarms attack into inert JSON tokens with zero system execution. |
| **The 227-Point Pre-Flight Checklist** | 227 Automated Tests (122 Unit + 32 Integration + 52 Vitest + 21 Adversarial) | A space rocket never launches on vibes. Before ignition, engineers verify a **227-point pre-flight checklist**. Every single LED must glow solid green. 226/227 is a no-go. | Mission control telemetry panel with a 10×23 matrix of glowing emerald LEDs and a central badge: `227 / 227 (100% PASS)`. |

---

## 3. Slide-by-Slide Storyboard (8 Frames, 1920×1080)

- **Slide 1: The Pipeline Flow (Conveyor)**
  * Headline: `Audio Transcript → AI Extraction → Human Review → Database`
  * Subtitle: `Zero unverified writes. From raw speech to immutable database records.`
  * Visual: 4-stage horizontal pipeline (Microphone Ingest $\to$ Sandboxed AI Extraction $\to$ Captain's Review Gate $\to$ Database Vault).

- **Slide 2: 6 Hard Adversarial Gates**
  * Headline: `6 Hard Adversarial Gates`
  * Subtitle: `Surviving real-world edge cases with mathematical precision.`
  * Visual: 2×3 matrix of tactical telemetry badges:
    1. Prompt Injection Shield (100% Blocked)
    2. Multi-Speaker Diarization (Speaker Separation)
    3. SMART Metric Guard (Strict Quantification)
    4. Semantic Deduplication (Suppresses Overlaps)
    5. Code-Switching (Denglish / Spanglish / French)
    6. 30k Token Stress Flood (Zero Context Exhaustion)

- **Slide 3: The Audit Crucible**
  * Headline: `Dual-Agent Crossfire: Auditor vs. Devil's Advocate`
  * Subtitle: `High-confidence goal extraction through ruthless adversarial debate.`
  * Visual: Bilateral duel with Scales of Justice. Left (Auditor in Cyan: detects ambiguity) vs Right (Devil's Advocate in Crimson: attacks feasibility). Bottom: Synthesized consensus.

- **Slide 4: Physical Read-Only Database Isolation**
  * Headline: `Hardware-Enforced Read-Only Isolation`
  * Subtitle: `Software checks can fail; physical database sandboxing guarantees zero writes.`
  * Visual: Bank Vault with Bulletproof Glass barrier (`PRAGMA query_only = ON;`), 3-state mechanical circuit breaker (Normal / Tripped / Reset), and Keyboard Escape Key.

- **Slide 5: Multi-Tier Verification Suite**
  * Headline: `227 / 227 Passing: Uncompromised Rigor`
  * Subtitle: `Zero flights launch until every green light is solid.`
  * Visual: Hero callout `227 / 227` (100% PASS) with 4 telemetry speedometer cards (122 Unit, 32 Integration, 52 Vitest, 21 Adversarial).

- **Slide 6: Live Browser Automation (Chrome DevTools MCP)**
  * Headline: `Live Browser Automation: Chrome DevTools MCP`
  * Subtitle: `Autonomous agent acts in real browser environments with deterministic proof.`
  * Visual: Framed Chromium browser window mockup showing DOM inspector, network request payloads, and 7-checkpoint verification runway.

- **Slide 7: Clean Architecture & Open-Source Artifacts**
  * Headline: `Clean Architecture & Open-Source Artifacts`
  * Subtitle: `Decoupled domain core, testable ports & adapters, production packaging.`
  * Visual: Clean Onion Architecture rings flowing into GitHub Octocat release repository with MIT License and full references.

- **Slide 8: The Autonomous Process Journey**
  * Headline: `The Autonomous Process Journey`
  * Subtitle: `From user prompt to production delivery: A subway transit map.`
  * Visual: Multi-line metro subway map with 6 station interchange hubs tracking the engineering trajectory.

---

## 4. Obsidian Excalidraw Engine Invariants (Strict Technical Rules)

1. **8-Character Blockref IDs**:
   - `obsidian-excalidraw-plugin` uses the regex `/\s\^(.{8})[\n]+/g` to bind text elements between markdown and drawing JSON.
   - Every text element ID in Excalidraw JSON MUST be mapped to an exact 8-character hash:
     ```python
     blockref = hashlib.sha256(el_id.encode()).hexdigest()[:8]
     ```
2. **Physical SVG Files & Embedded Files Declaration**:
   - All vector assets must exist as standalone files in `assets/excalidraw/<id>.svg`.
   - All files must be declared in `## Embedded Files` before `%% ## Drawing %%`:
     ```markdown
     ## Embedded Files
     asset_id: [[assets/excalidraw/asset_id.svg]]
     ```
3. **SVG Root Tag Dimensions**:
   - Every SVG must explicitly define:
     ```xml
     <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
     ```
   - Missing `viewBox` or `width`/`height` causes Electron to drop natural dimensions and render broken image place-holders.
4. **Z-Order Ordering**:
   - Frame child elements must be added to `elements[]` **before** the parent `frame` element.
   - Background rectangles must be added first within each frame.
5. **No Text Overflow**:
   - Card text widths must be calculated: `width = max_line_len * font_size * 0.6` and clamped to fit comfortably inside cards.
