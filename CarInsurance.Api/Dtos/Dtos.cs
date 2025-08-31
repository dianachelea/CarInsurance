namespace CarInsurance.Api.Dtos;

public record CarDto(long Id, string Vin, string? Make, string? Model, int Year, long OwnerId, string OwnerName, string? OwnerEmail);
public record InsuranceValidityResponse(long CarId, string Date, bool Valid);
public record CreateClaimRequest(string ClaimDate, string Description, decimal Amount);
public record CarHistory(string Type, string Start, string? End, string? Provider, string? Description, decimal? Amount);
public record CreatePolicyRequest(string StartDate, string EndDate, string? Provider);
public record CreateCarRequest(string Vin, string? Make, string? Model, int Year, long OwnerId);


