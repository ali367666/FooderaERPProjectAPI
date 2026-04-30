using Application.AuditLogs.Dtos.Response;
using Application.Common.Responce;
using MediatR;

namespace Application.AuditLogs.Queries.GetAll;

public record GetAuditLogsQuery(
    string? EntityName,
    string? EntityId,
    string? ActionType,
    int? UserId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? Search
) : IRequest<BaseResponse<List<AuditLogResponse>>>;