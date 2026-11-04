using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Domain.Enums;
using MediatR;

namespace Application.StoreMode.Cash;

public class ReopenStoreShiftRequest
{
    /// <summary>Why a closed shift is opened again; it is kept in the shift note and in the audit log.</summary>
    public string Reason { get; set; } = default!;
}

/// <summary>Opens a closed shift again (needs its own permission and a reason). The Z figures are cleared and are worked out again at the next closing.</summary>
public record ReopenStoreShiftCommand(int ShiftId, ReopenStoreShiftRequest Request) : IRequest<StoreShiftResponse>;

public class ReopenStoreShiftHandler : IRequestHandler<ReopenStoreShiftCommand, StoreShiftResponse>
{
    private readonly IStoreRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public ReopenStoreShiftHandler(IStoreRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<StoreShiftResponse> Handle(ReopenStoreShiftCommand command, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId;
        var reason = command.Request.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            throw new BadRequestException("Növbəni yenidən açmaq üçün səbəb yazın.");

        var shift = await _repository.GetShiftAsync(command.ShiftId, companyId, ct)
            ?? throw new NotFoundException("Növbə tapılmadı.");
        if (shift.Status != StoreShiftStatus.Closed)
            throw new BadRequestException("Növbə bağlı deyil.");
        if (await _repository.GetOpenShiftForRegisterAsync(shift.StoreCashRegisterId, ct) is not null)
            throw new BadRequestException("Bu kassada başqa açıq növbə var.");
        if (await _repository.GetOpenShiftForUserAsync(companyId, shift.CashierUserId, ct) is not null)
            throw new BadRequestException("Kassirin başqa açıq növbəsi var.");

        shift.Status = StoreShiftStatus.Open;
        shift.CloseNote = $"[Yenidən açıldı: {reason}] Əvvəlki Z: gözlənilən {shift.ExpectedCash:0.00}, faktiki {shift.ActualCash:0.00}. {shift.CloseNote}".Trim();
        shift.ClosedAtUtc = null;
        shift.ClosedByUserId = null;
        shift.ExpectedCash = null;
        shift.ActualCash = null;
        shift.CashDifference = null;
        await _repository.SaveChangesAsync(ct);

        var names = await _repository.GetUserNamesAsync(new[] { shift.CashierUserId }, ct);
        return StoreShiftMapper.ToResponse(shift, names);
    }
}
