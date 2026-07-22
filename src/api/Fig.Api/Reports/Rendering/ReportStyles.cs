namespace Fig.Api.Reports.Rendering;

public static class ReportStyles
{
    public const string CombinedCss = """
        :root {
          --fig-report-text: #1a1a1a;
          --fig-report-muted: #5a5a5a;
          --fig-report-border: #d0d0d0;
          --fig-report-accent: #2b6cb0;
          --fig-report-bg: #ffffff;
          --fig-report-header-bg: #f7f9fc;
          --fig-report-card-bg: #f3f6fa;
        }
        * { box-sizing: border-box; }
        body {
          margin: 0;
          font-family: "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
          color: var(--fig-report-text);
          background: var(--fig-report-bg);
          font-size: 12px;
          line-height: 1.45;
        }
        .report-page {
          max-width: 1100px;
          margin: 0 auto;
          padding: 24px 32px 48px;
        }
        .report-header {
          display: flex;
          gap: 20px;
          align-items: flex-start;
          border-bottom: 2px solid var(--fig-report-accent);
          padding-bottom: 16px;
          margin-bottom: 20px;
        }
        .report-logo { width: 64px; height: 64px; object-fit: contain; }
        .report-title { margin: 0 0 4px; font-size: 22px; font-weight: 650; }
        .report-description { margin: 0; color: var(--fig-report-muted); }
        .report-meta {
          display: grid;
          grid-template-columns: 1fr 1fr;
          gap: 8px 24px;
          margin-bottom: 20px;
          padding: 12px 14px;
          background: var(--fig-report-header-bg);
          border: 1px solid var(--fig-report-border);
          border-radius: 6px;
        }
        .report-meta dt { font-weight: 600; color: var(--fig-report-muted); font-size: 11px; text-transform: uppercase; letter-spacing: 0.03em; }
        .report-meta dd { margin: 0 0 8px; }
        .report-params {
          margin-bottom: 24px;
        }
        .report-params h2, .report-section h2 {
          font-size: 14px;
          margin: 0 0 10px;
          color: var(--fig-report-accent);
          border-bottom: 1px solid var(--fig-report-border);
          padding-bottom: 4px;
        }
        .report-params ul { margin: 0; padding-left: 18px; }
        .report-footer {
          margin-top: 32px;
          padding-top: 12px;
          border-top: 1px solid var(--fig-report-border);
          color: var(--fig-report-muted);
          font-size: 11px;
          text-align: center;
        }
        .summary-cards {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
          gap: 12px;
          margin-bottom: 20px;
        }
        .summary-card {
          background: var(--fig-report-card-bg);
          border: 1px solid var(--fig-report-border);
          border-radius: 6px;
          padding: 12px 14px;
        }
        .summary-card .label { color: var(--fig-report-muted); font-size: 11px; text-transform: uppercase; letter-spacing: 0.03em; }
        .summary-card .value { font-size: 22px; font-weight: 650; margin-top: 4px; }
        .report-table {
          width: 100%;
          border-collapse: collapse;
          margin-bottom: 20px;
        }
        .report-table th, .report-table td {
          border: 1px solid var(--fig-report-border);
          padding: 6px 8px;
          text-align: left;
          vertical-align: top;
        }
        .report-table th {
          background: var(--fig-report-header-bg);
          font-weight: 600;
        }
        .report-table tbody tr:nth-child(even) { background: #fafbfc; }
        .report-table-nested {
          margin: 0;
          font-size: 11px;
        }
        .report-table-nested th, .report-table-nested td {
          padding: 4px 6px;
        }
        .report-markdown {
          line-height: 1.45;
        }
        .report-markdown h1, .report-markdown h2, .report-markdown h3,
        .report-markdown h4, .report-markdown h5, .report-markdown h6 {
          margin: 0.4em 0 0.3em;
          color: var(--fig-report-text);
          border: 0;
          padding: 0;
          font-size: 1em;
        }
        .report-markdown h1 { font-size: 1.25em; }
        .report-markdown h2 { font-size: 1.15em; }
        .report-markdown p { margin: 0 0 0.5em; }
        .report-markdown p:last-child { margin-bottom: 0; }
        .report-markdown ul, .report-markdown ol { margin: 0 0 0.5em; padding-left: 1.2em; }
        .report-markdown img {
          max-width: 100%;
          height: auto;
          display: block;
          margin: 8px 0;
        }
        .report-inline-code, .report-code-block {
          font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
          font-size: 0.92em;
        }
        .report-inline-code {
          background: #f0f2f5;
          padding: 1px 4px;
          border-radius: 3px;
        }
        .report-code-block {
          background: #f0f2f5;
          padding: 8px 10px;
          border-radius: 4px;
          overflow-x: auto;
        }
        .timeline { border-left: 3px solid var(--fig-report-accent); margin: 0 0 20px 8px; padding-left: 16px; }
        .timeline-item { position: relative; margin-bottom: 14px; }
        .timeline-item::before {
          content: "";
          position: absolute;
          left: -21px;
          top: 4px;
          width: 10px;
          height: 10px;
          border-radius: 50%;
          background: var(--fig-report-accent);
        }
        .timeline-time { color: var(--fig-report-muted); font-size: 11px; }
        .timeline-title { font-weight: 600; }
        .rich-text { margin-bottom: 16px; }
        .rich-text p { margin: 0 0 8px; }
        .stat-inline { display: inline-block; margin-right: 18px; }
        .stat-inline .value { font-weight: 650; }
        .chart-wrap { margin: 16px 0 24px; max-width: 640px; }
        .availability-bar {
          display: flex;
          height: 28px;
          border: 1px solid var(--fig-report-border);
          border-radius: 4px;
          overflow: hidden;
          margin-bottom: 8px;
        }
        .availability-bar .up { background: #38a169; }
        .availability-bar .down { background: #e53e3e; }
        .muted { color: var(--fig-report-muted); }
        .page-break { page-break-before: always; break-before: page; }
        .no-print { }
        .toolbar {
          position: sticky;
          top: 0;
          z-index: 10;
          display: flex;
          gap: 8px;
          justify-content: flex-end;
          padding: 10px 16px;
          background: #1f2937;
          color: white;
        }
        .toolbar button {
          background: #3182ce;
          color: white;
          border: 0;
          border-radius: 4px;
          padding: 8px 14px;
          cursor: pointer;
          font-size: 13px;
        }
        .toolbar button:hover { background: #2b6cb0; }
        @media print {
          .toolbar, .no-print { display: none !important; }
          .report-footer { display: none !important; }
          body { background: white; }
          .report-page { max-width: none; padding: 0; }
          .report-table thead { display: table-header-group; }
          .report-table tr { page-break-inside: avoid; }
          .summary-card, .timeline-item { page-break-inside: avoid; }
        }
        @page {
          size: A4 portrait;
          margin: 12mm 12mm 16mm 12mm;
        }
        body.landscape @page, @page landscape-page {
          size: A4 landscape;
        }
        body.landscape {
          /* orientation hint for browsers that honor it via CSS */
        }
        @media print {
          body.landscape {
            /* some browsers use this with print dialog landscape selection */
          }
        }
        """;

    public static string BuildPrintFooterCss(DateTime generatedAtUtc)
    {
        var timestamp = generatedAtUtc.ToString("u");
        return $$"""
            @page {
              @bottom-left {
                content: "Generated by Fig · {{timestamp}} · Confidential";
                font-size: 9pt;
                color: #5a5a5a;
                font-family: "Segoe UI", system-ui, sans-serif;
              }
              @bottom-right {
                content: "Page " counter(page) " of " counter(pages);
                font-size: 9pt;
                color: #5a5a5a;
                font-family: "Segoe UI", system-ui, sans-serif;
              }
            }
            """;
    }
}
