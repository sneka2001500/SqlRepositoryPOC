using Microsoft.AspNetCore.Mvc;
using SqlRepositoryPOC.Models;
using SqlRepositoryPOC.Repositories;

namespace SqlRepositoryPOC.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;

    public OrderController(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    // POST: api/Order/createOrder
    [HttpPost("createOrder")]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request)
    {
        try
        {
            if (!int.TryParse(request.Id, out int customerId))
            {
                return BadRequest(new
                {
                    msg = "Invalid customer id"
                });
            }

            var order = new Order
            {
                CustomerId = customerId,
                TotalPrice = (decimal)request.TotalPrice,
                PaymentMode = request.PaymentMode,
                PaymentId = request.PaymentId,
                Status = "Placed",
                DeliveryDate = DateTime.UtcNow.AddDays(5),
                ShippingAddress = request.ShippingAddress,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdOrder = await _orderRepository.CreateAsync(
                order,
                request.Products
            );

            return Ok(new
            {
                id = createdOrder.Id,
                user = createdOrder.CustomerId,
                products = request.Products,
                totalPrice = createdOrder.TotalPrice,
                status = createdOrder.Status,
                deliveryDate = createdOrder.DeliveryDate,
                shippingAddress = createdOrder.ShippingAddress,
                paymentMode = createdOrder.PaymentMode,
                paymentId = createdOrder.PaymentId,
                createdAt = createdOrder.CreatedAt,
                updatedAt = createdOrder.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Failed to create order",
                error = ex.Message
            });
        }
    }


    // GET: api/Order/getAllOrders
    [HttpGet("getAllOrders")]
    public async Task<IActionResult> GetAllOrders()
    {
        try
        {
            var orders = await _orderRepository.GetAllAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Failed to fetch orders",
                error = ex.Message
            });
        }
    }


    // GET: api/Order/getOrdersByEmailId/{email}
    [HttpGet("getOrdersByEmailId/{email}")]
    public async Task<IActionResult> GetOrdersByEmailId(string email)
    {
        try
        {
            var orders = await _orderRepository.GetByEmailAsync(email);

            return Ok(orders);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Failed to fetch orders by email",
                error = ex.Message
            });
        }
    }


    // POST: api/Order/updateOrder
    [HttpPost("updateOrder")]
    public async Task<IActionResult> UpdateOrder(
        [FromBody] UpdateOrderRequest request)
    {
        try
        {
            if (!int.TryParse(request.Id, out int orderId))
            {
                return BadRequest(new
                {
                    msg = "Invalid order id"
                });
            }

            var updatedOrder = await _orderRepository.UpdateAsync(
                orderId,
                request.Status,
                request.DeliveryDate
            );

            if (updatedOrder == null)
            {
                return Ok("Order failed");
            }

            return Ok(updatedOrder);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                msg = "Failed to update order",
                error = ex.Message
            });
        }
    }
}


// Request models

public class CreateOrderRequest
{
    public string Id { get; set; } = string.Empty;

    public List<OrderProduct> Products { get; set; } = new();

    public double TotalPrice { get; set; }

    public string PaymentMode { get; set; } = string.Empty;

    public string? PaymentId { get; set; }

    public string? ShippingAddress { get; set; }
}


public class UpdateOrderRequest
{
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime DeliveryDate { get; set; }
}