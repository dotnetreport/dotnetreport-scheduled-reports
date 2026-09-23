# Scheduled PDF & Excel report emails in ASP.NET Core

Let end users schedule any report they build to be emailed to them — as a **PDF**, **Excel** workbook, or a **link** — on a cron schedule in their own time zone, with row-level security still enforced while nobody is logged in.

Built on [Dotnet Report](https://dotnetreport.com), which ships a background scheduler (Quartz.NET) alongside its self-service report builder. This repo shows only what you configure; the scheduler itself is installed by the NuGet package. Start from the [quickstart repo](https://github.com/dotnetreport/dotnetreport-aspnetcore-quickstart) if you haven't set up Dotnet Report yet.

> The Dotnet Report report-builder front-end is source-available on GitHub: https://github.com/dotnetreport/dotnetreport

## How it works

1. A user opens a report in the builder and adds a **schedule**: a cron expression, their time zone, recipients, and a format (PDF / Excel / Link), plus optional start/end dates and page size/orientation for PDF.
2. A Quartz.NET job (`DotNetReportJob`) starts with your app and **polls every 60 seconds**. For each saved schedule it evaluates the cron expression against the schedule's last run, in the schedule's time zone, to decide whether it is due.
3. When due, it renders the report **with the schedule's saved user, tenant and `DataFilters`** — so row-level security applies exactly as it would for that user in the browser — and produces the file:
   - **PDF** — rendered from the report's print view via a headless browser (PuppeteerSharp)
   - **Excel** — generated with EPPlus (`EXCEL`, or `EXCEL-SUB` to include subtotals), including a chart image when the report has one
   - **Link** — an email containing a link to the live report
4. It emails the result using the SMTP settings in your configuration and records the run so the next occurrence is computed correctly.

## 1. Start the scheduler

Add one line after `builder.Build()` in `Program.cs` (see [`Program.cs`](Program.cs)):

```csharp
var app = builder.Build();

JobScheduler.Start();                                   // starts the Quartz polling job
JobScheduler.WebAppRootUrl = "https://reports.yourapp.com"; // public URL of THIS app
```

`WebAppRootUrl` matters: PDF rendering loads the report's print page (`/DotnetReport/ReportPrint`) through a headless browser, so the job needs a URL it can reach.

## 2. Configure email

Add an `email` section to `appsettings.json` (see [`appsettings.sample.json`](appsettings.sample.json)):

```json
"email": {
  "fromemail": "reports@yourapp.com",
  "fromname": "Your App Reports",
  "server": "smtp.yourprovider.com",
  "username": "smtp-username",
  "password": "smtp-password"
}
```

Use user secrets or environment variables for the password in real deployments — never commit it.

## 3. Let users schedule

That's it on the developer side. In the report builder, users pick a report and set a schedule; every field they set maps to the job's `ReportSchedule` model:

| Field | Meaning |
|---|---|
| `Schedule` | A cron expression, e.g. `0 0 8 ? * MON-FRI` (weekdays at 08:00) |
| `TimeZone` | The IANA/Windows time zone the cron is evaluated in |
| `EmailTo` | Recipient list |
| `Format` | `PDF`, `Excel`, `Excel-Sub` (with subtotals) or `Link` |
| `SelectedPageSize` / `SelectedPageOrientation` | PDF layout |
| `ScheduleStart` / `ScheduleEnd` | Optional active window |
| `DataFilters` | Saved row-level-security filters applied at run time |

## Hosting notes

- The job runs **inside your web app process**. On IIS, disable idle timeout or use Application Initialization / "Always On" (Azure App Service) so the process stays alive to fire schedules.
- Run **one** instance of the scheduler. If you scale out, host the scheduler on a single instance to avoid duplicate sends.
- PDF rendering needs a headless Chromium (PuppeteerSharp downloads one on first use); make sure the host can run it.

## Related

- Quickstart: https://github.com/dotnetreport/dotnetreport-aspnetcore-quickstart
- Multi-tenant + row-level security: https://github.com/dotnetreport/dotnetreport-multitenant-rls
- Docs: https://dotnetreport.com/docs

---

Maintained by the Dotnet Report team. Issues and PRs welcome.
