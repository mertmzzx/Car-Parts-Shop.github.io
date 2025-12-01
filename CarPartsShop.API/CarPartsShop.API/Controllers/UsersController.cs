using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CarPartsShop.API.Models.Identity;

namespace CarPartsShop.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<AppUser> _um;
        public UsersController(UserManager<AppUser> um) { _um = um; }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var email = User.Identity?.Name;
            var user = email != null ? await _um.FindByEmailAsync(email) : null;
            if (user == null) return NotFound();

            var roles = await _um.GetRolesAsync(user);
            return Ok(new
            {
                Email = user.Email,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? "",
                Roles = roles
            });
        }

        public class UpdateMeDto { public string? FirstName { get; set; } public string? LastName { get; set; } }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateMeDto dto)
        {
            var email = User.Identity?.Name;
            var user = email != null ? await _um.FindByEmailAsync(email) : null;
            if (user == null) return NotFound();

            user.FirstName = dto.FirstName?.Trim() ?? user.FirstName;
            user.LastName = dto.LastName?.Trim() ?? user.LastName;

            var result = await _um.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return NoContent();
        }

        public class ChangePasswordDto { public string CurrentPassword { get; set; } = ""; public string NewPassword { get; set; } = ""; }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var email = User.Identity?.Name;
            var user = email != null ? await _um.FindByEmailAsync(email) : null;
            if (user == null) return NotFound();

            var result = await _um.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return NoContent();
        }
    }

}
