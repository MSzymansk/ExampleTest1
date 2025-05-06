using ExampleTest.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace ExampleTest.Services;

public class CustomerService(IConfiguration _configuration) : ICustomerService
{
    public async Task<CustomerDTO> GetCustomerRentals(int id)
    {
        var customer = new CustomerDTO()
        {
            Rentals = new List<RentalStatusDTO>()
        };
        var rentals = new Dictionary<int, RentalStatusDTO>();

        string query = @"
        SELECT C.first_name,
        C.last_name,
        R.rental_id,
        R.rental_date,
        R.return_date,
        S.name,
        RI.price_at_rental,
        M.title
        FROM Customer C
         INNER JOIN Rental R on C.customer_id = R.customer_id
         INNER JOIN Status S on R.status_id = S.status_id
         INNER JOIN Rental_Item RI on R.rental_id = RI.rental_id
         INNER JOIN Movie M on M.movie_id = RI.movie_id
        WHERE C.customer_id = @id;
        ";


        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        using (SqlCommand cmd = new SqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.IsNullOrEmpty(customer.FirstName))
                {
                    customer.FirstName = reader.GetString(reader.GetOrdinal("first_name"));
                }

                if (string.IsNullOrEmpty(customer.LastName))
                {
                    customer.LastName = reader.GetString(reader.GetOrdinal("last_name"));
                }

                int rentalId = reader.GetInt32(reader.GetOrdinal("rental_id"));

                if (!rentals.ContainsKey(rentalId))
                {
                    var rental = new RentalStatusDTO()
                    {
                        RentalId = rentalId,
                        RentalDate = reader.GetDateTime(reader.GetOrdinal("rental_date")),
                        ReturnDate = reader.IsDBNull(reader.GetOrdinal("return_date"))
                            ? (DateTime?)null
                            : reader.GetDateTime(reader.GetOrdinal("return_date")),
                        Status = reader.GetString(reader.GetOrdinal("name")),
                        Movies = new List<MovieDTO>()
                    };

                    rentals[rentalId] = rental;
                    customer.Rentals.Add(rental);
                }

                rentals[rentalId].Movies.Add(
                    new MovieDTO()
                    {
                        Title = reader.GetString(reader.GetOrdinal("title")),
                        PriceAtRental = reader.GetDecimal(reader.GetOrdinal("price_at_rental")),
                    }
                );
            }
        }

        return customer;
    }

    public async Task<bool> CheckCustomerExists(int id)
    {
        string command = @"SELECT COUNT(*) FROM Customer C WHERE C.customer_id = @id";

        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();
            int num = (int)await cmd.ExecuteScalarAsync();

            return num > 0;
        }
    }

    public async Task PostRental(int id, RentalDTO rental)
    {
        await using SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand cmd = new SqlCommand("", conn);

        await conn.OpenAsync();
        var transaction = conn.BeginTransaction();
        cmd.Transaction = transaction as SqlTransaction;

        try
        {
            cmd.CommandText = @"
            INSER INTO Rental(rental_id, rental_date, return_date, customer_id, status_i)
            VALUES (@rentalId, @rentalDate, @returnDate, @customerId, @statusId)
            ";
            cmd.Parameters.AddWithValue("@rentalId", rental.RentalId);
            cmd.Parameters.AddWithValue("@rentalDate", rental.RentalDate);
            cmd.Parameters.AddWithValue("@returnDate", DBNull.Value);
            cmd.Parameters.AddWithValue("@customerId", id);
            cmd.Parameters.AddWithValue("@statusId", 1);


            foreach (var movie in rental.Movies)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = @"SELECT movie_id FROM Movie M WHERE M.title = @title";
                cmd.Parameters.AddWithValue("@title", movie.Title);

                var result = await cmd.ExecuteScalarAsync();
                
                if (result == null || result == DBNull.Value)
                {
                    throw new Exception($"Movie with title '{movie.Title}' not found");
                }
                
                int movieId = Convert.ToInt32(result);
                cmd.Parameters.Clear();
                cmd.CommandText = @"
                     INSER INTO Rental_Item(rental_id, move_id, price_at_rental)
                    VALUES (@rentalId, @moveId, @priceAtRental)
                ";

                cmd.Parameters.AddWithValue("@rentalId", rental.RentalId);
                cmd.Parameters.AddWithValue("@moveId", movieId);
                cmd.Parameters.AddWithValue("@priceAtRental", movie.PriceAtRental);
            }


            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}