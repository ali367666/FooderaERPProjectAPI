namespace Application.BackOffice;

public class SaveEmployeeLeaveRequest
{
    public int EmployeeId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? Note { get; set; }
}

public record EmployeeLeaveResponse(int Id, int EmployeeId, string EmployeeName, DateTime FromDate, DateTime ToDate, string? Note);

public interface IEmployeeLeaveService
{
    Task<List<EmployeeLeaveResponse>> GetAsync(int companyId, int? employeeId, CancellationToken ct);
    Task<EmployeeLeaveResponse> AddAsync(int companyId, SaveEmployeeLeaveRequest request, CancellationToken ct);
    Task DeleteAsync(int companyId, int id, CancellationToken ct);
}
