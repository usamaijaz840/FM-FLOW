using System.Text.Json;
using FMFlow.Estimates.Interface.Models;

namespace FMFlow.Estimates.Interface;

public interface IEstimateCalculatorService
{
	EstimateCalculationResponse Calculate(JsonDocument attributes, int proUserId);
}
