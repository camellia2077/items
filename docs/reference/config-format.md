# Config Format (JSON5)

`RandomLoadout` configuration now uses `json5` files:

* `RandomLoadout.rules.json5`
* `RandomLoadout.aliases.json5`
* `RandomLoadout.rules.full-pool.json5`

## Supported JSON5 Features

The loader supports these JSON5 conveniences:

* Line comments: `// ...`
* Block comments: `/* ... */`
* Trailing commas in arrays and objects
* Single-quoted strings: `'value'`

## Recommended Style

To keep files readable and stable, use this style:

* Keep property names in double quotes
* Keep string values in double quotes
* Use comments for intent and grouping
* Keep IDs as integers (no quotes)
* Keep one rule object per block, and one alias entry per line

## Example: Rules

```json5
{
  // Startup: Casey + 2 random passives from the pool.
  "rules": [
    {
      "enabled": true,
      "mode": "specific",
      "category": "gun",
      "id": 541,
    },
    {
      "enabled": true,
      "mode": "random",
      "category": "passive",
      "count": 2,
      "poolIds": [427, 114, 118],
    },
  ],
}
```

## Example: Aliases

```json5
{
  "aliases": [
    { "alias": "casey_bat", "id": 541 },
    { "alias": "eyepatch", "id": 118 },
  ],
}
```

## Notes

* Missing or invalid `rules.json5` falls back to `rules.full-pool.json5`, then to built-in defaults.
* Missing or invalid `aliases.json5` falls back to built-in default aliases.
