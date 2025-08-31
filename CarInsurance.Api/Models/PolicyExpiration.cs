namespace CarInsurance.Api.Models
{
    public class PolicyExpiration
    {
        public long Id { get; set; }
        public long PolicyId { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public InsurancePolicy Policy { get; set; } = default!;
    }
}
