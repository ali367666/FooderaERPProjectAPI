using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Discounts.Dtos;
using Domain.Enums;
using MediatR;

namespace Application.Discounts.Commands.Update;

public class UpdateDiscountCommandHandler : IRequestHandler<UpdateDiscountCommand, DiscountResponse>
{
    private readonly IDiscountRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public UpdateDiscountCommandHandler(IDiscountRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<DiscountResponse> Handle(UpdateDiscountCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUser.CompanyId;
        var dto = request.Request;
        var code = dto.Code.Trim().ToUpperInvariant();

        var discount = await _repo.GetByIdAsync(request.Id, companyId, cancellationToken)
            ?? throw new Exception("Endirim tapılmadı.");

        if (await _repo.CodeExistsAsync(code, companyId, request.Id, cancellationToken))
            throw new Exception($"'{code}' kodlu endirim artıq mövcuddur.");

        if (!Enum.TryParse<DiscountType>(dto.Type, true, out var type))
            throw new Exception("Endirim növü yanlışdır.");

        if (dto.Value <= 0)
            throw new Exception("Endirim dəyəri sıfırdan böyük olmalıdır.");

        if (type == DiscountType.Percentage && dto.Value > 100)
            throw new Exception("Faiz endirimi 100-dən çox ola bilməz.");

        discount.Code = code;
        discount.Name = dto.Name.Trim();
        discount.Type = type;
        discount.Value = dto.Value;
        discount.MinOrderAmount = dto.MinOrderAmount;
        discount.MaxDiscountAmount = dto.MaxDiscountAmount;
        discount.StartDate = dto.StartDate.Date;
        discount.EndDate = dto.EndDate.Date;
        discount.StartTime = string.IsNullOrWhiteSpace(dto.StartTime) ? null : TimeSpan.Parse(dto.StartTime);
        discount.EndTime = string.IsNullOrWhiteSpace(dto.EndTime) ? null : TimeSpan.Parse(dto.EndTime);
        discount.MaxUsageCount = dto.MaxUsageCount;
        discount.IsActive = dto.IsActive;

        _repo.Update(discount);
        await _repo.SaveChangesAsync(cancellationToken);

        return DiscountMapper.ToResponse(discount);
    }
}
