using CarPartsShop.API.Data;
using CarPartsShop.API.DTOs.Users;
using CarPartsShop.API.Models;
using CarPartsShop.API.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarPartsShop.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/customers/me/address")]
    public class CustomerAddressController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public CustomerAddressController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private async Task<Customer?> GetCurrentCustomer()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        }

        [HttpGet]
        public async Task<ActionResult<AddressDto>> Get()
        {
            var customer = await GetCurrentCustomer();
            if (customer == null) return NotFound(); 

            return Ok(new AddressDto
            {
                AddressLine1 = customer.AddressLine1,
                AddressLine2 = customer.AddressLine2,
                City = customer.City,
                State = customer.State,
                PostalCode = customer.PostalCode,
                Country = customer.Country,
                Phone = customer.Phone
            });
        }


        [HttpPut]
        public async Task<IActionResult> Update(AddressDto dto)
        {
            var customer = await GetCurrentCustomer();
            if (customer == null) return NotFound();

            customer.AddressLine1 = dto.AddressLine1;
            customer.AddressLine2 = dto.AddressLine2;
            customer.City = dto.City;
            customer.State = dto.State;
            customer.PostalCode = dto.PostalCode;
            customer.Country = dto.Country;
            customer.Phone = dto.Phone;

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

}
