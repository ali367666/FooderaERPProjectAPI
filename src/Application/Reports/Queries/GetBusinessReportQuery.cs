using Application.Reports.Dtos;
using MediatR;

namespace Application.Reports.Queries;

public record GetBusinessReportQuery(BusinessReportRequest Request) : IRequest<BusinessReportResponse>;
