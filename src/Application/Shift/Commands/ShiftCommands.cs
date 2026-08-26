using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Commands;

public record OpenShiftCommand(OpenShiftRequest Request) : IRequest<ShiftResponse>;

public record CloseShiftCommand(int ShiftId, CloseShiftRequest Request) : IRequest<ZReportResponse>;
