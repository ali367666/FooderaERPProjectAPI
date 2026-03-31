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
            cancellationToken);

        var response = logs.Select(x => new AuditLogResponse
        {
            Id = x.Id,
            EntityName = x.EntityName,
            EntityId = x.EntityId,
            ActionType = x.ActionType,
            OldValues = x.OldValues,
            NewValues = x.NewValues,
            Message = x.Message,
            UserId = x.UserId,
            CompanyId = x.CompanyId,
            IsSuccess = x.IsSuccess,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();

        return BaseResponse<List<AuditLogResponse>>.Ok(response);
    }
}