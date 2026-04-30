using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.WarehouseStock.Commands.DeleteDocument;

public class DeleteWarehouseStockDocumentCommandHandler
    : IRequestHandler<DeleteWarehouseStockDocumentCommand, BaseResponse>
{
    private readonly IWarehouseStockDocumentRepository _documentRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<DeleteWarehouseStockDocumentCommandHandler> _logger;

    public DeleteWarehouseStockDocumentCommandHandler(
        IWarehouseStockDocumentRepository documentRepository,
        IAuditLogService auditLogService,
        ILogger<DeleteWarehouseStockDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        DeleteWarehouseStockDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdWithLinesAsync(request.Id, cancellationToken);
        if (document is null)
        {
            return BaseResponse.Fail("Warehouse stock document not found.");
        }

        if (document.Status != WarehouseStockDocumentStatus.Draft)
        {
            return BaseResponse.Fail("Only draft documents can be deleted.");
        }

        _documentRepository.Remove(document);
        await _documentRepository.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            EntityName = "WarehouseStockDocument",
            EntityId = request.Id.ToString(),
            ActionType = "Delete",
            Message = "Warehouse stock document deleted.",
            CompanyId = document.CompanyId,
            IsSuccess = true
        }, cancellationToken);

        _logger.LogInformation("DeleteWarehouseStockDocument completed. Id: {Id}", request.Id);

        return BaseResponse.Ok("Warehouse stock document deleted successfully.");
    }
}
