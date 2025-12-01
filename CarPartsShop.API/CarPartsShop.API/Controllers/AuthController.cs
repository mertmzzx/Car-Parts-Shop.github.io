using CarPartsShop.API.Auth;
using CarPartsShop.API.Data;
using CarPartsShop.API.DTOs.Auth;
using CarPartsShop.API.Models;
using CarPartsShop.API.Models.Identity;
using CarPartsShop.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarPartsShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly JwtTokenService _tokenService;
        private readonly AppDbContext _db;

        public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, JwtTokenService tokenService, AppDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _db = db;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!string.IsNullOrWhiteSpace(dto.ConfirmPassword) &&
                !string.Equals(dto.Password, dto.ConfirmPassword))
            {
                ModelState.AddModelError(nameof(dto.ConfirmPassword), "Passwords do not match.");
                return ValidationProblem(ModelState);
            }

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest("Email is already in use.");

            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, Roles.Customer);

            var customer = new Customer
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? ""
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            // Issue token 
            var token = await _tokenService.CreateTokenAsync(user);
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? Roles.Customer;

            return Ok(new AuthResponseDto
            {
                Token = token,
                Role = role,
                UserName = user.UserName!,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? ""
            });
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return Unauthorized("Invalid email or password.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
                return Unauthorized("Invalid email or password.");

            var token = await _tokenService.CreateTokenAsync(user);
            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";

            return Ok(new AuthResponseDto
            {
                Token = token,
                Role = role,
                UserName = user.UserName!,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
            });
        }
    }
}
