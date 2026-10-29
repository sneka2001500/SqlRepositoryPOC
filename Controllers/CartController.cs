using Microsoft.AspNetCore.Mvc;
using SqlRepositoryPOC.Models;
using SqlRepositoryPOC.Repositories;

namespace SqlRepositoryPOC.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartRepository _cartRepository;

    public CartController(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    // GET: api/Cart/getAllCarts
    [HttpGet("getAllCarts")]
    public async Task<IActionResult> GetAllCarts()
    {
        try
        {
            var carts = await _cartRepository.GetAllAsync();

            return Ok(new
            {
                data = carts
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Error getting carts",
                error = ex.Message
            });
        }
    }

    // POST: api/Cart/getCart
    [HttpPost("getCart")]
    public async Task<IActionResult> GetCart(
        [FromBody] GetCartRequest request)
    {
        try
        {
            var cart = await _cartRepository
                .GetByCustomerIdAsync(request.CustomerId);

            if (cart == null)
            {
                return Ok(new
                {
                    data = (Cart?)null
                });
            }

            return Ok(new
            {
                data = cart
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Error getting cart",
                error = ex.Message
            });
        }
    }

    // POST: api/Cart/addToCart
    [HttpPost("addToCart")]
    public async Task<IActionResult> AddToCart(
        [FromBody] AddToCartRequest request)
    {
        try
        {
            var cart = await _cartRepository.AddToCartAsync(
                request.CustomerId,
                request.Products
            );

            return Ok(new
            {
                msg = "Product added to cart successfully",
                data = cart
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Error adding product to cart",
                error = ex.Message
            });
        }
    }

    // POST: api/Cart/removeFromCart
    [HttpPost("removeFromCart")]
    public async Task<IActionResult> RemoveFromCart(
        [FromBody] RemoveFromCartRequest request)
    {
        try
        {
            var cart = await _cartRepository.RemoveFromCartAsync(
                request.CustomerId,
                request.ProductId
            );

            if (cart == null)
            {
                return NotFound(new
                {
                    msg = "Cart not found"
                });
            }

            return Ok(new
            {
                msg = "Product removed from cart successfully",
                data = cart
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Error removing product from cart",
                error = ex.Message
            });
        }
    }

    // POST: api/Cart/clearCart
    [HttpPost("clearCart")]
    public async Task<IActionResult> ClearCart(
        [FromBody] ClearCartRequest request)
    {
        try
        {
            var result = await _cartRepository
                .ClearCartAsync(request.CustomerId);

            if (!result)
            {
                return NotFound(new
                {
                    msg = "Cart not found"
                });
            }

            return Ok(new
            {
                msg = "Cart cleared successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Error clearing cart",
                error = ex.Message
            });
        }
    }
}


// Request models

public class GetCartRequest
{
    public int CustomerId { get; set; }
}


public class AddToCartRequest
{
    public int CustomerId { get; set; }

    public List<CartProduct> Products { get; set; } = new();
}


public class RemoveFromCartRequest
{
    public int CustomerId { get; set; }

    public int ProductId { get; set; }
}


public class ClearCartRequest
{
    public int CustomerId { get; set; }
}