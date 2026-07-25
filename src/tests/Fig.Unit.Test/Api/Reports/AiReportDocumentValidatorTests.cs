using Fig.Api.Reports;
using Fig.Api.Reports.Rendering.Components;
using NUnit.Framework;

namespace Fig.Unit.Test.Api.Reports;

[TestFixture]
public class AiReportDocumentValidatorTests
{
    [Test]
    public void ParseAndValidate_AcceptsFullDocument()
    {
        const string json = """
            {
              "title": "Change hotspots",
              "description": "Last 7 days",
              "sections": [
                { "type": "summary", "items": [ { "label": "Events", "value": "10", "subText": "utc" } ] },
                { "type": "markdown", "content": "Short narrative." },
                { "type": "table", "title": "Clients", "columns": ["Client", "Count"], "rows": [["A", "3"], ["B", "7"]] },
                { "type": "chart", "title": "By type", "chartType": "pie", "datasetLabel": "Count",
                  "data": [ { "label": "Change", "value": 7 }, { "label": "Login", "value": 3 } ] },
                { "type": "timeline", "title": "Notable",
                  "items": [ { "timestampUtc": "2026-07-24T10:00:00Z", "title": "Spike", "detail": "client A" } ] }
              ]
            }
            """;

        var document = AiReportDocumentValidator.ParseAndValidate(json);

        Assert.That(document.Title, Is.EqualTo("Change hotspots"));
        Assert.That(document.Description, Is.EqualTo("Last 7 days"));
        Assert.That(document.Sections, Has.Count.EqualTo(5));
        Assert.That(document.Sections[0], Is.TypeOf<AiReportSummarySection>());
        Assert.That(((AiReportSummarySection)document.Sections[0]).Items[0].Value, Is.EqualTo("10"));
        Assert.That(document.Sections[2], Is.TypeOf<AiReportTableSection>());
        var table = (AiReportTableSection)document.Sections[2];
        Assert.That(table.Rows, Has.Count.EqualTo(2));
        Assert.That(table.Rows[0]["Client"], Is.EqualTo("A"));
        Assert.That(((AiReportChartSection)document.Sections[3]).Data, Has.Count.EqualTo(2));
        Assert.That(((AiReportTimelineSection)document.Sections[4]).Items[0].Title, Is.EqualTo("Spike"));
    }

    [Test]
    public void ParseAndValidate_RejectsMissingTitle()
    {
        var ex = Assert.Throws<AiReportValidationException>(() =>
            AiReportDocumentValidator.ParseAndValidate("""{"sections":[{"type":"markdown","content":"x"}]}"""));
        Assert.That(ex!.Message, Does.Contain("non-empty title"));
    }

    [Test]
    public void ParseAndValidate_RejectsChartWithoutData_IncludesShapeHint()
    {
        var ex = Assert.Throws<AiReportValidationException>(() =>
            AiReportDocumentValidator.ParseAndValidate(
                """{"title":"T","sections":[{"type":"chart","chartType":"pie"}]}"""));
        Assert.That(ex!.Message, Does.Contain("data"));
        Assert.That(ex.Message, Does.Contain("label"));
    }

    [Test]
    public void ParseAndValidate_RejectsUnknownSectionType()
    {
        Assert.Throws<AiReportValidationException>(() =>
            AiReportDocumentValidator.ParseAndValidate(
                """{"title":"T","sections":[{"type":"html","content":"<b>x</b>"}]}"""));
    }

    [Test]
    public void ParseAndValidate_RejectsTooManyTableRows()
    {
        var rows = string.Join(",", Enumerable.Range(0, AiReportDocumentValidator.MaxTableRows + 1)
            .Select(i => $"[\"r{i}\"]"));
        var json = $$"""{"title":"T","sections":[{"type":"table","columns":["C"],"rows":[{{rows}}]}]}""";

        Assert.Throws<AiReportValidationException>(() => AiReportDocumentValidator.ParseAndValidate(json));
    }

    [Test]
    public void ParseAndValidate_AcceptsObjectRows()
    {
        var document = AiReportDocumentValidator.ParseAndValidate(
            """
            {
              "title": "T",
              "sections": [
                {
                  "type": "table",
                  "columns": ["Client", "Count"],
                  "rows": [ { "Client": "X", "Count": 5 } ]
                }
              ]
            }
            """);

        var table = (AiReportTableSection)document.Sections[0];
        Assert.That(table.Rows[0]["Count"], Is.EqualTo(5L));
    }
}
