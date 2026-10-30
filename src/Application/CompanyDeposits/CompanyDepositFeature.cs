using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using Domain.Entities;
using MediatR;

namespace Application.CompanyDeposits;

public class AddCompanyDepositRequest
{
    /// <summary>Amount in AZN, more than zero.</summary>
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class CompanyDepositResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }

    /// <summary>True for the amount the company started working with.</summary>
    public bool IsOpening { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
}

public class CompanyDepositSummaryResponse
{
    /// <summary>What the company started with; null until it is recorded.</summary>
    public decimal? OpeningAmount { get; set; }
    public DateTime? OpeningAtUtc { get; set; }

    /// <summary>Opening deposit plus every later deposit.</summary>
    public decimal TotalAmount { get; set; }

    public List<CompanyDepositResponse> Entries { get; set; } = new();
}

public record GetCompanyDepositsQuery : IRequest<CompanyDepositSummaryResponse>;

public record AddCompanyDepositCommand(AddCompanyDepositRequest Request) : IRequest<BaseResponse>;

public class GetCompanyDepositsQueryHandler : IRequestHandler<GetCompanyDepositsQuery, CompanyDepositSummaryResponse>
{
    private readonly ICompanyDepositRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public GetCompanyDepositsQueryHandler(ICompanyDepositRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<CompanyDepositSummaryResponse> Handle(GetCompanyDepositsQuery query, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetByCompanyAsync(_currentUserService.CompanyId, cancellationToken);
        var names = await _repository.GetUserNamesAsync(rows.Select(r => r.CreatedByUserId).OfType<int>(), cancellationToken);

        var opening = rows.FirstOrDefault(r => r.IsOpening);
        return new CompanyDepositSummaryResponse
        {
            OpeningAmount = opening?.Amount,
            OpeningAtUtc = opening?.CreatedAtUtc,
            TotalAmount = rows.Sum(r => r.Amount),
            Entries = rows.Select(r => new CompanyDepositResponse
            {
                Id = r.Id,
                Amount = r.Amount,
                IsOpening = r.IsOpening,
                Note = r.Note,
                CreatedAtUtc = r.CreatedAtUtc,
                CreatedByUserId = r.CreatedByUserId,
                CreatedByUserName = r.CreatedByUserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            }).ToList(),
        };
    }
}

public class AddCompanyDepositCommandHandler : IRequestHandler<AddCompanyDepositCommand, BaseResponse>
{
    private readonly ICompanyDepositRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public AddCompanyDepositCommandHandler(ICompanyDepositRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<BaseResponse> Handle(AddCompanyDepositCommand command, CancellationToken cancellationToken)
    {
        var amount = Math.Round(command.Request.Amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0)
            return new BaseResponse { Success = false, Message = "Depozit məbləği 0-dan böyük olmalıdır." };

        var companyId = _currentUserService.CompanyId;

        // The first deposit is the amount the company starts working with.
        var isOpening = !await _repository.HasOpeningAsync(companyId, cancellationToken);

        await _repository.AddAsync(new CompanyDeposit
        {
            CompanyId = companyId,
            Amount = amount,
            IsOpening = isOpening,
            Note = string.IsNullOrWhiteSpace(command.Request.Note) ? null : command.Request.Note.Trim(),
            CreatedByUserId = _currentUserService.UserId,
        }, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new BaseResponse
        {
            Success = true,
            Message = isOpening ? "Başlanğıc depozit qeyd olundu." : "Əlavə depozit qeyd olundu."
        };
    }
}
