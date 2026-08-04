using Application.BscInvoice.Dtos;
using Application.Common.Responce;
using MediatR;

namespace Application.BscInvoice.Queries.GetAll;

public record GetAllBscInvoicesQuery(DateTime? DocDate = null)
    : IRequest<BaseResponse<List<BscInvoiceMResponse>>>;
