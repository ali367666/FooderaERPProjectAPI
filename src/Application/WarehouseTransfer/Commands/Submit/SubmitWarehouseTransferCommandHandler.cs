using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Interfaces.Abstracts.Services;
using Application.Common.Models;
using Application.Common.Responce;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Application.WarehouseTransfer.Commands.Submit;

public class SubmitWarehouseTransferCommandHandler
    : IRequestHandler<SubmitWarehouseTransferCommand, BaseResponse>
{
    private readonly IWarehouseTransferRepository _warehouseTransferRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SubmitWarehouseTransferCommandHandler> _logger;

    public SubmitWarehouseTransferCommandHandler(
        IWarehouseTransferRepository warehouseTransferRepository,
        IWarehouseRepository warehouseRepository,
        IEmailService emailService,
        IAuditLogService auditLogService,
        ILogger<SubmitWarehouseTransferCommandHandler> logger)
    {
        _warehouseTransferRepository = warehouseTransferRepository;
        _warehouseRepository = warehouseRepository;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<BaseResponse> Handle(
        SubmitWarehouseTransferCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "SubmitWarehouseTransferCommand başladı. TransferId: {TransferId}",
            request.Id);

        var transfer = await _warehouseTransferRepository
            .GetByIdWithLinesAsync(request.Id, cancellationToken);

        if (transfer is null)
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Transfer tapılmadı. TransferId: {TransferId}",
                request.Id);

            return BaseResponse.Fail("Warehouse transfer not found.");
        }

        if (transfer.Status != TransferStatus.Draft)
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Status uyğun deyil. TransferId: {TransferId}, CurrentStatus: {CurrentStatus}",
                transfer.Id,
                transfer.Status);

            return BaseResponse.Fail("Only draft warehouse transfers can be submitted.");
        }

        if (transfer.FromWarehouseId == transfer.ToWarehouseId)
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Eyni warehouse seçilib. TransferId: {TransferId}, FromWarehouseId: {FromWarehouseId}, ToWarehouseId: {ToWarehouseId}",
                transfer.Id,
                transfer.FromWarehouseId,
                transfer.ToWarehouseId);

            return BaseResponse.Fail("From warehouse and To warehouse cannot be the same.");
        }

        if (!transfer.VehicleWarehouseId.HasValue)
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. VehicleWarehouse seçilməyib. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Vehicle warehouse is required.");
        }

        if (transfer.VehicleWarehouseId.Value == transfer.FromWarehouseId ||
            transfer.VehicleWarehouseId.Value == transfer.ToWarehouseId)
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Vehicle warehouse uyğun deyil. TransferId: {TransferId}, VehicleWarehouseId: {VehicleWarehouseId}",
                transfer.Id,
                transfer.VehicleWarehouseId.Value);

            return BaseResponse.Fail("Vehicle warehouse cannot be the same as From or To warehouse.");
        }

        if (transfer.Lines is null || !transfer.Lines.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Line yoxdur. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("Warehouse transfer must contain at least one line.");
        }

        if (transfer.Lines.Any(x => x.Quantity <= 0))
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Quantity 0 və ya mənfidir. TransferId: {TransferId}",
                transfer.Id);

            return BaseResponse.Fail("All line quantities must be greater than 0.");
        }

        var duplicateStockItemIds = transfer.Lines
            .GroupBy(x => x.StockItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateStockItemIds.Any())
        {
            _logger.LogWarning(
                "Warehouse transfer submit olunmadı. Duplicate StockItem var. TransferId: {TransferId}, DuplicateStockItemIds: {DuplicateStockItemIds}",
                transfer.Id,
                string.Join(", ", duplicateStockItemIds));

            return BaseResponse.Fail("The same StockItem cannot be added more than once in a transfer.");
        }

        var oldStatus = transfer.Status;
        var lineCount = transfer.Lines.Count;

        transfer.Status = TransferStatus.Pending;

        _warehouseTransferRepository.Update(transfer);
        await _warehouseTransferRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await _auditLogService.LogAsync(
                new AuditLogEntry
                {
                    EntityName = "WarehouseTransfer",
                    EntityId = transfer.Id.ToString(),
                    ActionType = "Submit",
                    Message = $"WarehouseTransfer submit olundu. Id: {transfer.Id}, FromWarehouseId: {transfer.FromWarehouseId}, ToWarehouseId: {transfer.ToWarehouseId}, VehicleWarehouseId: {transfer.VehicleWarehouseId}, OldStatus: {oldStatus}, NewStatus: {transfer.Status}, LineCount: {lineCount}",
                    IsSuccess = true
                },
                cancellationToken);

            _logger.LogInformation(
                "WarehouseTransfer üçün audit log yazıldı. TransferId: {TransferId}",
                transfer.Id);
        }
        catch (Exception auditEx)
        {
            _logger.LogError(
                auditEx,
                "WarehouseTransfer submit audit log yazılarkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        try
        {
            var toWarehouse = await _warehouseRepository.GetByIdWithResponsibleAsync(
                transfer.ToWarehouseId,
                transfer.CompanyId,
                cancellationToken);

            var fromWarehouse = await _warehouseRepository.GetByIdAsync(
                transfer.FromWarehouseId,
                cancellationToken);

            if (toWarehouse is null)
            {
                _logger.LogWarning(
                    "Warehouse transfer submit email göndərilmədi. To warehouse tapılmadı. TransferId: {TransferId}, ToWarehouseId: {ToWarehouseId}",
                    transfer.Id,
                    transfer.ToWarehouseId);
            }
            else
            {
                var approverEmail = toWarehouse.ResponsibleEmployee?.Email;

                if (!string.IsNullOrWhiteSpace(approverEmail))
                {
                    var baseUrl = "https://localhost:7145";

                    var approveUrl = $"{baseUrl}/api/WarehouseTransfers/{transfer.Id}/approve-from-mail";
                    var rejectUrl = $"{baseUrl}/api/WarehouseTransfers/{transfer.Id}/reject-from-mail";

                    var linesHtml = new StringBuilder();

                    foreach (var line in transfer.Lines)
                    {
                        linesHtml.Append($@"
<tr>
    <td style='border:1px solid #ddd;padding:8px;'>{line.StockItem?.Name ?? "-"}</td>
    <td style='border:1px solid #ddd;padding:8px;'>{line.Quantity:0.##}</td>
</tr>");
                    }

                    var body = $@"
<html>
<head>
    <meta charset='UTF-8' />
</head>
<body style='font-family:Arial,sans-serif;line-height:1.6;color:#212529;'>
    <h2>Yeni warehouse transfer təsdiq gözləyir</h2>

    <p><strong>Transfer nömrəsi:</strong> #{transfer.Id}</p>
    <p><strong>Göndərən anbar:</strong> {fromWarehouse?.Name ?? "-"}</p>
    <p><strong>Qəbul edən anbar:</strong> {toWarehouse.Name}</p>
    <p><strong>Qeyd:</strong> {transfer.Note ?? "-"}</p>

    <h3>Transfer sətirləri</h3>

    <table style='border-collapse:collapse;width:100%;max-width:700px;'>
        <thead>
            <tr style='background-color:#f8f9fa;'>
                <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Stok adı</th>
                <th style='border:1px solid #ddd;padding:8px;text-align:left;'>Miqdar</th>
            </tr>
        </thead>
        <tbody>
            {linesHtml}
        </tbody>
    </table>

    <br />

    <p>
        <a href='{approveUrl}' 
           style='display:inline-block;padding:10px 16px;background:#198754;color:white;text-decoration:none;border-radius:6px;'>
           Approve
        </a>

        &nbsp;

        <a href='{rejectUrl}' 
           style='display:inline-block;padding:10px 16px;background:#dc3545;color:white;text-decoration:none;border-radius:6px;'>
           Reject
        </a>
    </p>

    <p><strong>Approve link:</strong></p>
    <p>{approveUrl}</p>

    <p><strong>Reject link:</strong></p>
    <p>{rejectUrl}</p>
</body>
</html>";

                    await _emailService.SendAsync(
                        approverEmail,
                        $"Warehouse transfer approval #{transfer.Id}",
                        body,
                        cancellationToken);

                    _logger.LogInformation(
                        "WarehouseTransfer submit email göndərildi. TransferId: {TransferId}, Email: {Email}",
                        transfer.Id,
                        approverEmail);
                }
                else
                {
                    _logger.LogWarning(
                        "Warehouse transfer submit email göndərilmədi. Responsible employee email tapılmadı. TransferId: {TransferId}, ToWarehouseId: {ToWarehouseId}",
                        transfer.Id,
                        transfer.ToWarehouseId);
                }
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(
                emailEx,
                "WarehouseTransfer submit email göndərilərkən xəta baş verdi. TransferId: {TransferId}",
                transfer.Id);
        }

        _logger.LogInformation(
            "Warehouse transfer uğurla submit olundu. TransferId: {TransferId}",
            transfer.Id);

        return BaseResponse.Ok("Warehouse transfer submitted successfully.");
    }
}