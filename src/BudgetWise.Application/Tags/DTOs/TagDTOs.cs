namespace BudgetWise.Application.Tags.DTOs;

public sealed record TagResponse(Guid Id, string Name, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record TagSummary(Guid Id, string Name);

public sealed record CreateTagRequest(string Name);

public sealed record UpdateTagRequest(string Name);
