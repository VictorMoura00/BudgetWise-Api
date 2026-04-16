namespace BudgetWise.Application.FamilyGroups.DTOs;

public sealed record FamilyMemberResponse(
    Guid Id,
    Guid UserId,
    string Role,
    DateTime JoinedAt);

public sealed record FamilyGroupResponse(
    Guid Id,
    string Name,
    string? Description,
    string InviteCode,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<FamilyMemberResponse> Members);

public sealed record FamilyGroupSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    int MemberCount,
    DateTime CreatedAt);

public sealed record CreateFamilyGroupRequest(string Name, string? Description);

public sealed record UpdateFamilyGroupRequest(string Name, string? Description);

public sealed record JoinFamilyGroupRequest(string InviteCode);
