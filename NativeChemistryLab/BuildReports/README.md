# Structured build reports

Unity editor automation writes durable JSON summaries here:

- `desktop-validation-report.json` — chemistry, safety and audio-signal matrix
- `desktop-build-report.json` — Windows player result, warning/error counts,
  output size and the same validation matrix
- `desktop-smoke-report.json` — runtime database, UI, camera and audio assertions

Raw Unity `*.log` files are intentionally ignored. Commit JSON reports when they
represent a meaningful validation or release checkpoint.
