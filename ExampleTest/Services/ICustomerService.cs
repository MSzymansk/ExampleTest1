using ExampleTest.Models.DTOs;

namespace ExampleTest.Services;

public interface ICustomerService
{
    Task<CustomerDTO> GetCustomerRentals(int id);
    
    Task<bool> CheckCustomerExists(int id);
    Task PostRental(int id, RentalDTO rental);
}