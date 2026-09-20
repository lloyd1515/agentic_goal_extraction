#!/usr/bin/env python3
"""
Agentic Goal Extraction - Presentation Deck Generator.
Implements ELI5 visual presentation design, Dieter Rams minimalism,
and Linear/Refactoring UI dark mode color theory.

Target: /home/vlads4r4/dev/side-projects/Agentic__Transcribe_Goal_Todolist/vault
Outputs:
  - vault/assets/excalidraw/*.svg (18 bespoke SVGs)
  - vault/Agentic_Goal_Extraction_Presentation.excalidraw.md
  - vault/Agentic_Goal_Extraction_Presentation.excalidraw
  - vault/Agentic Goal Extraction - Case Study & Presentation.md
"""

import json
import base64
import random
import hashlib
from pathlib import Path

random.seed(42)

# =============================================================================
# 18 BESPOKE SVG ASSETS (viewBox="0 0 100 100" width="100" height="100")
# Calibrated with dark mode color theory and high contrast
# =============================================================================
SVGS = {

    # --- Slide 1: Pipeline icons ---
    "svg_microphone": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <rect x="36" y="14" width="28" height="42" rx="14" fill="#38BDF8" stroke="#F8FAFC" stroke-width="2.5"/>
  <path d="M22 52 C22 68 78 68 78 52" fill="none" stroke="#F8FAFC" stroke-width="3" stroke-linecap="round"/>
  <line x1="50" y1="68" x2="50" y2="80" stroke="#F8FAFC" stroke-width="3" stroke-linecap="round"/>
  <line x1="36" y1="80" x2="64" y2="80" stroke="#F8FAFC" stroke-width="3" stroke-linecap="round"/>
  <circle cx="50" cy="35" r="5" fill="#121722"/>
</svg>''',

    "svg_mediatr_bus": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <rect x="12" y="38" width="76" height="24" rx="6" fill="#1E293B" stroke="#A5B4FC" stroke-width="2"/>
  <circle cx="24" cy="50" r="6" fill="#818CF8"/>
  <circle cx="50" cy="50" r="6" fill="#818CF8"/>
  <circle cx="76" cy="50" r="6" fill="#818CF8"/>
  <path d="M50 20 L50 38 M50 62 L50 80" stroke="#A5B4FC" stroke-width="3" stroke-linecap="round"/>
  <polygon points="50,14 56,26 44,26" fill="#FBBF24"/>
  <polygon points="50,86 56,74 44,74" fill="#FBBF24"/>
  <text x="50" y="54" font-family="sans-serif" font-size="10" fill="#F8FAFC" text-anchor="middle" font-weight="bold">MediatR</text>
</svg>''',

    "svg_ai_sandbox": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <rect x="14" y="14" width="72" height="72" rx="10" fill="none" stroke="#38BDF8" stroke-width="2.5" stroke-dasharray="6 3"/>
  <circle cx="50" cy="50" r="20" fill="#1E293B" stroke="#38BDF8" stroke-width="2"/>
  <circle cx="43" cy="46" r="3" fill="#38BDF8"/>
  <circle cx="57" cy="46" r="3" fill="#38BDF8"/>
  <path d="M42 57 Q50 63 58 57" fill="none" stroke="#38BDF8" stroke-width="2.5" stroke-linecap="round"/>
  <text x="50" y="84" font-family="sans-serif" font-size="9" fill="#38BDF8" text-anchor="middle" font-weight="bold">AI SANDBOX</text>
  <line x1="14" y1="14" x2="26" y2="26" stroke="#FB7185" stroke-width="2"/>
  <line x1="86" y1="14" x2="74" y2="26" stroke="#FB7185" stroke-width="2"/>
  <line x1="14" y1="86" x2="26" y2="74" stroke="#FB7185" stroke-width="2"/>
  <line x1="86" y1="86" x2="74" y2="74" stroke="#FB7185" stroke-width="2"/>
</svg>''',

    "svg_human_canvas": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <rect x="14" y="24" width="72" height="52" rx="6" fill="#1E293B" stroke="#FBBF24" stroke-width="2"/>
  <line x1="22" y1="36" x2="52" y2="36" stroke="#FBBF24" stroke-width="2.5" stroke-linecap="round"/>
  <line x1="22" y1="46" x2="70" y2="46" stroke="#94A3B8" stroke-width="2" stroke-linecap="round"/>
  <line x1="22" y1="56" x2="60" y2="56" stroke="#94A3B8" stroke-width="2" stroke-linecap="round"/>
  <circle cx="76" cy="62" r="10" fill="#FBBF24" stroke="#F8FAFC" stroke-width="2"/>
  <path d="M70 62 L75 67 L83 58" fill="none" stroke="#121722" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
  <path d="M48 14 L60 14 L60 24 L48 24 Z" fill="#FBBF24"/>
</svg>''',

    "svg_database_vault": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <ellipse cx="50" cy="28" rx="28" ry="10" fill="#1E293B" stroke="#34D399" stroke-width="2"/>
  <rect x="22" y="28" width="56" height="36" fill="#1E293B" stroke="#34D399" stroke-width="2"/>
  <ellipse cx="50" cy="64" rx="28" ry="10" fill="#1E293B" stroke="#34D399" stroke-width="2"/>
  <ellipse cx="50" cy="28" rx="28" ry="10" fill="#064E3B" stroke="#34D399" stroke-width="2"/>
  <ellipse cx="50" cy="46" rx="28" ry="10" fill="#064E3B" stroke="#34D399" stroke-width="1.5"/>
  <circle cx="78" cy="70" r="13" fill="#34D399" stroke="#F8FAFC" stroke-width="2"/>
  <rect x="74" y="66" width="8" height="6" rx="2" fill="#121722"/>
  <circle cx="78" cy="65" r="3" fill="none" stroke="#121722" stroke-width="2"/>
</svg>''',

    # --- Slide 2: Dimension badges ---
    "svg_speaker_bubbles": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <rect x="10" y="12" width="52" height="34" rx="8" fill="#1E293B" stroke="#38BDF8" stroke-width="2"/>
  <polygon points="22,46 22,58 34,46" fill="#38BDF8"/>
  <circle cx="30" cy="29" r="4" fill="#38BDF8"/>
  <circle cx="44" cy="29" r="4" fill="#38BDF8"/>
  <line x1="30" y1="34" x2="44" y2="34" stroke="#38BDF8" stroke-width="2" stroke-linecap="round"/>
  <rect x="38" y="48" width="52" height="34" rx="8" fill="#1E293B" stroke="#FB7185" stroke-width="2"/>
  <polygon points="78,82 78,94 66,82" fill="#FB7185"/>
  <path d="M48 62 L82 62 M48 72 L72 72" stroke="#FB7185" stroke-width="2.5" stroke-linecap="round"/>
  <circle cx="84" cy="18" r="10" fill="#FB7185" stroke="#F8FAFC" stroke-width="2"/>
  <path d="M79 18 L83 22 L89 14" fill="none" stroke="#F8FAFC" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
</svg>''',

    "svg_shield_sql": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <path d="M50 12 L82 24 C82 52 68 74 50 88 C32 74 18 52 18 24 Z" fill="#1E293B" stroke="#FB7185" stroke-width="2.5"/>
  <rect x="28" y="48" width="44" height="20" rx="4" fill="#4C0519"/>
  <text x="50" y="62" font-family="monospace" font-size="9" fill="#FB7185" text-anchor="middle" font-weight="bold">'; DROP TABLE</text>
  <line x1="28" y1="48" x2="72" y2="68" stroke="#FB7185" stroke-width="3" stroke-linecap="round"/>
  <line x1="72" y1="48" x2="28" y2="68" stroke="#FB7185" stroke-width="3" stroke-linecap="round"/>
</svg>''',

    "svg_bullseye_target": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <circle cx="50" cy="50" r="34" fill="none" stroke="#FBBF24" stroke-width="3"/>
  <circle cx="50" cy="50" r="24" fill="none" stroke="#FBBF24" stroke-width="3"/>
  <circle cx="50" cy="50" r="14" fill="#FBBF24"/>
  <path d="M60 40 L82 18 M74 18 L82 18 L82 26" stroke="#F8FAFC" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
  <circle cx="50" cy="50" r="4" fill="#121722"/>
</svg>''',

    "svg_dedup_lens": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <circle cx="44" cy="44" r="24" fill="#1E293B" stroke="#38BDF8" stroke-width="3"/>
  <line x1="62" y1="62" x2="84" y2="84" stroke="#38BDF8" stroke-width="5" stroke-linecap="round"/>
  <rect x="30" y="34" width="28" height="20" rx="4" fill="#0369A1"/>
  <line x1="32" y1="40" x2="52" y2="40" stroke="#F8FAFC" stroke-width="2" stroke-linecap="round"/>
  <line x1="32" y1="48" x2="46" y2="48" stroke="#F8FAFC" stroke-width="2" stroke-linecap="round"/>
  <rect x="60" y="14" width="28" height="18" rx="4" fill="#4C0519" stroke="#FB7185" stroke-width="2"/>
  <text x="74" y="26" font-family="sans-serif" font-size="9" fill="#FB7185" text-anchor="middle" font-weight="bold">DUP</text>
  <circle cx="60" cy="14" r="6" fill="#FB7185"/>
  <text x="60" y="18" font-family="sans-serif" font-size="9" fill="#121722" text-anchor="middle" font-weight="bold">✕</text>
</svg>''',

    "svg_multilingual_globe": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <circle cx="50" cy="50" r="30" fill="#1E293B" stroke="#A5B4FC" stroke-width="2"/>
  <path d="M50 20 Q62 35 62 50 Q62 65 50 80 Q38 65 38 50 Q38 35 50 20 Z" fill="#312E81" stroke="#A5B4FC" stroke-width="1.5"/>
  <line x1="20" y1="50" x2="80" y2="50" stroke="#A5B4FC" stroke-width="1.5"/>
  <line x1="24" y1="36" x2="76" y2="36" stroke="#A5B4FC" stroke-width="1.5"/>
  <line x1="24" y1="64" x2="76" y2="64" stroke="#A5B4FC" stroke-width="1.5"/>
  <rect x="8" y="10" width="18" height="12" rx="3" fill="#38BDF8"/>
  <text x="17" y="20" font-family="sans-serif" font-size="8" fill="#121722" text-anchor="middle" font-weight="bold">DE</text>
  <rect x="74" y="10" width="18" height="12" rx="3" fill="#FB7185"/>
  <text x="83" y="20" font-family="sans-serif" font-size="8" fill="#121722" text-anchor="middle" font-weight="bold">ES</text>
  <rect x="74" y="78" width="18" height="12" rx="3" fill="#34D399"/>
  <text x="83" y="88" font-family="sans-serif" font-size="8" fill="#121722" text-anchor="middle" font-weight="bold">FR</text>
</svg>''',

    "svg_gauge_tachometer": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <path d="M18 62 A34 34 0 0 1 82 62" fill="none" stroke="#1E293B" stroke-width="8" stroke-linecap="round"/>
  <path d="M18 62 A34 34 0 0 1 76 44" fill="none" stroke="#FBBF24" stroke-width="8" stroke-linecap="round"/>
  <line x1="50" y1="62" x2="72" y2="36" stroke="#F8FAFC" stroke-width="3.5" stroke-linecap="round"/>
  <circle cx="50" cy="62" r="5" fill="#F8FAFC"/>
  <text x="50" y="80" font-family="sans-serif" font-size="9" fill="#F8FAFC" text-anchor="middle" font-weight="bold">30,802 / 32,000</text>
  <text x="50" y="90" font-family="sans-serif" font-size="8" fill="#94A3B8" text-anchor="middle">chars</text>
</svg>''',

    # --- Slide 3: Audit crucible ---
    "svg_scales_justice": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <line x1="50" y1="14" x2="50" y2="82" stroke="#F8FAFC" stroke-width="3" stroke-linecap="round"/>
  <line x1="20" y1="32" x2="80" y2="32" stroke="#F8FAFC" stroke-width="3" stroke-linecap="round"/>
  <circle cx="50" cy="14" r="5" fill="#FBBF24"/>
  <path d="M20 32 Q12 46 20 52 Q28 46 20 32 Z" fill="#4C0519" stroke="#FB7185" stroke-width="1.5"/>
  <path d="M80 32 Q72 40 80 46 Q88 40 80 32 Z" fill="#064E3B" stroke="#34D399" stroke-width="1.5"/>
  <text x="20" y="64" font-family="sans-serif" font-size="8" fill="#FB7185" text-anchor="middle" font-weight="bold">CRITIC</text>
  <text x="80" y="56" font-family="sans-serif" font-size="8" fill="#34D399" text-anchor="middle" font-weight="bold">DEFENSE</text>
  <line x1="36" y1="82" x2="64" y2="82" stroke="#F8FAFC" stroke-width="3" stroke-linecap="round"/>
</svg>''',

    # --- Slide 4: Sandboxing ---
    "svg_circuit_breaker": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <line x1="14" y1="50" x2="34" y2="50" stroke="#38BDF8" stroke-width="4" stroke-linecap="round"/>
  <circle cx="34" cy="50" r="5" fill="#38BDF8"/>
  <line x1="34" y1="50" x2="50" y2="28" stroke="#38BDF8" stroke-width="4" stroke-linecap="round"/>
  <circle cx="66" cy="50" r="5" fill="#38BDF8"/>
  <line x1="66" y1="50" x2="86" y2="50" stroke="#38BDF8" stroke-width="4" stroke-linecap="round"/>
  <rect x="20" y="62" width="18" height="10" rx="3" fill="#FB7185"/>
  <text x="29" y="70" font-family="sans-serif" font-size="7" fill="#121722" text-anchor="middle" font-weight="bold">OPEN</text>
  <rect x="41" y="62" width="18" height="10" rx="3" fill="#FBBF24"/>
  <text x="50" y="70" font-family="sans-serif" font-size="6" fill="#121722" text-anchor="middle" font-weight="bold">HALF</text>
  <rect x="62" y="62" width="18" height="10" rx="3" fill="#34D399"/>
  <text x="71" y="70" font-family="sans-serif" font-size="6" fill="#121722" text-anchor="middle" font-weight="bold">CLOSED</text>
  <text x="50" y="86" font-family="sans-serif" font-size="9" fill="#38BDF8" text-anchor="middle" font-weight="bold">Polly Circuit Breaker</text>
</svg>''',

    "svg_escape_key": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <rect x="18" y="30" width="64" height="40" rx="8" fill="#1E293B" stroke="#94A3B8" stroke-width="2"/>
  <text x="50" y="56" font-family="sans-serif" font-size="16" fill="#F8FAFC" text-anchor="middle" font-weight="bold">ESC</text>
  <path d="M50 78 L50 88 L40 80 L50 78 Z" fill="#FBBF24"/>
  <path d="M50 78 L60 80 L50 88 L50 78 Z" fill="#F59E0B"/>
  <text x="50" y="22" font-family="sans-serif" font-size="9" fill="#94A3B8" text-anchor="middle">revert edits</text>
</svg>''',

    # --- Slide 5: Tests ---
    "svg_certificate_trophy": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <path d="M28 18 L72 18 L72 52 C72 66 60 72 50 72 C40 72 28 66 28 52 Z" fill="#FBBF24" stroke="#D97706" stroke-width="2.5"/>
  <path d="M28 26 L18 26 C14 26 14 40 20 42 L28 42" fill="none" stroke="#FBBF24" stroke-width="3" stroke-linecap="round"/>
  <path d="M72 26 L82 26 C86 26 86 40 80 42 L72 42" fill="none" stroke="#FBBF24" stroke-width="3" stroke-linecap="round"/>
  <circle cx="50" cy="47" r="12" fill="#121722"/>
  <path d="M44 47 L48 51 L57 42" fill="none" stroke="#34D399" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round"/>
  <rect x="36" y="72" width="28" height="6" rx="2" fill="#D97706"/>
  <rect x="30" y="78" width="40" height="6" rx="2" fill="#B45309"/>
  <text x="50" y="92" font-family="sans-serif" font-size="10" fill="#34D399" text-anchor="middle" font-weight="bold">227/227 ✓</text>
</svg>''',

    # --- Slide 6: DevTools ---
    "svg_browser_window": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <rect x="8" y="14" width="84" height="72" rx="8" fill="#0A0D14" stroke="#222F43" stroke-width="2"/>
  <rect x="8" y="14" width="84" height="20" rx="8" fill="#1E293B"/>
  <circle cx="18" cy="24" r="3.5" fill="#FB7185"/>
  <circle cx="27" cy="24" r="3.5" fill="#FBBF24"/>
  <circle cx="36" cy="24" r="3.5" fill="#34D399"/>
  <rect x="44" y="20" width="42" height="8" rx="3" fill="#121722" stroke="#334155" stroke-width="1"/>
  <rect x="8" y="68" width="84" height="18" rx="4" fill="#1E293B" stroke="#334155" stroke-width="1"/>
  <text x="12" y="80" font-family="monospace" font-size="7" fill="#34D399">▶ POST /api/goals/batch 201</text>
</svg>''',

    # --- Slide 7: GitHub ---
    "svg_github_octocat": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <circle cx="50" cy="42" r="26" fill="#F8FAFC"/>
  <path d="M50 52 C46 52 42 56 42 62 C42 68 44 72 50 72 C56 72 58 68 58 62 C58 56 54 52 50 52 Z" fill="#121722"/>
  <circle cx="42" cy="38" r="4" fill="#121722"/>
  <circle cx="58" cy="38" r="4" fill="#121722"/>
  <path d="M44 48 C44 44 38 38 30 40" fill="none" stroke="#121722" stroke-width="2.5"/>
  <path d="M56 48 C56 44 62 38 70 40" fill="none" stroke="#121722" stroke-width="2.5"/>
  <rect x="24" y="76" width="52" height="12" rx="4" fill="#15803D"/>
  <text x="50" y="86" font-family="sans-serif" font-size="9" fill="#F8FAFC" text-anchor="middle" font-weight="bold">lloyd1515 / agentic</text>
</svg>''',

    # --- Slide 8: Journey / Subway map ---
    "svg_subway_map": '''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100" width="100" height="100">
  <rect width="100" height="100" rx="18" fill="#121722" stroke="#222F43" stroke-width="2"/>
  <line x1="12" y1="50" x2="88" y2="50" stroke="#334155" stroke-width="4" stroke-linecap="round"/>
  <circle cx="12" cy="50" r="7" fill="#38BDF8" stroke="#F8FAFC" stroke-width="2"/>
  <circle cx="29" cy="50" r="7" fill="#FB7185" stroke="#F8FAFC" stroke-width="2"/>
  <circle cx="46" cy="50" r="7" fill="#818CF8" stroke="#F8FAFC" stroke-width="2"/>
  <circle cx="63" cy="50" r="7" fill="#FBBF24" stroke="#F8FAFC" stroke-width="2"/>
  <circle cx="80" cy="50" r="7" fill="#34D399" stroke="#F8FAFC" stroke-width="2"/>
  <circle cx="88" cy="50" r="7" fill="#10B981" stroke="#F8FAFC" stroke-width="2"/>
  <text x="12" y="68" font-family="sans-serif" font-size="6" fill="#38BDF8" text-anchor="middle">Intent</text>
  <text x="29" y="68" font-family="sans-serif" font-size="6" fill="#FB7185" text-anchor="middle">Audit</text>
  <text x="46" y="68" font-family="sans-serif" font-size="6" fill="#818CF8" text-anchor="middle">CQRS</text>
  <text x="63" y="68" font-family="sans-serif" font-size="6" fill="#FBBF24" text-anchor="middle">Sandbox</text>
  <text x="80" y="68" font-family="sans-serif" font-size="6" fill="#34D399" text-anchor="middle">227✓</text>
  <text x="88" y="68" font-family="sans-serif" font-size="6" fill="#10B981" text-anchor="middle">Release</text>
</svg>''',
}


# =============================================================================
# EXCALIDRAW BUILDER
# =============================================================================

class ExcalidrawBuilder:
    """Fluent primitives for building Excalidraw elements."""

    def __init__(self):
        self.elements = []
        self.files = {}
        self.text_block_refs = []
        self._init_files()

    def _init_files(self):
        for fid, svg_content in SVGS.items():
            b64 = base64.b64encode(svg_content.encode("utf-8")).decode("ascii")
            self.files[fid] = {
                "id": fid,
                "mimeType": "image/svg+xml",
                "dataURL": f"data:image/svg+xml;base64,{b64}",
                "created": 1726800000000,
            }

    def _base(self, el_id, el_type, x, y, w, h, angle=0.0):
        return {
            "id": el_id,
            "type": el_type,
            "x": round(x, 2),
            "y": round(y, 2),
            "width": round(w, 2),
            "height": round(h, 2),
            "angle": round(angle, 4),
            "strokeColor": "#F8FAFC",
            "backgroundColor": "transparent",
            "fillStyle": "solid",
            "strokeWidth": 2,
            "strokeStyle": "solid",
            "roughness": 0,
            "opacity": 100,
            "groupIds": [],
            "frameId": None,
            "roundness": None,
            "seed": random.randint(1000, 999999),
            "version": 1,
            "versionNonce": random.randint(1000, 999999),
            "isDeleted": False,
            "boundElements": None,
            "link": None,
            "locked": False,
        }

    def add_rect(self, el_id, x, y, w, h,
                 stroke_color="#222F43", bg_color="transparent",
                 fill_style="solid", roughness=0, stroke_width=2,
                 stroke_style="solid", roundness=None, frame_id=None, angle=0.0):
        el = self._base(el_id, "rectangle", x, y, w, h, angle)
        el["strokeColor"] = stroke_color
        el["backgroundColor"] = bg_color
        el["fillStyle"] = fill_style
        el["roughness"] = roughness
        el["strokeWidth"] = stroke_width
        el["strokeStyle"] = stroke_style
        el["frameId"] = frame_id
        if roundness:
            el["roundness"] = roundness if isinstance(roundness, dict) else {"type": 3}
        self.elements.append(el)
        return el

    def add_text(self, el_id, x, y, text_str, font_size=20, font_family=1,
                 stroke_color="#F8FAFC", text_align="left", frame_id=None, angle=0.0):
        lines = text_str.split("\n")
        char_w = font_size * (0.6 if font_family == 1 else 0.62)
        line_h = font_size * 1.3
        w = max(40, max(len(l) for l in lines) * char_w)
        h = max(25, len(lines) * line_h)
        # Obsidian Excalidraw plugin strictly requires EXACTLY 8-char blockref IDs
        safe_id = hashlib.sha256(el_id.encode("utf-8")).hexdigest()[:8]
        el = self._base(safe_id, "text", x, y, w, h, angle)
        el["text"] = text_str
        el["originalText"] = text_str
        el["fontSize"] = font_size
        el["fontFamily"] = font_family
        el["textAlign"] = text_align
        el["verticalAlign"] = "top"
        el["strokeColor"] = stroke_color
        el["frameId"] = frame_id
        el["baseline"] = int(font_size * 0.9)
        el["containerId"] = None
        el["lineHeight"] = 1.25
        self.elements.append(el)
        self.text_block_refs.append((text_str, safe_id))
        return el

    def add_line(self, el_id, x, y, points, stroke_color="#F8FAFC", stroke_width=2,
                 stroke_style="solid", roughness=0, frame_id=None,
                 start_arrow=None, end_arrow=None, angle=0.0):
        xs = [p[0] for p in points]
        ys = [p[1] for p in points]
        w = max(xs) - min(xs) if xs else 10
        h = max(ys) - min(ys) if ys else 10
        el_type = "arrow" if (start_arrow or end_arrow) else "line"
        el = self._base(el_id, el_type, x, y, w, h, angle)
        el["points"] = points
        el["strokeColor"] = stroke_color
        el["strokeWidth"] = stroke_width
        el["strokeStyle"] = stroke_style
        el["roughness"] = roughness
        el["frameId"] = frame_id
        el["startArrowhead"] = start_arrow
        el["endArrowhead"] = end_arrow
        el["elbowed"] = False
        self.elements.append(el)
        return el

    def add_image(self, el_id, x, y, w, h, file_id, frame_id=None, angle=0.0):
        el = self._base(el_id, "image", x, y, w, h, angle)
        el["strokeColor"] = "transparent"
        el["backgroundColor"] = "transparent"
        el["fileId"] = file_id
        el["status"] = "saved"
        el["scale"] = [1, 1]
        el["frameId"] = frame_id
        self.elements.append(el)
        return el

    def add_frame(self, frame_id, name, x, y, w=1920, h=1080):
        """Must be added AFTER all its children."""
        el = self._base(frame_id, "frame", x, y, w, h)
        el["name"] = name
        el["strokeColor"] = "#334155"
        el["backgroundColor"] = "#0A0D14"
        el["strokeWidth"] = 1
        el["roughness"] = 0
        self.elements.append(el)
        return el

    def pill(self, prefix, x, y, w, h, label, bg, stroke, text_color="#F8FAFC",
             font_size=16, frame_id=None):
        self.add_rect(f"{prefix}_bg", x, y, w, h, stroke_color=stroke, bg_color=bg,
                      fill_style="solid", roughness=0, stroke_width=2,
                      roundness={"type": 3}, frame_id=frame_id)
        self.add_text(f"{prefix}_lbl", x + 16, y + (h - font_size * 1.3) / 2,
                      label, font_size=font_size, stroke_color=text_color,
                      frame_id=frame_id)


# =============================================================================
# SLIDE 1 — Pipeline Flow
# =============================================================================

def slide_1(b, fx, fy, fid):
    """5 Station Pods + Directional Arrows. ByteByteGo left-to-right flow."""
    b.add_rect("s1_bg", fx, fy, 1920, 1080, stroke_color="#0A0D14", bg_color="#0A0D14",
               fill_style="solid", roughness=0, stroke_width=0, frame_id=fid)

    b.add_text("s1_title", fx + 60, fy + 36,
               "Agentic Goal Extraction", font_size=62, stroke_color="#F8FAFC",
               font_family=1, frame_id=fid)
    b.add_text("s1_subtitle", fx + 62, fy + 125,
               "CQRS · Human-in-the-Loop · 227/227 Tests", font_size=26,
               stroke_color="#94A3B8", font_family=1, frame_id=fid)

    pods = [
        ("svg_microphone",   "1. Audio Ingest",  "#38BDF8", "#121722"),
        ("svg_mediatr_bus",   "2. MediatR Bus",   "#A5B4FC", "#121722"),
        ("svg_ai_sandbox",    "3. AI Sandbox",    "#818CF8", "#121722"),
        ("svg_human_canvas",  "4. Human Review",  "#FBBF24", "#121722"),
        ("svg_database_vault", "5. DB Vault",     "#34D399", "#121722"),
    ]
    pod_w, pod_h = 240, 320
    gap = (1920 - 5 * pod_w) // 6
    py = fy + 300

    for i, (svg_id, label, accent, bg) in enumerate(pods):
        px = fx + gap + i * (pod_w + gap)
        pid = f"s1_pod{i}"
        b.add_rect(f"{pid}_card", px, py, pod_w, pod_h,
                   stroke_color=accent, bg_color=bg,
                   fill_style="solid", roughness=0, stroke_width=3,
                   roundness={"type": 3}, frame_id=fid)
        b.add_image(f"{pid}_img", px + 30, py + 25, 180, 180, svg_id, frame_id=fid)
        b.add_text(f"{pid}_lbl", px + 20, py + 260, label,
                   font_size=20, stroke_color=accent, font_family=1, frame_id=fid)

        if i < 4:
            ax = px + pod_w + 4
            ay = py + pod_h // 2
            nx = px + pod_w + gap - 4
            b.add_line(f"{pid}_arr", ax, ay,
                       [[0, 0], [nx - ax, 0]],
                       stroke_color="#38BDF8", stroke_width=3,
                       frame_id=fid, end_arrow="arrow")

    b.pill("s1_badge", fx + 60, fy + 980, 540, 44,
           "github.com/lloyd1515/agentic_goal_extraction",
           "#121722", "#222F43", text_color="#94A3B8", font_size=15, frame_id=fid)
    b.add_text("s1_tests", fx + 1620, fy + 980,
               "227 / 227  100%", font_size=28, stroke_color="#34D399",
               font_family=1, frame_id=fid)


# =============================================================================
# SLIDE 2 — 6 Hard Adversarial Gates
# =============================================================================

def slide_2(b, fx, fy, fid):
    b.add_rect("s2_bg", fx, fy, 1920, 1080, stroke_color="#0A0D14", bg_color="#0A0D14",
               fill_style="solid", roughness=0, stroke_width=0, frame_id=fid)

    b.add_text("s2_title", fx + 60, fy + 32,
               "6 Hard Adversarial Gates", font_size=52, stroke_color="#F8FAFC",
               font_family=1, frame_id=fid)
    b.add_text("s2_sub", fx + 62, fy + 96,
               "Surviving real-world edge cases with mathematical precision.",
               font_size=22, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    cards = [
        ("svg_speaker_bubbles",  "Multi-Speaker",   "Attribution & Rejection",  "#38BDF8"),
        ("svg_shield_sql",       "Adversarial IPI",  "Prompt Injection Shield",  "#FB7185"),
        ("svg_bullseye_target",  "SMART Goals",      "Ambiguity Filter",         "#FBBF24"),
        ("svg_dedup_lens",       "Deduplication",    "Incremental State Guard",  "#34D399"),
        ("svg_multilingual_globe", "Code-Switching", "DE · ES · FR + EN",        "#A5B4FC"),
        ("svg_gauge_tachometer", "Token Stress",     "30,802 / 32,000 chars",    "#FBBF24"),
    ]

    cw, ch = 520, 360
    cols, rows = 3, 2
    gx = (1920 - cols * cw) // (cols + 1)
    gy = (1080 - 150 - rows * ch) // (rows + 1)

    for i, (svg_id, title, sub, accent) in enumerate(cards):
        col = i % cols
        row = i // cols
        cx = fx + gx + col * (cw + gx)
        cy = fy + 140 + gy + row * (ch + gy)
        cid = f"s2_c{i}"
        b.add_rect(f"{cid}_bg", cx, cy, cw, ch,
                   stroke_color=accent, bg_color="#121722",
                   fill_style="solid", roughness=0, stroke_width=2,
                   roundness={"type": 3}, frame_id=fid)
        b.add_image(f"{cid}_img", cx + 30, cy + 20, 180, 180, svg_id, frame_id=fid)
        b.add_text(f"{cid}_t", cx + 24, cy + 230, title,
                   font_size=28, stroke_color=accent, font_family=1, frame_id=fid)
        b.add_text(f"{cid}_s", cx + 24, cy + 276, sub,
                   font_size=18, stroke_color="#94A3B8", font_family=1, frame_id=fid)


# =============================================================================
# SLIDE 3 — The Audit Crucible
# =============================================================================

def slide_3(b, fx, fy, fid):
    b.add_rect("s3_bg", fx, fy, 1920, 1080, stroke_color="#0A0D14", bg_color="#0A0D14",
               fill_style="solid", roughness=0, stroke_width=0, frame_id=fid)

    b.add_text("s3_title", fx + 60, fy + 32,
               "The Audit Crucible", font_size=52, stroke_color="#F8FAFC",
               font_family=1, frame_id=fid)
    b.add_text("s3_sub", fx + 62, fy + 98,
               "Critique  ←  Scales of Justice  →  Devil's Advocate",
               font_size=22, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    b.add_image("s3_scales", fx + 760, fy + 160, 400, 400, "svg_scales_justice", frame_id=fid)

    gaps = [
        ("READ-WRITE LEAK",   "PHYSICAL LOCK"),
        ("HARDCODED KEY",     ".ENV ENCRYPTED"),
        ("SCHEMA DRIFT",      "DTO ALIGNED"),
        ("429 QUOTA CLIFF",   "CIRCUIT BREAKER"),
    ]
    for i, (bad, good) in enumerate(gaps):
        row = i // 2
        col = i % 2
        rx = fx + 80 + col * 310
        ry = fy + 580 + row * 190
        b.add_rect(f"s3_gap{i}", rx, ry, 280, 80,
                   stroke_color="#FB7185", bg_color="#121722",
                   fill_style="solid", roughness=0, stroke_width=2,
                   roundness={"type": 3}, frame_id=fid)
        b.add_text(f"s3_gap{i}_t", rx + 16, ry + 26, bad,
                   font_size=18, stroke_color="#FB7185", font_family=1, frame_id=fid)

        gx2 = fx + 1560 - col * 310
        b.add_rect(f"s3_fix{i}", gx2, ry, 280, 80,
                   stroke_color="#34D399", bg_color="#121722",
                   fill_style="solid", roughness=0, stroke_width=2,
                   roundness={"type": 3}, frame_id=fid)
        b.add_text(f"s3_fix{i}_t", gx2 + 16, ry + 26, good,
                   font_size=18, stroke_color="#34D399", font_family=1, frame_id=fid)


# =============================================================================
# SLIDE 4 — Hardware-Enforced Sandboxing
# =============================================================================

def slide_4(b, fx, fy, fid):
    b.add_rect("s4_bg", fx, fy, 1920, 1080, stroke_color="#0A0D14", bg_color="#0A0D14",
               fill_style="solid", roughness=0, stroke_width=0, frame_id=fid)

    b.add_text("s4_title", fx + 60, fy + 32,
               "Physical Database Sandboxing", font_size=52, stroke_color="#F8FAFC",
               font_family=1, frame_id=fid)
    b.add_text("s4_sub", fx + 62, fy + 98,
               "The AI can inspect everything, but touch nothing.",
               font_size=22, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    cols = [
        ("svg_database_vault",  "DB Vault",          "ReadOnly + PRAGMA query_only", "#34D399"),
        ("svg_circuit_breaker", "Circuit Breaker",   "Polly: Open / Half / Closed",  "#38BDF8"),
        ("svg_escape_key",      "Escape Key",        "Revert · Provenance Badges",   "#FBBF24"),
    ]
    col_w = 520
    gx = (1920 - 3 * col_w) // 4
    for i, (svg_id, title, sub, accent) in enumerate(cols):
        cx = fx + gx + i * (col_w + gx)
        cy = fy + 140
        cid = f"s4_c{i}"
        b.add_rect(f"{cid}_bg", cx, cy, col_w, 800,
                   stroke_color=accent, bg_color="#121722",
                   fill_style="solid", roughness=0, stroke_width=3,
                   roundness={"type": 3}, frame_id=fid)
        b.add_image(f"{cid}_img", cx + 60, cy + 40, 400, 400, svg_id, frame_id=fid)
        b.add_text(f"{cid}_t", cx + 24, cy + 490, title,
                   font_size=32, stroke_color=accent, font_family=1, frame_id=fid)
        b.add_text(f"{cid}_s", cx + 24, cy + 540, sub,
                   font_size=20, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    b.add_rect("s4_code_bg", fx + 360, fy + 980, 1200, 60,
               stroke_color="#222F43", bg_color="#121722",
               fill_style="solid", roughness=0, stroke_width=1,
               roundness={"type": 3}, frame_id=fid)
    b.add_text("s4_code", fx + 390, fy + 996,
               "PRAGMA query_only = ON;   ·   Mode=ReadOnly;Cache=Shared",
               font_size=20, font_family=5, stroke_color="#38BDF8", frame_id=fid)


# =============================================================================
# SLIDE 5 — 227/227 Tests Dashboard
# =============================================================================

def slide_5(b, fx, fy, fid):
    b.add_rect("s5_bg", fx, fy, 1920, 1080, stroke_color="#0A0D14", bg_color="#0A0D14",
               fill_style="solid", roughness=0, stroke_width=0, frame_id=fid)

    b.add_text("s5_title", fx + 60, fy + 32,
               "Verification Suite: 227 / 227 PASS", font_size=52, stroke_color="#F8FAFC",
               font_family=1, frame_id=fid)
    b.add_text("s5_sub", fx + 62, fy + 96,
               "Zero flights launch until every green light is solid.",
               font_size=22, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    metrics = [
        ("122", "Unit Tests",      "#38BDF8"),
        ("32",  "Integration",     "#34D399"),
        ("52",  "Vitest (React)",  "#A5B4FC"),
        ("21",  "Edge-Cases",      "#FBBF24"),
    ]
    mw, mh = 380, 260
    mx_start = fx + (1920 - 4 * mw - 3 * 40) // 2
    my = fy + 140
    for i, (num, label, accent) in enumerate(metrics):
        mx = mx_start + i * (mw + 40)
        mid = f"s5_m{i}"
        b.add_rect(f"{mid}_bg", mx, my, mw, mh,
                   stroke_color=accent, bg_color="#121722",
                   fill_style="solid", roughness=0, stroke_width=3,
                   roundness={"type": 3}, frame_id=fid)
        b.add_text(f"{mid}_num", mx + 30, my + 30, num,
                   font_size=96, stroke_color=accent, font_family=1, frame_id=fid)
        b.add_text(f"{mid}_lbl", mx + 30, my + 190, label,
                   font_size=24, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    b.add_image("s5_trophy", fx + 680, fy + 440, 560, 560, "svg_certificate_trophy", frame_id=fid)
    b.add_text("s5_total", fx + 640, fy + 1010,
               "227 / 227  ·  100 % PASS", font_size=44, stroke_color="#34D399",
               font_family=1, frame_id=fid)


# =============================================================================
# SLIDE 6 — Live Browser Automation Proof
# =============================================================================

def slide_6(b, fx, fy, fid):
    b.add_rect("s6_bg", fx, fy, 1920, 1080, stroke_color="#0A0D14", bg_color="#0A0D14",
               fill_style="solid", roughness=0, stroke_width=0, frame_id=fid)

    b.add_text("s6_title", fx + 60, fy + 32,
               "Live Browser Automation Proof", font_size=52, stroke_color="#F8FAFC",
               font_family=1, frame_id=fid)
    b.add_text("s6_sub", fx + 62, fy + 96,
               "Chrome DevTools MCP executing live DOM actions and assertions.",
               font_size=22, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    b.add_image("s6_browser", fx + 60, fy + 140, 900, 800, "svg_browser_window", frame_id=fid)

    checkpoints = [
        ("1", "App Loaded & Responsive",  "#38BDF8"),
        ("2", "Goals Extracted (AI)",     "#34D399"),
        ("3", "30,802 Char Boundary OK",  "#FBBF24"),
        ("4", "Jailbreak Defeated",       "#FB7185"),
        ("5", "Batch Atomically Saved",   "#34D399"),
        ("6", "Provenance Badges Valid",  "#A5B4FC"),
        ("7", "201 Created Status",       "#38BDF8"),
    ]
    bx = fx + 1020
    by_start = fy + 140
    bh = 95
    gap_b = 20
    for i, (num, label, accent) in enumerate(checkpoints):
        by = by_start + i * (bh + gap_b)
        bid = f"s6_chk{i}"
        b.add_rect(f"{bid}_bg", bx, by, 840, bh,
                   stroke_color=accent, bg_color="#121722",
                   fill_style="solid", roughness=0, stroke_width=2,
                   roundness={"type": 3}, frame_id=fid)
        b.add_text(f"{bid}_num", bx + 20, by + 22,
                   f"✓ {num}", font_size=32, stroke_color=accent,
                   font_family=1, frame_id=fid)
        b.add_text(f"{bid}_lbl", bx + 100, by + 28, label,
                   font_size=24, stroke_color="#F8FAFC", font_family=1, frame_id=fid)


# =============================================================================
# SLIDE 7 — Production Release & GitHub
# =============================================================================

def slide_7(b, fx, fy, fid):
    b.add_rect("s7_bg", fx, fy, 1920, 1080, stroke_color="#0A0D14", bg_color="#0A0D14",
               fill_style="solid", roughness=0, stroke_width=0, frame_id=fid)

    b.add_text("s7_title", fx + 60, fy + 32,
               "Production Release & Artifacts", font_size=52, stroke_color="#F8FAFC",
               font_family=1, frame_id=fid)
    b.add_text("s7_sub", fx + 62, fy + 96,
               "Decoupled domain core, testable ports, zero leaked secrets.",
               font_size=22, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    b.add_image("s7_octo", fx + 660, fy + 120, 600, 580, "svg_github_octocat", frame_id=fid)

    b.pill("s7_commit", fx + 700, fy + 720, 520, 68,
           "commit  ffaa2f1  ·  Release v1.0", "#121722", "#34D399", text_color="#34D399",
           font_size=22, frame_id=fid)

    refs = [
        ("LangGraph",  "#38BDF8"),
        ("Logue",      "#34D399"),
        ("Meetily",    "#A5B4FC"),
        ("Summeet",    "#FBBF24"),
    ]
    pw, ph = 350, 80
    px_start = fx + (1920 - 4 * pw - 3 * 30) // 2
    for i, (label, accent) in enumerate(refs):
        px = px_start + i * (pw + 30)
        b.add_rect(f"s7_ref{i}", px, fy + 840, pw, ph,
                   stroke_color=accent, bg_color="#121722",
                   fill_style="solid", roughness=0, stroke_width=2,
                   roundness={"type": 3}, frame_id=fid)
        b.add_text(f"s7_ref{i}_t", px + 24, fy + 858, label,
                   font_size=24, stroke_color=accent, font_family=1, frame_id=fid)

    b.add_text("s7_secret", fx + 62, fy + 980,
               "Secrets sanitized  ·  .env.example committed  ·  MIT License",
               font_size=20, stroke_color="#94A3B8", font_family=1, frame_id=fid)


# =============================================================================
# SLIDE 8 — Autonomous Process Journey Map
# =============================================================================

def slide_8(b, fx, fy, fid):
    b.add_rect("s8_bg", fx, fy, 1920, 1080, stroke_color="#0A0D14", bg_color="#0A0D14",
               fill_style="solid", roughness=0, stroke_width=0, frame_id=fid)

    b.add_text("s8_title", fx + 60, fy + 32,
               "The Autonomous Process Journey", font_size=52, stroke_color="#F8FAFC",
               font_family=1, frame_id=fid)
    b.add_text("s8_sub", fx + 62, fy + 96,
               "From prompt to production release: A subway transit map.",
               font_size=22, stroke_color="#94A3B8", font_family=1, frame_id=fid)

    b.add_image("s8_subway", fx + 110, fy + 160, 1700, 600, "svg_subway_map", frame_id=fid)

    stations = [
        ("1. Intent Gate",    "User prompt\n& clarification", "#38BDF8"),
        ("2. Audit Crucible", "Critique + Devil\nAdv dual-agent",  "#FB7185"),
        ("3. CQRS Build",     "Backend +\nFrontend clean",   "#A5B4FC"),
        ("4. Sandboxing",     "Read-only DB\n+ circuit break", "#FBBF24"),
        ("5. 227 Tests",      "100% pass rate\nunit+e2e+edge",   "#34D399"),
        ("6. Release",        "GitHub push\n+ bibliography", "#10B981"),
    ]
    sw, sh = 260, 200
    sx_start = fx + (1920 - 6 * sw - 5 * 20) // 2
    for i, (title, desc, accent) in enumerate(stations):
        sx = sx_start + i * (sw + 20)
        sy = fy + 820
        sid = f"s8_st{i}"
        b.add_rect(f"{sid}_bg", sx, sy, sw, sh,
                   stroke_color=accent, bg_color="#121722",
                   fill_style="solid", roughness=0, stroke_width=2,
                   roundness={"type": 3}, frame_id=fid)
        b.add_text(f"{sid}_t", sx + 14, sy + 18, title,
                   font_size=20, stroke_color=accent, font_family=1, frame_id=fid)
        b.add_text(f"{sid}_d", sx + 14, sy + 60, desc,
                   font_size=15, stroke_color="#94A3B8", font_family=1, frame_id=fid)


# =============================================================================
# ASSEMBLY
# =============================================================================

GRID = [
    (0,    0,    "01 — Pipeline Flow",      slide_1),
    (2300, 0,    "02 — 6 Dimensions",       slide_2),
    (4600, 0,    "03 — Audit Crucible",     slide_3),
    (6900, 0,    "04 — Sandboxing",         slide_4),
    (0,    1380, "05 — Test Dashboard",     slide_5),
    (2300, 1380, "06 — DevTools Evidence",  slide_6),
    (4600, 1380, "07 — GitHub Release",     slide_7),
    (6900, 1380, "08 — Journey Map",        slide_8),
]


def build_deck():
    b = ExcalidrawBuilder()
    for i, (fx, fy, name, fn) in enumerate(GRID):
        fid = f"frame_{i+1:02d}"
        fn(b, fx, fy, fid)              # Children first
        b.add_frame(fid, name, fx, fy)  # Frame last
    return b


def generate_dual_format(builder):
    excalidraw_data = {
        "type": "excalidraw",
        "version": 2,
        "source": "https://excalidraw.com",
        "elements": builder.elements,
        "appState": {
            "gridSize": 20,
            "viewBackgroundColor": "#0A0D14",
            "theme": "dark",
        },
        "files": builder.files,
    }

    # Obsidian text-element block references
    lines = []
    for text_str, el_id in builder.text_block_refs:
        parts = text_str.split("\n")
        for j, p in enumerate(parts):
            if j == len(parts) - 1:
                lines.append(f"{p} ^{el_id}")
            else:
                lines.append(p)
        lines.append("")
    text_section = "\n".join(lines)

    # Obsidian embedded files section referencing dedicated vault SVG files
    embedded_files_lines = []
    for fid in builder.files.keys():
        embedded_files_lines.append(f"{fid}: [[assets/excalidraw/{fid}.svg]]\n")
    embedded_files_section = "\n".join(embedded_files_lines)

    json_str = json.dumps(excalidraw_data, indent=2, ensure_ascii=False)

    md = f"""---

excalidraw-plugin: parsed
tags: [excalidraw, agentic-ai, goal-extraction, cqrs, hitl, forensic-audit]

---
==⚠  Switch to EXCALIDRAW VIEW in the MORE OPTIONS menu of this document. ⚠==

# Excalidraw Data

## Text Elements

{text_section}
## Embedded Files

{embedded_files_section}
%%
## Drawing
```json
{json_str}
```
%%
"""
    return excalidraw_data, md


def main():
    vault = Path("/home/vlads4r4/dev/side-projects/Agentic__Transcribe_Goal_Todolist/vault")
    assets_dir = vault / "assets" / "excalidraw"
    assets_dir.mkdir(parents=True, exist_ok=True)

    print(f"Writing {len(SVGS)} bespoke SVGs to vault assets: {assets_dir}…")
    for fid, svg_content in SVGS.items():
        svg_file = assets_dir / f"{fid}.svg"
        svg_file.write_text(svg_content, encoding="utf-8")

    print("Building visual presentation deck…")
    b = build_deck()
    ex_data, md = generate_dual_format(b)

    md_path = vault / "Agentic_Goal_Extraction_Presentation.excalidraw.md"
    json_path = vault / "Agentic_Goal_Extraction_Presentation.excalidraw"

    md_path.write_text(md, encoding="utf-8")
    json_path.write_text(json.dumps(ex_data, indent=2, ensure_ascii=False), encoding="utf-8")

    frames = [e for e in b.elements if e["type"] == "frame"]
    images = [e for e in b.elements if e["type"] == "image"]
    texts  = [e for e in b.elements if e["type"] == "text"]
    rects  = [e for e in b.elements if e["type"] == "rectangle"]

    print("\n--- GENERATION REPORT ---")
    print(f"Total elements : {len(b.elements)}")
    print(f"Frames         : {len(frames)}  (target 8)")
    print(f"Images (SVGs)  : {len(images)}")
    print(f"Text blocks    : {len(texts)}")
    print(f"Rectangles     : {len(rects)}")
    print(f"Embedded SVGs  : {len(b.files)}  (target 18)")
    print(f"Output MD      : {md_path}  ({md_path.stat().st_size // 1024} KB)")
    print(f"Output JSON    : {json_path}  ({json_path.stat().st_size // 1024} KB)")
    print("Done!")


if __name__ == "__main__":
    main()
