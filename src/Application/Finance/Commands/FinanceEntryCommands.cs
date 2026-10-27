using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Finance.Dtos;
using Domain.Entities;
using MediatR;

namespace Application.Finance.Commands;

public record CreateFinanceEntryCommand(SaveFinanceEntryRequest Request) : IRequest<FinanceEntryResponse>;
public record UpdateFinanceEntryCommand(int Id, SaveFinanceEntryRequest Request) : IRequest<FinanceEntryResponse>;
public record DeleteFinanceEntryCommand(int Id) : IRequest;

internal static class FinanceEntryMapper
{
    public static void Validate(SaveFinanceEntryRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BadRequestException("Ad boş ola bilməz.");
        if (dto.Amount <= 0)
            throw new BadRequestException("Məbləğ 0-dan böyük olmalıdır.");
        if (!Enum.IsDefined(dto.Type))
            throw new BadRequestException("Növ yanlışdır.");
    }

    public static void Apply(FinanceEntry entry, SaveFinanceEntryRequest dto)
    {
        entry.RestaurantId = dto.RestaurantId is > 0 ? dto.RestaurantId : null;
        entry.Type = dto.Type;
        entry.Name = dto.Name.Trim();
        entry.Amount = dto.Amount;
        entry.EntryDate = dto.EntryDate.Date;
        entry.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
    }

    public static FinanceEntryResponse Map(FinanceEntry e) => new()
    {
        Id = e.Id,
        RestaurantId = e.RestaurantId,
        Type = e.Type,
        Name = e.Name,
        Amount = e.Amount,
        EntryDate = e.EntryDate,
        Note = e.Note,
        CreatedByUserName = e.CreatedByUser?.FullName,
        CreatedAtUtc = e.CreatedAtUtc,
    };
}

public class CreateFinanceEntryCommandHandler : IRequestHandler<CreateFinanceEntryCommand, FinanceEntryResponse>
{
    private readonly IFinanceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateFinanceEntryCommandHandler(IFinanceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<FinanceEntryResponse> Handle(CreateFinanceEntryCommand command, CancellationToken cancellationToken)
    {
        FinanceEntryMapper.Validate(command.Request);

        var entry = new FinanceEntry
        {
            CompanyId = _currentUserService.CompanyId,
            CreatedByUserId = _currentUserService.UserId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        FinanceEntryMapper.Apply(entry, command.Request);

        await _repository.AddAsync(entry, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return FinanceEntryMapper.Map(entry);
    }
}

public class UpdateFinanceEntryCommandHandler : IRequestHandler<UpdateFinanceEntryCommand, FinanceEntryResponse>
{
    private readonly IFinanceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateFinanceEntryCommandHandler(IFinanceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<FinanceEntryResponse> Handle(UpdateFinanceEntryCommand command, CancellationToken cancellationToken)
    {
        FinanceEntryMapper.Validate(command.Request);

        var entry = await _repository.GetByIdAsync(_currentUserService.CompanyId, command.Id, cancellationToken)
            ?? throw new NotFoundException("Qeyd tapılmadı.");

        FinanceEntryMapper.Apply(entry, command.Request);
        entry.LastModifiedByUserId = _currentUserService.UserId;
        entry.LastModifiedAtUtc = DateTime.UtcNow;

        await _repository.SaveChangesAsync(cancellationToken);
        return FinanceEntryMapper.Map(entry);
    }
}

public class DeleteFinanceEntryCommandHandler : IRequestHandler<DeleteFinanceEntryCommand>
{
    private readonly IFinanceRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteFinanceEntryCommandHandler(IFinanceRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteFinanceEntryCommand command, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(_currentUserService.CompanyId, command.Id, cancellationToken)
            ?? throw new NotFoundException("Qeyd tapılmadı.");

        _repository.Remove(entry);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
