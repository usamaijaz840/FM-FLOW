using FMFlow.Entities;
using FMFlow.Files.Interface.DTOs;
using FMFlow.Pro.Interface.Dtos;
using FMFlow.ProUser.Interface;
using FMFlow.ProUser.Interface.DTOs;
using Riok.Mapperly.Abstractions;

namespace FMFlow.ProUser.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, AllowNullPropertyAssignment = false)]
public partial class ProUserMapper
{
	[MapperIgnoreTarget(nameof(ProUserDetail.BusinessAddressID))]
	[MapperIgnoreTarget(nameof(ProUserDetail.SherwinHomeAddressID))]
	[MapProperty(nameof(ProUserDto.BusinessAddress), [nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.Line1)])]
	[MapProperty(nameof(ProUserDto.City), [nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.City)])]
	[MapProperty(nameof(ProUserDto.State), [nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.StateId)])]
	[MapProperty(nameof(ProUserDto.ZipCode), [nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.ZipCode)])]
	[MapProperty(nameof(ProUserDto.AddressOfStore), [nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.Line1)])]
	[MapProperty(nameof(ProUserDto.CityOfStore), [nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.City)])]
	[MapProperty(nameof(ProUserDto.StateOfStore), [nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.StateId)])]
	[MapProperty(nameof(ProUserDto.ZipCodeOfStore), [nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.ZipCode)])]
	public partial ProUserDetail MapToProUserDetail(ProUserDto user);

	[MapperIgnoreTarget(nameof(ProUserDetail.UserID))]
	[MapperIgnoreTarget(nameof(ProUserDetail.InsuranceExpDate))]
	[MapperIgnoreTarget(nameof(ProUserDetail.BusinessAddress))]
	[MapperIgnoreTarget(nameof(ProUserDetail.BusinessAddressID))]
	[MapperIgnoreTarget(nameof(ProUserDetail.SherwinHomeAddress))]
	[MapperIgnoreTarget(nameof(ProUserDetail.SherwinHomeAddressID))]
	[MapProperty(nameof(SearchedProUserDetails.FirstName), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.FirstName)])]
	[MapProperty(nameof(SearchedProUserDetails.LastName), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.LastName)])]
	[MapProperty(nameof(SearchedProUserDetails.Email), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.Email)])]
	[MapProperty(nameof(SearchedProUserDetails.PhoneNumber), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.PhoneNumber)])]
	public partial void MapToProUserDetail(UpdateProUser user, ProUserDetail existingValue);

	[MapProperty(nameof(ProUserDto.FirstName), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.FirstName)])]
	[MapProperty(nameof(ProUserDto.LastName), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.LastName)])]
	[MapProperty(nameof(ProUserDto.Email), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.Email)])]
	[MapProperty(nameof(ProUserDto.Phone), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.PhoneNumber)])]
	[MapProperty(nameof(ProUserDto.IsDeleted), [nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.IsDeleted)])]
	[MapProperty(nameof(ProUserDto.InsuranceExpDate), nameof(ProUserDetail.InsuranceExpDate))]
	[MapProperty(nameof(ProUserDto.BusinessAddress), [nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.Line1)])]
	[MapProperty(nameof(ProUserDto.City), [nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.City)])]
	[MapProperty(nameof(ProUserDto.State), [nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.StateId)])]
	[MapProperty(nameof(ProUserDto.ZipCode), [nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.ZipCode)])]
	[MapProperty(nameof(ProUserDto.AddressOfStore), [nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.Line1)])]
	[MapProperty(nameof(ProUserDto.CityOfStore), [nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.City)])]
	[MapProperty(nameof(ProUserDto.StateOfStore), [nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.StateId)])]
	[MapProperty(nameof(ProUserDto.ZipCodeOfStore), [nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.ZipCode)])]
	public partial void MapToProUserDetail(ProUserDto user, ProUserDetail existingValue);

	public partial void MapToProUserDetail(BusinessProfile value, ProUserDetail existingValue);
	[MapProperty(nameof(ProUserDto.Phone), nameof(FlowUser.PhoneNumber))]
	public partial FlowUser MapToFlowUser(ProUserDto user);
	[MapProperty([nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.FirstName)], nameof(ProUserDto.FirstName))]
	[MapProperty([nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.LastName)], nameof(ProUserDto.LastName))]
	[MapProperty([nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.Email)], nameof(ProUserDto.Email))]
	[MapProperty([nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.PhoneNumber)], nameof(ProUserDto.Phone))]
	[MapProperty([nameof(ProUserDetail.FlowUser), nameof(ProUserDetail.FlowUser.IsDeleted)], nameof(ProUserDto.IsDeleted))]
	[MapProperty([nameof(ProUserDetail.FMTimeZone), nameof(ProUserDetail.FMTimeZone.Name)], nameof(ProUserDto.TimeZone))]
	[MapProperty([nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.Line1)], nameof(ProUserDto.BusinessAddress))]
	[MapProperty([nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.City)], nameof(ProUserDto.City))]
	[MapProperty([nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.StateId)], nameof(ProUserDto.State))]
	[MapProperty([nameof(ProUserDetail.BusinessAddress), nameof(ProUserDetail.BusinessAddress.ZipCode)], nameof(ProUserDto.ZipCode))]
	[MapProperty([nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.Line1)], nameof(ProUserDto.AddressOfStore))]
	[MapProperty([nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.City)], nameof(ProUserDto.CityOfStore))]
	[MapProperty([nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.StateId)], nameof(ProUserDto.StateOfStore))]
	[MapProperty([nameof(ProUserDetail.SherwinHomeAddress), nameof(ProUserDetail.SherwinHomeAddress.ZipCode)], nameof(ProUserDto.ZipCodeOfStore))]
	public partial ProUserDto MapToProUserDTO(ProUserDetail user);

#pragma warning disable RMG020 // Source member is not mapped to any target member
	[MapNestedProperties(nameof(FlowUser.ProUser))]

	public partial BasicProResponseDto MapToBasicProDto(FlowUser user);
	public BasicProResponseDto MapToBasicProDto(FlowUser user, List<string>? zipCodes)
	{
		var dto = MapToBasicProDto(user);
		return dto with { ZipCodesPendingAMAssignment = zipCodes };
	}
#pragma warning restore RMG020 // Source member is not mapped to any target member

	[MapperIgnoreTarget(nameof(StateWithCounties.Counties))]
	public static ProZipCodeStateResponseDto MapToStateWithCounties(IEnumerable<ProUserToProZipcode> proUserToProZipcodes)
	{
		return new ProZipCodeStateResponseDto()
		{
			States = [.. proUserToProZipcodes
				.GroupBy(p => new { p.ProZipcode.StateAbbreviation, p.ProZipcode.State })
				.Select(stateGroup => new StateWithCounties
				{
					Abbreviation = stateGroup.Key.StateAbbreviation,
					Name = stateGroup.Key.State.StateName,
					Counties = [.. stateGroup
						.GroupBy(z => z.ProZipcode.County)
						.Select(countyGroup => new CountyWithProZipcode
						{
							Name = countyGroup.Key,
							ZipCodes = [.. countyGroup.Select(z => z.ProZipcode.Zipcode).Distinct()]
						})]
				})
				.GroupBy(s => s.Abbreviation)
				.Select(g => new StateWithCounties
				{
					Abbreviation = g.Key,
					Name = g.First().Name,
					Counties = [.. g
						.SelectMany(s => s.Counties.Cast<CountyWithProZipcode>())
						.GroupBy(c => c.Name)
						.Select(cg => new CountyWithProZipcode
						{
							Name = cg.Key,
							ZipCodes = [.. cg.SelectMany(c => (c.ZipCodes ?? []).Cast<string>()).Distinct()]
						})]
				})]
		};
	}

	public partial FileResponseDto MapToFile(FileItem source);

	public static ProUserFileDto MapToProUserFileDto(ProUserFile source)
	{
		return new ProUserFileDto(
			ProUserFileID: source.FileID,
			FileName: source.File.Name,
			FilePath: $"api/ProUsers/{source.UserID}/Files/{source.FileID}",
			ContentType: source.File.ContentType,
			ThumbnailFileName: source.ThumbnailFile?.Name ?? string.Empty,
			ThumbnailPath: source.ThumbnailFile != null ? $"api/ProUsers/{source.UserID}/Thumbnails/{source.ThumbnailFileID}" : string.Empty,
			ProUserFileType: source.ProFileType,
			FileID: source.FileID
		);
	}

	public static ProUserFileUploadResultDto MapToFileResult(ProUserFile source)
	{
		return new ProUserFileUploadResultDto
		{
			FileId = source.FileID,
			FileName = source.File.Name,
			FilePath = $"api/ProUsers/{source.UserID}/Files/{source.FileID}",
			ContentType = source.File.ContentType,
			ThumbnailFileId = source.ThumbnailFileID,
			ThumbnailFileName = source.ThumbnailFile?.Name ?? string.Empty,
			ProFileType = source.ProFileType,
			Message = "File uploaded successfully.",
			ThumbnailFilePath = source.ThumbnailFileID.HasValue
				? $"api/ProUsers/{source.UserID}/Thumbnails/{source.ThumbnailFileID.Value}"
				: string.Empty
		};
	}
}
