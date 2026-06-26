using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMFlow.Entities;
using FMFlow.FlowAPI.Interface;

namespace FMFlow.Estimates.Interface;

public interface IEstimateTypesService
{
	public Task<Result<List<EstimateType>>> GetEstimateTypes(CancellationToken ct);

	public Task<Result<EstimateType>> GetEstimateTypeById(int id,  CancellationToken ct);
}
