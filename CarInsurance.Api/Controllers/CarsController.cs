using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

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
        if (string.IsNullOrWhiteSpace(date))
            return BadRequest("Write a date. Format: YYYY-MM-DD.");

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsed))
            return BadRequest("Invalid date or date format(YYYY-MM-DD).");

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
    [HttpPost("cars/{carId:long}/policies")]
    public async Task<IActionResult> CreatePolicy(long carId, CreatePolicyRequest request)
    {
        if (request is null)
            return BadRequest("Body is required.");

        if (!DateOnly.TryParseExact(request.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var start))
            return BadRequest("Invalid StartDate. Format YYYY-MM-DD.");

        if (!DateOnly.TryParseExact(request.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var end))
            return BadRequest("Invalid EndDate. Format YYYY-MM-DD.");

        try
        {
            var id = await _service.CreatePolicyAsync(carId, start, end, request.Provider);
            if (id is null)
                return BadRequest("Could not create policy.");
            var location = $"/api/cars/{carId}/policies/{id}";
            return Created(location, new { id });
        }
        catch (ArgumentException ex)             
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)        
        {
            return Conflict(ex.Message);            
        }
    }

    [HttpPost("cars")]
    public async Task<IActionResult> CreateCar([FromBody] CreateCarRequest body)
    {
        if (body is null)
            return BadRequest("Body is required.");

        try
        {
            var id = await _service.CreateCar(body);
            var location = $"/api/cars/{id}";
            return Created(location, new { id });
        }
        catch (ArgumentException ex)          
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)   
        {
            return Conflict(ex.Message);      
        }
        catch (KeyNotFoundException)           
        {
            return NotFound();
        }
    }
}
