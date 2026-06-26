using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMFlow.ProUser.Interface;

public interface ICustomerTempProsService
{
	Task<int?> GetProId(int tempProId, CancellationToken ct);
	Task<int?> GetTempProId(int proId, CancellationToken ct);
	Task<int?> CreateTempProId(int proId, CancellationToken ct);
}
