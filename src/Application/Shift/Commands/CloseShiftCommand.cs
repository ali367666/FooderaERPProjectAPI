using Application.Shift.Dtos;
using MediatR;

namespace Application.Shift.Commands;

public record CloseShiftCommand(int ShiftId, CloseShiftRequest Request) : IRequest<ZReportResponse>;
