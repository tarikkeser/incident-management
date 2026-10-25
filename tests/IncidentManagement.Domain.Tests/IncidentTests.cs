using IncidentManagement.Domain.Entities;
using IncidentManagement.Domain.Enums;

namespace IncidentManagement.Domain.Tests;

public class IncidentTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);

    private static Incident NewIncident(
        string title = "Payment service down",
        string? description = "Returns 500",
        Severity severity = Severity.High,
        string? affectedService = "payments",
        string reportedBy = "U024BE7LH",
        string channel = "C012AB3CD",
        string messageId = "Ev0123",
        string threadTs = "1729872000.000200",
        DateTimeOffset? createdAt = null)
        => Incident.Create(title, description, severity, affectedService,
            reportedBy, channel, messageId, threadTs, createdAt ?? T0);

    // ---------- Create ----------

    [Fact]
    public void Create_WithValidData_StartsOpenAndEmptyOptionalFields()
    {
        var incident = NewIncident();

        Assert.NotEqual(Guid.Empty, incident.Id);
        Assert.Equal(IncidentStatus.Open, incident.Status);
        Assert.Equal(T0, incident.CreatedAt);
        Assert.Null(incident.ResolvedAt);
        Assert.Null(incident.GitHubIssueNumber);
        Assert.Null(incident.GitHubIssueUrl);
    }

    [Fact]
    public void Create_TrimsTextFields()
    {
        var incident = NewIncident(title: "  Outage  ", description: "  details ", affectedService: " payments ");

        Assert.Equal("Outage", incident.Title);
        Assert.Equal("details", incident.Description);
        Assert.Equal("payments", incident.AffectedService);
    }

    [Fact]
    public void Create_WithNullOptionalFields_Works()
    {
        var incident = NewIncident(description: null, affectedService: null);

        Assert.Null(incident.Description);
        Assert.Null(incident.AffectedService);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTitle_Throws(string title)
    {
        Assert.ThrowsAny<ArgumentException>(() => NewIncident(title: title));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankSlackFields_Throws(string blank)
    {
        Assert.ThrowsAny<ArgumentException>(() => NewIncident(reportedBy: blank));
        Assert.ThrowsAny<ArgumentException>(() => NewIncident(channel: blank));
        Assert.ThrowsAny<ArgumentException>(() => NewIncident(messageId: blank));
        Assert.ThrowsAny<ArgumentException>(() => NewIncident(threadTs: blank));
    }

    [Fact]
    public void Create_TitleLongerThanMax_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewIncident(title: new string('a', 101)));
    }

    [Fact]
    public void Create_TitleAtMaxLength_Works()
    {
        var incident = NewIncident(title: new string('a', 100));

        Assert.Equal(100, incident.Title.Length);
    }

    [Fact]
    public void Create_LengthIsCheckedAfterTrim()
    {
        var incident = NewIncident(title: " " + new string('a', 100) + " ");

        Assert.Equal(100, incident.Title.Length);
    }

    [Fact]
    public void Create_DescriptionLongerThanMax_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewIncident(description: new string('a', 4001)));
    }

    [Fact]
    public void Create_WithUndefinedSeverity_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => NewIncident(severity: (Severity)99));
    }

    [Fact]
    public void Create_WithDefaultCreatedAt_Throws()
    {
        Assert.Throws<ArgumentException>(() => NewIncident(createdAt: default(DateTimeOffset)));
    }

    // ---------- StartProgress ----------

    [Fact]
    public void StartProgress_WhenOpen_MovesToInProgress()
    {
        var incident = NewIncident();

        var changed = incident.StartProgress();

        Assert.True(changed);
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
    }

    [Fact]
    public void StartProgress_WhenAlreadyInProgress_ReturnsFalse()
    {
        var incident = NewIncident();
        incident.StartProgress();

        Assert.False(incident.StartProgress());
        Assert.Equal(IncidentStatus.InProgress, incident.Status);
    }

    [Fact]
    public void StartProgress_WhenResolved_ReturnsFalseAndStaysResolved()
    {
        var incident = NewIncident();
        incident.Resolve(T0.AddHours(1));

        Assert.False(incident.StartProgress());
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
    }

    // ---------- Resolve ----------

    [Fact]
    public void Resolve_FromOpen_SetsResolvedAtAndStatus()
    {
        var incident = NewIncident();

        var changed = incident.Resolve(T0.AddMinutes(90));

        Assert.True(changed);
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal(T0.AddMinutes(90), incident.ResolvedAt);
    }

    [Fact]
    public void Resolve_FromInProgress_Works()
    {
        var incident = NewIncident();
        incident.StartProgress();

        Assert.True(incident.Resolve(T0.AddMinutes(30)));
        Assert.Equal(IncidentStatus.Resolved, incident.Status);
    }

    [Fact]
    public void Resolve_ResolutionTimeIsDifferenceBetweenCreatedAndResolved()
    {
        var incident = NewIncident();
        incident.Resolve(T0.AddMinutes(90));

        Assert.Equal(TimeSpan.FromMinutes(90), incident.ResolvedAt - incident.CreatedAt);
    }

    [Fact]
    public void Resolve_CalledTwice_SecondCallReturnsFalseAndKeepsFirstResolvedAt()
    {
        var incident = NewIncident();
        incident.Resolve(T0.AddMinutes(30));

        var changed = incident.Resolve(T0.AddMinutes(99));

        Assert.False(changed);
        Assert.Equal(T0.AddMinutes(30), incident.ResolvedAt);
    }

    [Fact]
    public void Resolve_BeforeCreatedAt_ThrowsAndLeavesIncidentUnchanged()
    {
        var incident = NewIncident();

        Assert.Throws<ArgumentException>(() => incident.Resolve(T0.AddMinutes(-1)));
        Assert.Equal(IncidentStatus.Open, incident.Status);
        Assert.Null(incident.ResolvedAt);
    }

    // ---------- LinkGitHubIssue ----------

    [Fact]
    public void LinkGitHubIssue_SetsNumberAndTrimmedUrl()
    {
        var incident = NewIncident();

        incident.LinkGitHubIssue(42, " https://github.com/o/r/issues/42 ");

        Assert.Equal(42, incident.GitHubIssueNumber);
        Assert.Equal("https://github.com/o/r/issues/42", incident.GitHubIssueUrl);
    }

    [Fact]
    public void LinkGitHubIssue_SameNumberTwice_IsIdempotent()
    {
        var incident = NewIncident();
        incident.LinkGitHubIssue(42, "https://github.com/o/r/issues/42");

        var ex = Record.Exception(() => incident.LinkGitHubIssue(42, "https://github.com/o/r/issues/42"));

        Assert.Null(ex);
        Assert.Equal(42, incident.GitHubIssueNumber);
    }

    [Fact]
    public void LinkGitHubIssue_DifferentNumber_ThrowsAndKeepsOriginal()
    {
        var incident = NewIncident();
        incident.LinkGitHubIssue(42, "https://github.com/o/r/issues/42");

        Assert.Throws<InvalidOperationException>(
            () => incident.LinkGitHubIssue(43, "https://github.com/o/r/issues/43"));
        Assert.Equal(42, incident.GitHubIssueNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void LinkGitHubIssue_NonPositiveNumber_Throws(int number)
    {
        var incident = NewIncident();

        Assert.Throws<ArgumentException>(() => incident.LinkGitHubIssue(number, "https://github.com/o/r/issues/1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void LinkGitHubIssue_BlankUrl_Throws(string url)
    {
        var incident = NewIncident();

        Assert.ThrowsAny<ArgumentException>(() => incident.LinkGitHubIssue(1, url));
    }
}