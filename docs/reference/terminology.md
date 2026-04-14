# Terminology And Naming

This project touches both ETG runtime internals and player-facing UI, so naming needs to separate gameplay meaning from scene-token implementation details.

## Preferred Terms

- `character select hub`
  The preferred gameplay term for the out-of-run player hub where character selection happens.
  Use this when describing behavior, transitions, and feature requirements.

- `tt_foyer`
  The primary ETG scene token for the character-select hub.
  Use this only for scene-name constants, scene comparisons, and logs that need the exact token.

- `tt_breach`
  A legacy scene token still recognized by the project for compatibility.
  Treat it as a fallback scene token, not the preferred gameplay term.

- `Foyer`
  The in-game runtime type and API surface used by ETG for hub-specific behavior.
  Use this when referring to ETG classes such as `Foyer.Instance` or methods like `OnDepartedFoyer()`.

- `Breach`
  Allowed in player-facing UI and historical docs because ETG players commonly understand it.
  Avoid introducing new internal code identifiers that use `Breach` when the real meaning is "character select hub" or when the real implementation is a specific scene token.

## Naming Rules

- Use gameplay semantics for methods and state names.
  Example: `ReturnToCharacterSelect()`

- Use exact scene tokens for scene constants.
  Example: `CharacterSelectSceneName = "tt_foyer"`

- Do not use a scene token as if it were a gameplay state.
  Example: avoid treating `tt_foyer` as interchangeable with "safe to return to hub" or "already in character select flow".

- When a name refers to ETG's runtime type, keep the ETG type name.
  Example: `Foyer.Instance.OnDepartedFoyer()`

- Prefer logs that include both semantic meaning and exact token when useful.
  Example: `Returning to character select hub.` and `Observed scene tt_foyer.`

## Boss Rush Guidance

- Start conditions should be described as `character select hub only` in code semantics, even if current UI still says `Breach`.
- Returning from Boss Rush should prefer the ETG character-select flow, not a hard `LoadCustomLevel("tt_foyer")`.
- Boss-room logic should use gameplay terms like `boss room`, `boss staging room`, and `encounter`, while scene comparisons should still use exact ETG tokens.
