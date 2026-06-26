using FMFlow.Common;
using FMFlow.Employees.Interface.DTOs;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Employees.Interface;

public interface IEmployeeService
{
	Task<Result<EmployeeResponseDto>> CreateEmployeeUser(EmployeeRequestDto employeeRequest, CancellationToken ct);

	Task<Result<EmployeeResponseDto>> UpdateEmployeeUser(int employeeId, EmployeeRequestDto request, CancellationToken ct);

	Task<Result<SearchResult<SearchEmployeeAndRoleDto>>> SearchEmployeesAndRoles(int pageIndex, int pageSize, string? keywordSearch, string? userRole, CancellationToken ct);

	Task<Result<EmployeeResponseDto>> GetEmployeeDetailsByUserId(int userID, CancellationToken ct);
}
