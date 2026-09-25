using Application.AuditLogs;
using Application.AuditLogs.Dtos.Response;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.AuditLogs.Queries.GetById;

public class GetAuditLogByIdQueryHandler
    : IRequestHandler<GetAuditLogByIdQuery, BaseResponse<AuditLogResponse>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetAuditLogByIdQueryHandler(
        IAuditLogRepository auditLogRepository,
        ICurrentUserService currentUserService)
    {
        _auditLogRepository = auditLogRepository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse<AuditLogResponse>> Handle(
        GetAuditLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        var x = await _auditLogRepository.GetByIdAsync(request.Id, cancellationToken);
        if (x == null || (!_currentUserService.IsSuperAdmin && x.CompanyId != _currentUserService.CompanyId))
            return BaseResponse<AuditLogResponse>.Fail("Audit log not found.");

        return BaseResponse<AuditLogResponse>.Ok(AuditLogUserMapper.ToResponse(x));
    }
}
