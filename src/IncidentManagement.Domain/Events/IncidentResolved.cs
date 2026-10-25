using IncidentManagement.Domain.Common;
using IncidentManagement.Domain.Entities;

namespace IncidentManagement.Domain.Events;

public sealed record IncidentResolved(Incident Incident, DateTimeOffset ResolvedAt) : IDomainEvent;
