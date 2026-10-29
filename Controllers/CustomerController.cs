using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SqlRepositoryPOC.Models;
using SqlRepositoryPOC.Repositories;
using System.Text;
using System.Security.Cryptography;
namespace SqlRepositoryPOC.Controllers;
using SqlRepositoryPOC.Security;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IConfiguration _configuration;
    private readonly PasswordService _passwordService;

    private readonly CsrfTokenService _csrfTokenService;
private readonly IRefreshTokenRepository _refreshTokenRepository;
 public CustomerController(
    ICustomerRepository customerRepository,
    ICartRepository cartRepository,
    IConfiguration configuration,
    PasswordService passwordService,
    IRefreshTokenRepository refreshTokenRepository,
    CsrfTokenService csrfTokenService
    )
{
    _customerRepository = customerRepository;
    _cartRepository = cartRepository;
    _configuration = configuration;
    _passwordService = passwordService;
    _refreshTokenRepository = refreshTokenRepository;
    _csrfTokenService = csrfTokenService;
}
private string GenerateRefreshToken()
{
    return Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(64)
    );
}



[HttpGet("paged")]

public async Task<IActionResult> GetPagedCustomers(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? name = null,
    [FromQuery] string? email = null,
    [FromQuery] string? sortBy = "id",
    [FromQuery] string? sortDirection = "asc")
{
    if (pageNumber < 1)
    {
        return BadRequest(new
        {
            message = "Page number must be greater than 0."
        });
    }

    if (pageSize < 1 || pageSize > 100)
    {
        return BadRequest(new
        {
            message = "Page size must be between 1 and 100."
        });
    }

    if (!string.Equals(sortDirection, "asc",
            StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(sortDirection, "desc",
            StringComparison.OrdinalIgnoreCase))
    {
        return BadRequest(new
        {
            message = "Sort direction must be asc or desc."
        });
    }

    var allowedSortFields = new[]
    {
        "id", "name", "age", "email", "createdAt"
    };

    if (!allowedSortFields.Contains(
            sortBy ?? "id",
            StringComparer.OrdinalIgnoreCase))
    {
        return BadRequest(new
        {
            message = "Invalid sort field."
        });
    }

    var customers = await _customerRepository
        .GetFilteredAndSortedAsync(
            name,
            email,
            sortBy,
            sortDirection,
            pageNumber,
            pageSize);

    return Ok(new
    {
        pageNumber,
        pageSize,
        count = customers.Count,
        customers = customers.Select(customer => new
        {
            customer.Id,
            customer.Name,
            customer.Age,
            customer.Email,
            customer.Photo,
            customer.Address,
            customer.IsGuest,
            customer.Role,
            customer.CreatedAt,
            customer.UpdatedAt
        })
    });
}


    // GET CUSTOMER BY EMAIL
    [Authorize(Roles = "Admin")]
    [HttpPost("getCustomerByEmail")]
    public async Task<IActionResult> GetCustomerByEmail(
        [FromBody] GetCustomerByEmailRequest request)
    {
        var user = await _customerRepository
            .GetByEmailAsync(request.Email);

      
return Ok(new
{
    user = user == null ? null : new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Photo,
        user.IsGuest,
        user.CreatedAt,
        user.UpdatedAt
    }
});

    }

[Authorize]
[HttpGet("csrf-token")]
public IActionResult GetCsrfToken()
{
    var userId = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst(
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized();

    return Ok(new
    {
        csrfToken = _csrfTokenService.GenerateToken(userId)
    });
}
    // ME
    [HttpPost("me")]
    public async Task<IActionResult> Me(
        [FromBody] MeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
{
    return BadRequest(new
    {
        message = "Password is required"
    });
}
        
        try
        {
            var user = new Customer
            {
                Name = request.Name,
                Email = request.Email,
                Password = _passwordService.HashPassword(
    request.Email ?? string.Empty,
    request.Password ?? string.Empty),
                IsGuest = request.IsGuest,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user = await _customerRepository
                .CreateAsync(user);

            // Create empty cart for customer
            var cart = await _cartRepository
                .AddToCartAsync(
                    user.Id,
                    new List<CartProduct>());

            
return Ok(new
{
    user = new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Photo,
        user.IsGuest,
        user.CreatedAt,
        user.UpdatedAt
    },
    cart
});

        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error creating customer: {ex}");

            return StatusCode(500, new
            {
                msg = "Internal server error",
                error = ex.Message
            });
        }
    }


    // SIGNUP
    [HttpPost("signup")]
    public async Task<IActionResult> Signup(
        [FromBody] SignupRequest request)
    {

if (string.IsNullOrWhiteSpace(request.Password))
{
    return BadRequest(new
    {
        message = "Password is required"
    });
}
        var existingUser = await _customerRepository
            .GetByEmailAsync(request.Email ?? string.Empty);

        if (existingUser != null)
        {
            return Ok(new
            {
                status = "fails",
                message = "user is already register"
            });
        }

        try
        {
            var user = new Customer
            {
                Name = request.Name,
                Email = request.Email,
             Password = _passwordService.HashPassword(
    request.Email ?? string.Empty,
    request.Password ?? string.Empty),
                IsGuest = request.IsGuest,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow 
            };

            user = await _customerRepository
                .CreateAsync(user);

            // Create empty cart
            var cart = await _cartRepository
                .AddToCartAsync(
                    user.Id,
                    new List<CartProduct>());

           return Ok(new
{
    user = new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Photo,
        user.IsGuest,
        user.CreatedAt,
        user.UpdatedAt
    },
    cart
});
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error creating customer: {ex}");

            return StatusCode(500, new
            {
                msg = "Internal server error",
                error = ex.Message
            });
        }
    }


    // LOGIN
 // LOGIN
[EnableRateLimiting("LoginPolicy")]
[HttpPost("login")]
public async Task<IActionResult> Login(
    [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
    string.IsNullOrWhiteSpace(request.Password))
{
    return BadRequest(new
    {
        message = "Email and password are required"
    });
}
        var user = await _customerRepository
            .GetByEmailAsync(request.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Invalid credentials"
            });
        }

     
if (string.IsNullOrEmpty(user.Password))
{
    return Unauthorized(new { message = "Invalid credentials" });
}

bool passwordValid;

if (_passwordService.VerifyPassword(
        user.Email ?? string.Empty,
        user.Password,
        request.Password))
{
    passwordValid = true;
}
else if (user.Password == request.Password)
{
    // Legacy plaintext password: upgrade it to a hash.
    user.Password = _passwordService.HashPassword(
        user.Email ?? string.Empty,
        request.Password);

    await _customerRepository.UpdateAsync(user);
    passwordValid = true;
}
else
{
    passwordValid = false;
}

if (!passwordValid)
{
    return Unauthorized(new { message = "Invalid credentials" });
}


        
var claims = new[]
{
    new Claim(
        JwtRegisteredClaimNames.Sub,
        user.Id.ToString()),

    new Claim(
        JwtRegisteredClaimNames.Email,
        user.Email ?? string.Empty),

    new Claim(
        ClaimTypes.Name,
        user.Name ?? string.Empty),

    new Claim(
        ClaimTypes.Role,
        user.Role ?? "User")
};


        // JWT secret key 
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!
            )
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var expiryMinutes = int.Parse(
            _configuration["Jwt:ExpiryMinutes"] ?? "60"
        );

        // Create JWT
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                expiryMinutes),
            signingCredentials: credentials
        );

        var jwtToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

                // Generate refresh token
var refreshToken = GenerateRefreshToken();

var refreshTokenEntity = new RefreshToken
{
    CustomerId = user.Id,
    Token = refreshToken,
    ExpiresAt = DateTime.UtcNow.AddDays(7),
    CreatedAt = DateTime.UtcNow
};

await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        // Get user's cart
        var cart = await _cartRepository
            .GetByCustomerIdAsync(user.Id);

       return Ok(new
{
    accessToken = jwtToken,
    refreshToken = refreshToken,

    user = new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Photo,
        user.IsGuest,
        user.CreatedAt,
        user.UpdatedAt
    },

    cart
});
    }
[HttpPost("refreshToken")]
public async Task<IActionResult> RefreshToken(
    [FromBody] RefreshTokenRequest request)
{
    var refreshToken = await _refreshTokenRepository
        .GetByTokenAsync(request.RefreshToken);

    if (refreshToken == null)
    {
        return Unauthorized(new
        {
            message = "Invalid refresh token"
        });
    }

    if (refreshToken.RevokedAt != null)
    {
        return Unauthorized(new
        {
            message = "Refresh token has been revoked"
        });
    }

    if (refreshToken.ExpiresAt <= DateTime.UtcNow)
    {
        return Unauthorized(new
        {
            message = "Refresh token has expired"
        });
    }

    var user = await _customerRepository
        .GetByIdAsync(refreshToken.CustomerId);

    if (user == null)
    {
        return Unauthorized(new
        {
            message = "User not found"
        });
    }

    // Create new JWT
    var claims = new[]
    {
        new Claim(
            JwtRegisteredClaimNames.Sub,
            user.Id.ToString()),

        new Claim(
            JwtRegisteredClaimNames.Email,
            user.Email ?? string.Empty),

     new Claim(
    ClaimTypes.Name,
    user.Name ?? string.Empty),

new Claim(
    ClaimTypes.Role,
    user.Role ?? "User")
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"]!
        )
    );

    var credentials = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256
    );

    var expiryMinutes = int.Parse(
        _configuration["Jwt:ExpiryMinutes"] ?? "60"
    );

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
        signingCredentials: credentials
    );

    var newAccessToken =
        new JwtSecurityTokenHandler()
            .WriteToken(token);

    return Ok(new
    {
        accessToken = newAccessToken
    });
}


[Authorize]
[HttpPut("updateAccount")]
public async Task<IActionResult> UpdateAccount(
    [FromBody] UpdateAccountRequest request)
{
    if (request == null)
    {
        return BadRequest(new
        {
            message = "Invalid request"
        });
    }

    // 1. Get the authenticated user's ID
    var currentUserId =
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

    if (string.IsNullOrWhiteSpace(currentUserId))
    {
        return Unauthorized(new
        {
            message = "Invalid user identity"
        });
    }

    // 2. Validate the CSRF token
    var csrfToken = Request.Headers["X-CSRF-TOKEN"].FirstOrDefault();

    if (!_csrfTokenService.IsValid(csrfToken, currentUserId))
    {
        return BadRequest(new
        {
            message = "Invalid or missing CSRF token"
        });
    }

    // 3. Convert the authenticated user ID
    if (!int.TryParse(currentUserId, out int authenticatedUserId))
    {
        return Unauthorized(new
        {
            message = "Invalid user identity"
        });
    }

    // 4. Find the customer
    Customer? user = await _customerRepository
        .GetByIdAsync(authenticatedUserId);

    if (user == null)
    {
        return NotFound(new
        {
            message = "Customer not found"
        });
    }

    // 5. Update name
    user.Name = request.Name;

    // 6. Validate and update email
    if (!string.IsNullOrWhiteSpace(request.Email))
    {
        var existingEmail = await _customerRepository
            .GetByEmailAsync(request.Email);

        if (existingEmail != null && existingEmail.Id != user.Id)
        {
            return Conflict(new
            {
                message = "Email is already in use"
            });
        }

        user.Email = request.Email;
    }

    // 7. Hash the new password if supplied
   if (request.Password != null)
{
    if (!string.IsNullOrWhiteSpace(request.Password))
    {
        user.Password = _passwordService.HashPassword(
            user.Email ?? string.Empty,
            request.Password);
    }
}
    // 8. Update remaining customer fields
    user.Photo = request.Photo;
    user.IsGuest = request.IsGuest;
    user.UpdatedAt = DateTime.UtcNow;

    // 9. Save changes
    var updated = await _customerRepository.UpdateAsync(user);

    if (!updated)
    {
        return StatusCode(500, new
        {
            message = "Failed to update account"
        });
    }

    // 10. Get the customer's cart
    var cart = await _cartRepository
        .GetByCustomerIdAsync(user.Id);

    // 11. Return safe customer details
    return Ok(new
    {
        user = new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Photo,
            user.IsGuest,
            user.CreatedAt,
            user.UpdatedAt
        },
        cart
    });
}


[Authorize(Roles = "Admin")]
[HttpPost("transactionTest/{id}")]
public async Task<IActionResult> TransactionTest(int id)
{
    var result = await _customerRepository.TransactionTestAsync(id);

    if (!result)
    {
        return StatusCode(500, new
        {
            message = "Transaction failed and was rolled back"
        });
    }

    return Ok(new
    {
        message = "Transaction committed successfully"
    });
}

    // DELETE ONE CUSTOMER
    [Authorize(Roles = "Admin")]
    [HttpDelete("deleteOne")]
    public async Task<IActionResult> DeleteOne(
        [FromBody] DeleteCustomerRequest request)
    {
        var user = await _customerRepository
            .GetByEmailAsync(request.Email);

        if (user == null)
        {
            return Ok("failed deletion");
        }

        // Delete cart first because Cart has
        // a foreign key to Customer.
        var cartDeleted =
            await _cartRepository
                .DeleteByCustomerIdAsync(user.Id);

        var userDeleted =
            await _customerRepository
                .DeleteByEmailAsync(request.Email);

        if (userDeleted)
        {
            return Ok(new
            {
                user = 1,
                userCart = cartDeleted ? 1 : 0
            });
        }

        return Ok("failed deletion");
    }


    // DELETE ALL
    [Authorize(Roles = "Admin")]
    [HttpDelete("deleteAll")]
    public async Task<IActionResult> DeleteAll()
    {
        // Delete carts first because they reference customers.
        await _cartRepository.DeleteAllAsync();

        await _customerRepository.DeleteAllAsync();

        return Ok("deleted");
    }
}


// REQUEST MODELS

public class GetCustomerByEmailRequest
{
    public string Email { get; set; } = string.Empty;
}


public class MeRequest
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public bool IsGuest { get; set; } = true;
}


public class SignupRequest
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public bool IsGuest { get; set; } = false;
}


public class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}


public class UpdateAccountRequest
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Photo { get; set; }

    public bool IsGuest { get; set; }
}


public class DeleteCustomerRequest
{
    public string Email { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}