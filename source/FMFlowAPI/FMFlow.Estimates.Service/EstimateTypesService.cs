using EFRepository;
using FMFlow.Entities;
using FMFlow.Estimates.Interface;
using FMFlow.FlowAPI.Interface;
using Microsoft.EntityFrameworkCore;

namespace FMFlow.Estimates.Service;

public class EstimateTypesService(IRepository repository) : IEstimateTypesService
{
	public async Task<Result<EstimateType>> GetEstimateTypeById(int id, CancellationToken ct)
	{
		try
		{
			var estimateType = await repository
				.Query<EstimateType>()
				.FirstOrDefaultAsync(pt => pt.EstimateTypeId == id, ct);

			if (estimateType == null)
				return Result<EstimateType>.Failure(ErrorMessages.ResourceNotFound, ResultErrorType.NotFound);

			return Result<EstimateType>.Success(estimateType);
		}
		catch (Exception ex)
		{
			return Result<EstimateType>.Failure($"An error occurred while retrieving the estimate type: {ex.Message}", ResultErrorType.BadRequest);
		}

	}

	public async Task<Result<List<EstimateType>>> GetEstimateTypes(CancellationToken ct)
	{
		try
		{
			var estimateTypes = await repository
				.Query<EstimateType>()
				.OrderBy(pt => pt.EstimateTypeId)
				.ToListAsync(ct);

			return Result<List<EstimateType>>.Success(estimateTypes);
		}
		catch (Exception ex)
		{
			return Result<List<EstimateType>>.Failure($"An error occurred while retrieving estimate types: {ex.Message}", ResultErrorType.BadRequest);
		}
	}
}
