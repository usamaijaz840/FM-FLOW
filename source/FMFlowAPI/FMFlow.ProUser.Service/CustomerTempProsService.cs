using EFRepository;
using FMFlow.Entities;
using FMFlow.Identity.Interface;
using FMFlow.ProUser.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.ProUser.Service;

public class CustomerTempProsService(ICurrentUserService currentUserService, IRepository repository) : ICustomerTempProsService
{
	public async Task<int?> CreateTempProId(int proId, CancellationToken ct)
	{
		if (!currentUserService.IsTempCustomer() && !currentUserService.IsCustomer())
		{
			return null;
		}

		var customerId = currentUserService.GetUserID();

		var nextTempPro = new CustomerTempPro
		{
			ProId = proId,
			CustomerId = customerId
		};

		repository.AddNew(nextTempPro);

		await repository.SaveAsync(ct);

		return nextTempPro.CustomerTempProId;
	}

	public async Task<int?> GetProId(int tempProId, CancellationToken ct)
	{
		var result = await repository.Query<CustomerTempPro>()
			.ByExpireDateTimeIsAfter(DateTimeOffset.UtcNow)
			.ByCustomerTempProId(tempProId)
			.Select(x => x.ProId)
			.FirstOrDefaultAsync(ct);

		return result == 0 ? null : result;
	}

	public async Task<int?> GetTempProId(int proId, CancellationToken ct)
	{
		var result = await repository.Query<CustomerTempPro>()
			.ByExpireDateTimeIsAfter(DateTimeOffset.UtcNow)
			.ByProId(proId)
			.Select(x => x.CustomerTempProId)
			.FirstOrDefaultAsync(ct);

		return result == 0 ? null : result;
	}
}
