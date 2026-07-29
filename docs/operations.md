# Operations Runbook

## Health check fails

1. Request `GET /health`.
2. Check that the process can read and write the configured SQLite directory.
3. Confirm the `ConnectionStrings__Inventory` environment variable.
4. Review startup logs for migration failures.
5. In Docker, inspect `docker compose logs inventory`.

## Database is locked

SQLite serializes writes. Confirm that only one application container is using the database
volume and that no desktop database editor has an open write transaction. Restarting should be a
last resort after confirming no write is in progress.

## Migration fails

1. Stop the application.
2. Copy `inventory.db` to a safe local backup.
3. Run `dotnet tool run dotnet-ef migrations script` and inspect the pending SQL.
4. Correct the migration or restore the backup.
5. Start one application instance and verify `/health` before restoring access.

## Reset the demonstration

Local:

```bash
rm src/WindowsAssetInventory/App_Data/inventory.db
dotnet run --project src/WindowsAssetInventory
```

Docker:

```bash
docker compose down --volumes
docker compose up --build --detach
```

These commands permanently remove local demonstration data.
