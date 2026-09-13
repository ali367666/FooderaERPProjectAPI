using Application.AuditLogs;
using Application.AuditLogs.Dtos.Response;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.AuditLogs.Queries.GetAll;

public class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, BaseResponse<List<AuditLogResponse>>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAuditLogsQueryHandler(
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService)
    {
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
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

        // A tenant Admin only ever sees their own company's audit trail — platform-level entries
        // (CompanyId null) and other tenants' entries must never leak. SuperAdmin can target one
        // company via CompanyId, or omit it to see everything.
        if (!_currentUserService.IsSuperAdmin)
        {
            logs = logs.Where(x => x.CompanyId == _currentUserService.CompanyId).ToList();
        }
        else if (request.CompanyId is > 0)
        {
            logs = logs.Where(x => x.CompanyId == request.CompanyId.Value).ToList();
        }

        var response = logs.Select(AuditLogUserMapper.ToResponse).ToList();

        return BaseResponse<List<AuditLogResponse>>.Ok(response);
    }
}