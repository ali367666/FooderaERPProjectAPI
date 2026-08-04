using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Discounts.Dtos;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Discounts.Commands.Create;

public class CreateDiscountCommandHandler : IRequestHandler<CreateDiscountCommand, DiscountResponse>
{
    private readonly IDiscountRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public CreateDiscountCommandHandler(IDiscountRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<DiscountResponse> Handle(CreateDiscountCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Request;
        var companyId = _currentUser.CompanyId;
        var code = dto.Code.Trim().ToUpperInvariant();

        if (await _repo.CodeExistsAsync(code, companyId, null, cancellationToken))
            throw new Exception($"'{code}' kodlu endirim artıq mövcuddur.");

        if (!Enum.TryParse<DiscountType>(dto.Type, true, out var type))
            throw new Exception("Endirim növü yanlışdır. 'Percentage' və ya 'FixedAmount' olmalıdır.");

        if (dto.Value <= 0)
            throw new Exception("Endirim dəyəri sıfırdan böyük olmalıdır.");

        if (type == DiscountType.Percentage && dto.Value > 100)
            throw new Exception("Faiz endirimi 100-dən çox ola bilməz.");

        if (dto.EndDate.Date < dto.StartDate.Date)
            throw new Exception("Bitmə tarixi başlama tarixindən əvvəl ola bilməz.");

        var discount = new Discount
        {
            CompanyId = companyId,
            Code = code,
            Name = dto.Name.Trim(),
            Type = type,
            Value = dto.Value,
            MinOrderAmount = dto.MinOrderAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            StartTime = string.IsNullOrWhiteSpace(dto.StartTime) ? null : TimeSpan.Parse(dto.StartTime),
            EndTime = string.IsNullOrWhiteSpace(dto.EndTime) ? null : TimeSpan.Parse(dto.EndTime),
            MaxUsageCount = dto.MaxUsageCount,
            IsActive = true,
        };

        await _repo.AddAsync(discount, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        return DiscountMapper.ToResponse(discount);
    }
}
