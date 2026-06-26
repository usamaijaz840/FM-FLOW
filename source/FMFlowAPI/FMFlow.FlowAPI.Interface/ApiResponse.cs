namespace FMFlow.FlowAPI.Interface;

public class ApiResponse
{
	public bool IsSuccessful { get; set; }
	public string Message { get; set; } = string.Empty;
	public int Key { get; set; }
}
