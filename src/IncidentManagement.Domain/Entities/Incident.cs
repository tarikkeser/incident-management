using IncidentManagement.Domain.Enums;

namespace IncidentManagement.Domain.Entities;

public class Incident
{
    private const int MaxTitleLength = 100;
    private const int MaxDescriptionLength = 2000;

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public IncidentStatus Status { get; private set; }

    public Severity Severity { get; private set; }

    public string? AffectedService { get; private set; }

    public string ReportedBySlackUserId { get; private set; }

    public string SlackChannelId { get; private set; }

    public string SlackMessageId { get; private set; }

    public string SlackThreadTs { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public int? GitHubIssueNumber { get; private set; }

    public string? GitHubIssueUrl { get; private set; }

    // Private constructor for EF Core
    private Incident()
    {
    }

    public static Incident Create(string title, string? description, Severity severity, string? affectedService,
        string reportedBySlackUserId,
        string slackChannelId, string slackMessageId, string slackThreadTs, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportedBySlackUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(slackChannelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(slackMessageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(slackThreadTs);

        var trimmedTitle = title.Trim();
        var trimmedDescription = description?.Trim();


        if (trimmedTitle.Length > MaxTitleLength)
        {
            throw new ArgumentException($"Title cannot exceed {MaxTitleLength} characters.", nameof(title));
        }

        if (trimmedDescription?.Length > MaxDescriptionLength)
        {
            throw new ArgumentException($"Description cannot exceed {MaxDescriptionLength} characters.",
                nameof(description));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("CreatedAt must be set", nameof(createdAt));
        }

        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentException("Invalid severity value", nameof(severity));
        }

        return new Incident
        {
            Id = Guid.CreateVersion7(),
            Title = trimmedTitle,
            Description = description?.Trim(),
            Status = IncidentStatus.Open,
            Severity = severity,
            AffectedService = affectedService?.Trim(),
            ReportedBySlackUserId = reportedBySlackUserId,
            SlackChannelId = slackChannelId,
            SlackMessageId = slackMessageId,
            SlackThreadTs = slackThreadTs,
            CreatedAt = createdAt
        };
    }

    public bool StartProgress()
    {
        if (this.Status != IncidentStatus.Open)
        {
            return false;
        }

        Status = IncidentStatus.InProgress;
        return true;
    }

    public bool Resolve(DateTimeOffset resolvedAt)
    {
        if (this.Status == IncidentStatus.Resolved)
        {
            return false;
        }

        if (resolvedAt < CreatedAt)
        {
            throw new ArgumentException("ResolvedAt cannot be earlier than CreatedAt", nameof(resolvedAt));
        }

        this.Status = IncidentStatus.Resolved;
        this.ResolvedAt = resolvedAt;
        return true;
    }

    public void LinkGitHubIssue(int issueNumber, string issueUrl)
    {
        if (issueNumber <= 0)
        {
            throw new ArgumentException("GitHub issue number must be a positive integer.", nameof(issueNumber));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(issueUrl);

        if (GitHubIssueNumber != null)
        {
            if (GitHubIssueNumber == issueNumber)
            {
                return; // Already linked to the same issue, do nothing
            }

            throw new InvalidOperationException($"Incident is already linked to a GitHub issue: #{GitHubIssueNumber}.");
        }

        this.GitHubIssueNumber = issueNumber;
        this.GitHubIssueUrl = issueUrl.Trim();
    }
}