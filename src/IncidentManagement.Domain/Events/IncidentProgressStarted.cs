using IncidentManagement.Domain.Common;
using IncidentManagement.Domain.Entities;

namespace IncidentManagement.Domain.Events;

public sealed record IncidentProgressStarted(Incident Incident, DateTimeOffset StartedAt) : IDomainEvent;