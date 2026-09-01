# Project CSS to Tailwind Conversion

## Context
Goal: Improve maintainability/develop speed using Tailwind CSS. Current CSS is monolithic in `app.css`.

## Approach
Convert step by step to avoid breakage.
1. Target `AdminBayiler.svelte` as pilot.
2. In `AdminBayiler.svelte`: Replace `class="tablo-cerceve"` with Tailwind classes equivalent to `app.css` `.tablo-cerceve` styles.
3. Replace/Update pagination `button` styles correspondingly.
4. Verify functionality and layout.
5. Apply pattern to subsequent components.

## Critical Files
- `frontend/src/app.css` (Target for removal of converted CSS)
- `frontend/src/lib/Components/AdminBayiler.svelte` (Pilot)

## Verification
- Inspect component in browser while running.
- Ensure layout matches original styles based on `app.css` rules.
