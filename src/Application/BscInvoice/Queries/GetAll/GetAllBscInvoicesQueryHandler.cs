using Application.BscInvoice.Dtos;
using Application.Common.Interfaces.Abstracts.Repositories;
using Application.Common.Responce;
using MediatR;

namespace Application.BscInvoice.Queries.GetAll;

public class GetAllBscInvoicesQueryHandler
    : IRequestHandler<GetAllBscInvoicesQuery, BaseResponse<List<BscInvoiceMResponse>>>
{
    private readonly IBscInvoiceRepository _repository;

    public GetAllBscInvoicesQueryHandler(IBscInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<BaseResponse<List<BscInvoiceMResponse>>> Handle(
        GetAllBscInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _repository.GetAllAsync(request.DocDate, cancellationToken);

        var result = invoices.Select(m => new BscInvoiceMResponse
        {
            Id = m.Id,
            BscInvoiceMId = m.BscInvoiceMId,
            DocNo = m.DocNo,
            DocDate = m.DocDate,
            EntityId = m.EntityId,
            BranchId = m.BranchId,
            CoId = m.CoId,
            Amt = m.Amt,
            AmtVat = m.AmtVat,
            PurchaseSales = m.PurchaseSales,
            BscCreateDate = m.BscCreateDate,
            Lines = m.Lines.Select(d => new BscInvoiceDResponse
            {
                Id = d.Id,
                BscInvoiceDId = d.BscInvoiceDId,
                BscInvoiceMId = d.BscInvoiceMId,
                LineNo = d.LineNo,
                ItemId = d.ItemId,
                Qty = d.Qty,
                UnitPrice = d.UnitPrice,
                Amt = d.Amt,
                AmtVat = d.AmtVat,
                VatRate = d.VatRate,
                BranchId = d.BranchId,
                CoId = d.CoId,
                DocDate = d.DocDate,
                BscCreateDate = d.BscCreateDate,
            }).ToList(),
        }).ToList();

        return new BaseResponse<List<BscInvoiceMResponse>> { Success = true, Data = result };
    }
}
