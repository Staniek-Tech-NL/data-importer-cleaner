# Performance evidence

## Verified release-candidate measurement

The M8 performance smoke test generates and processes 50,000 fully synthetic rows. It imports CSV, profiles every column, runs deterministic cleaning and post-cleaning validation, detects composite-key duplicates and exports the working dataset to CSV.

Verified locally on 2026-08-14 using Windows 10.0.26200, .NET SDK 10.0.400 and an AMD64 Family 23 Model 113 processor:

| Stage | Time |
| --- | ---: |
| CSV import and type inference | 833 ms |
| Column profiling | 825 ms |
| Cleaning | 163 ms |
| Complete pipeline including validation, deduplication and CSV export | 2,853 ms |

The automated budget is 30 seconds for 50,000 rows. This is a regression guard, not a cross-device speed guarantee. Disk, antivirus, processor, culture configuration, rule count and value complexity can change results. The WPF view model dispatches import, profiling, validation, cleaning, deduplication and export work away from the UI thread so the shell can continue repainting during these operations.

Run the measurement with:

```powershell
dotnet test tests/DataCleaner.Infrastructure.Tests/DataCleaner.Infrastructure.Tests.csproj `
  --configuration Release `
  --filter "Category=Performance" `
  --logger "console;verbosity=detailed"
```
