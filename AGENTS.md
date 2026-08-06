# Repository guidance

- Keep source code, tests, documentation, configuration, and commit messages in English.
- Return DTOs from APIs and keep Entity Framework entities inside persistence boundaries.
- Preserve assignment invariants, unique asset identifiers, validation, and Problem Details errors.
- Add unit tests for business rules and integration tests for changed HTTP behavior.
- Keep migrations forward-compatible and update seed data deterministically.
- Run formatting verification, build, unit tests, integration tests, and Docker checks before push.
- Never commit real employee data, serial numbers, credentials, tokens, or production databases.
