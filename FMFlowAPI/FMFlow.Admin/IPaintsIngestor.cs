using FMFlow.FlowAPI.Interface;

namespace FMFlow.Admin.Interface;
public interface IPaintsIngestor
{
	Task<Result> ProcessXlsx(Stream xlsxStream, CancellationToken ct);
}
