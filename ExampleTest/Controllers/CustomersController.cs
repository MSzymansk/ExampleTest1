using ExampleTest.Models.DTOs;
using ExampleTest.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExampleTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController(ICustomerService _customerService) : ControllerBase
    {
        [HttpGet("{id}/rentals")]
        public async Task<IActionResult> GetRentals(int id)
        {
            if (id == null)
            {
                return BadRequest("id is null");
            }

            var customer = await _customerService.GetCustomerRentals(id);

            return Ok(customer);
        }

        [HttpPost("{id}/rentals")]
        public async Task<IActionResult> PostRentals(int id, [FromBody] RentalDTO rental)
        {
            if (!await _customerService.CheckCustomerExists(id))
            {
                return NotFound("customer doesn't exist");
            }

            try
            {
                await _customerService.PostRental(id, rental);
            }
            catch (Exception e)
            {
                return NotFound(e.Message);
            }
            
            return Ok("Rental posted");
        }
    }
}