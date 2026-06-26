using FMFlow.ProUser.Interface.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.ProUser.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class ProZipcodeMapper
{
	[MapProperty([nameof(Entities.ZipCode.State), nameof(Entities.ZipCode.State.StateName)], nameof(FMFlow.ProUser.ProZipcode.StateName))]
	public partial ProZipcode Map(Entities.ZipCode proZipcode);

	[MapperIgnoreTarget(nameof(StateWithCounties.Counties))]

	public ProZipCodeStateResponseDto MapToStateWithCounties(IEnumerable<Entities.ZipCode> proZipcode)
	{
		return new ProZipCodeStateResponseDto()
		{
			States = proZipcode
			.GroupBy(p => new { p.StateAbbreviation, p.State })
			.Select(stateGroup => new StateWithCounties
			{
				Abbreviation = stateGroup.Key.StateAbbreviation,
				Name = stateGroup.Key.State.StateName,
				Counties = stateGroup
					.GroupBy(z => z.County)
					.Select(countyGroup => new CountyWithProZipcode
					{
						Name = countyGroup.Key,
					})
					.ToList()
			})
			.GroupBy(s => s.Abbreviation)
			.Select(g => new StateWithCounties
			{
				Abbreviation = g.Key,
				Name = g.First().Name,
				Counties = g.SelectMany(s => s.Counties)
							.GroupBy(c => c.Name)
							.Select(cg => new CountyWithProZipcode
							{
								Name = cg.Key
							})
							.ToList()
			})
			.ToList()
		};
	}



}
