using Application.Analytics.Dtos;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Abstracts.Repositories;
using MediatR;

namespace Application.Analytics.Queries;

public record GetSalesReportQuery(DateTime From, DateTime To) : IRequest<SalesReportResponse>;
