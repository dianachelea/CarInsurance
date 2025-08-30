using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var parsed))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");

        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("cars/{carId:long}/claims")]
    public async Task<IActionResult> CreateClaim(long carId, [FromBody] CreateClaimRequest request)
    {
        try
        {
            var claimId = await _service.CreateClaimAsync(carId, request);
            if (claimId is null) return BadRequest("Invalid payload.");
            var location = $"/api/cars/{carId}/claims/{claimId}";
            return Created(location, new { id = claimId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("cars/{carId:long}/history")]

    public async Task<ActionResult<List<CarHistory>>> GetHistory(long carId)
    {
        try
        {
            return Ok(await _service.GetHistory(carId));

        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
