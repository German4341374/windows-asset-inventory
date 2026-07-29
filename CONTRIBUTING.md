# Contributing

1. Create a focused branch.
2. Keep API changes backward-compatible or document the breaking change.
3. Add tests for business rules and HTTP behavior.
4. Run format, build, tests, and the NuGet vulnerability audit.
5. Open a pull request using the repository template.

Use Conventional Commits:

```text
feat: add assignment history endpoint
fix: prevent retired assets from being imported as assigned
test: cover CSV quoted fields
docs: explain SQLite backup procedure
```

Never commit real employee records, serial numbers, credentials, SQLite database files, or
exported company inventory.
