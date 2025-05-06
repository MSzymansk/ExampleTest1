namespace ExampleTest.Models.DTOs;

public class CustomerDTO
{
    public string FirstName { get; set; }  
    public string LastName { get; set; }
    public List<RentalStatusDTO> Rentals { get; set; }
}

public class RentalStatusDTO
{
    public int RentalId { get; set; }
    public DateTime RentalDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; }
    public List<MovieDTO> Movies { get; set; }
}


