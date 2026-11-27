using Application.Common.Interfaces;
using Application.Common.Responce;
using MediatR;

namespace Application.CompanySettings.Commands.SetPaymentBanks;

public record PaymentBankStatusResponse(string Name, bool Enabled);

/// <summary>The banks a company has switched on (SuperAdmin only).</summary>
public record GetCompanyPaymentBanksQuery(int CompanyId) : IRequest<List<PaymentBankStatusResponse>>;

/// <summary>Gives a company exactly these banks (SuperAdmin only); the others are switched off.</summary>
public record SetCompanyPaymentBanksCommand(int CompanyId, List<string> Names) : IRequest<BaseResponse>;

public class GetCompanyPaymentBanksQueryHandler : IRequestHandler<GetCompanyPaymentBanksQuery, List<PaymentBankStatusResponse>>
{
    private readonly IPaymentBankProvisioner _banks;
    private readonly ICurrentUserService _currentUser;

    public GetCompanyPaymentBanksQueryHandler(IPaymentBankProvisioner banks, ICurrentUserService currentUser)
    {
        _banks = banks;
        _currentUser = currentUser;
    }

    public async Task<List<PaymentBankStatusResponse>> Handle(GetCompanyPaymentBanksQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsSuperAdmin)
            throw new Application.Common.Exceptions.BadRequestException("Bu əməliyyat yalnız SuperAdmin üçündür.");
        return (await _banks.GetAsync(request.CompanyId, ct)).Select(b => new PaymentBankStatusResponse(b.Name, b.Enabled)).ToList();
    }
}

public class SetCompanyPaymentBanksCommandHandler : IRequestHandler<SetCompanyPaymentBanksCommand, BaseResponse>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IPaymentBankProvisioner _banks;
    private readonly ICurrentUserService _currentUser;

    public SetCompanyPaymentBanksCommandHandler(ICompanyRepository companyRepository, IPaymentBankProvisioner banks, ICurrentUserService currentUser)
    {
        _companyRepository = companyRepository;
        _banks = banks;
        _currentUser = currentUser;
    }

    public async Task<BaseResponse> Handle(SetCompanyPaymentBanksCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsSuperAdmin)
            return BaseResponse.Fail("Bu əməliyyat yalnız SuperAdmin üçündür.");
        if (!await _companyRepository.ExistsAsync(request.CompanyId, ct))
            return BaseResponse.Fail("Company tapılmadı.");

        await _banks.SetAsync(request.CompanyId, request.Names ?? new List<string>(), ct);
        return BaseResponse.Ok("Banklar yadda saxlanıldı.");
    }
}
