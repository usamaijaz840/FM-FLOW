using FMFlow.Employees.Interface.DTOs;
using FMFlow.Entities;
using Riok.Mapperly.Abstractions;

namespace FMFlow.Employees.Service.Mapper;

[Mapper]
public partial class EmployeeMapper
{
	[MapNestedProperties(nameof(FlowUser.EmployeeUser))]
	[MapperIgnoreSource(nameof(FlowUser.ProUser))]
	[MapperIgnoreSource(nameof(FlowUser.IdentityGuid))]
	public partial EmployeeResponseDto MapToEmployeeDto(FlowUser user);

	public partial EmployeeResponseDto MapToEmployeeDto(EmployeeUser user);

	[MapProperty(nameof(EmployeeRequestDto.Role), nameof(FlowUser.EmployeeUser.Role))]
	[MapProperty(nameof(EmployeeRequestDto.Biography), nameof(FlowUser.EmployeeUser.Biography))]
	[MapProperty(nameof(EmployeeRequestDto.Memo), nameof(FlowUser.EmployeeUser.Memo))]
	[MapProperty(nameof(EmployeeRequestDto.DailyGoal), nameof(FlowUser.EmployeeUser.DailyGoal))]
	[MapProperty(nameof(EmployeeRequestDto.BurdenRate), nameof(FlowUser.EmployeeUser.BurdenRate))]
	[MapProperty(nameof(EmployeeRequestDto.Skills), nameof(FlowUser.EmployeeUser.Skills))]
	[MapProperty(nameof(EmployeeRequestDto.TwilioNumber), nameof(FlowUser.EmployeeUser.TwilioNumber))]
	[MapProperty(nameof(EmployeeRequestDto.TwilioCallerID), nameof(FlowUser.EmployeeUser.TwilioCallerID))]
	[MapperIgnoreTarget(nameof(FlowUser.ProUser))]
	[MapperIgnoreTarget(nameof(FlowUser.IdentityGuid))]
	public partial FlowUser MapToEmployeeUser(EmployeeRequestDto EmployeeRequestDto);

	[MapProperty([nameof(EmployeeUser.FlowUser), nameof(ProUserDetail.FlowUser.FirstName)], nameof(SearchEmployeeAndRoleDto.FirstName))]
	[MapProperty([nameof(EmployeeUser.FlowUser), nameof(ProUserDetail.FlowUser.LastName)], nameof(SearchEmployeeAndRoleDto.LastName))]
	[MapProperty([nameof(EmployeeUser.FlowUser), nameof(ProUserDetail.FlowUser.Email)], nameof(SearchEmployeeAndRoleDto.EmailAddress))]
	[MapProperty([nameof(EmployeeUser.FlowUser), nameof(ProUserDetail.FlowUser.IsDeleted)], nameof(SearchEmployeeAndRoleDto.IsDeleted))]
	public partial SearchEmployeeAndRoleDto Map(EmployeeUser employeeUser);

	public Address CreateAddressFromEmployeeRequest(EmployeeRequestDto employeeRequest, State state)
	{
		return new Address
		{
			Line1 = employeeRequest.AddressLine1,
			Line2 = employeeRequest.AddressLine2,
			State = state,
			City = employeeRequest.City,
			ZipCode = employeeRequest.ZipCode
		};
	}

	public EmployeeResponseDto MapToEmployeeDtoFromEmployeeUser(EmployeeUser employeeUser)
	{
		var dto = MapToEmployeeDto(employeeUser.FlowUser);

		if (employeeUser.Address != null)
		{
			dto.AddressLine1 = employeeUser.Address.Line1;
			dto.AddressLine2 = employeeUser.Address.Line2;
			dto.City = employeeUser.Address.City;
			dto.ZipCode = employeeUser.Address.ZipCode;
			dto.State = employeeUser.Address.State?.Abbreviation;
		}

		return dto;
	}
}
