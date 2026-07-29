# CSV Import and Export

## Import

Use `POST /api/assets/import-csv` as multipart form data with a field named `file`.

Required columns:

- `AssetTag`
- `Type`
- `Manufacturer`
- `Model`
- `SerialNumber`
- `Status`

Optional columns:

- `Hostname`
- `PurchaseDate`
- `WarrantyUntil`

The parser supports quoted commas and escaped double quotes. Dates must use `yyyy-MM-dd`.
Supported types are `Computer`, `Laptop`, `Monitor`, and `Peripheral`. Supported statuses are
`In Stock`, `Repair`, and `Retired`; assignment must use the assignment API because it requires a
valid user.

The endpoint limits each file to 2 MiB and 500 non-empty rows. Existing or repeated asset tags are
skipped and returned as warnings.

## Export

`GET /api/assets/export-csv` exports the inventory with the assigned user's display name. The
response uses UTF-8 with a byte-order mark for compatibility with common Windows spreadsheet
tools.

The export is an operational interchange file, not a database backup. Back up `inventory.db` when
the full relational history must be preserved.
