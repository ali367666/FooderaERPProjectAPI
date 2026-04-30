using Application.AuditLogs;
using Application.AuditLogs.Dtos.Response;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.AuditLogs.Queries.GetAll;

public class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, BaseResponse<List<AuditLogResponse>>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<BaseResponse<List<AuditLogResponse>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var logs = await _auditLogRepository.GetAllAsync(
            request.EntityName,
            request.EntityId,
            request.ActionType,
            request.UserId,
            request.FromUtc,
            request.ToUtc,
            request.Search,
            cancellationToken);

        var response = logs.Select(AuditLogUserMapper.ToResponse).ToList();

        return BaseResponse<List<AuditLogResponse>>.Ok(response);
    }
}