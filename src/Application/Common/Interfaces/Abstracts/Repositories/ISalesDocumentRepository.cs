using Application.Orders.Queries.SalesDocuments;

namespace Application.Common.Interfaces.Abstracts.Repositories;

/// <summary>The sales documents: paid and cancelled orders.</summary>
public interface ISalesDocumentRepository
{
    /// <summary>Totals of the filter, ignoring its status.</summary>
    Task<SalesDocumentsSummary> GetSummaryAsync(SalesDocumentFilter filter, CancellationToken cancellationToken);

    /// <summary>Newest first.</summary>
    Task<(List<SalesDocumentRow> Items, int Total)> GetPageAsync(
        SalesDocumentFilter filter, int skip, int take, CancellationToken cancellationToken);
}
