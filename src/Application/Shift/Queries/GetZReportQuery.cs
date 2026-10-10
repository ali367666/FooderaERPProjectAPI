using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Queries;

public record GetZReportQuery(int ShiftId) : IRequest<ZReportResponse>;
