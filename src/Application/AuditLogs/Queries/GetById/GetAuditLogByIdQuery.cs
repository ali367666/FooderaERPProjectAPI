using Application.AuditLogs.Dtos.Response;
using Application.Common.Responce;
using MediatR;

namespace Application.AuditLogs.Queries.GetById;

public record GetAuditLogByIdQuery(long Id) : IRequest<BaseResponse<AuditLogResponse>>;
