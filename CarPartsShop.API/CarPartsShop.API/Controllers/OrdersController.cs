using CarPartsShop.API.Auth;
using CarPartsShop.API.Data;
using CarPartsShop.API.DTOs.Orders;
using CarPartsShop.API.Enums;
using CarPartsShop.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace CarPartsShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private const decimal TAX_RATE = 0.20m; 

        public OrdersController(AppDbContext db) => _db = db;

        // --- helpers 
        private async Task<IActionResult> CancelOrderCore(Order order)
        {
            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                return BadRequest("This order has already been shipped/delivered and cannot be cancelled.");

            if (order.Status == OrderStatus.Cancelled)
                return NoContent();

            // Restock items
            var partIds = order.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = await _db.Parts.Where(p => partIds.Contains(p.Id))
                                       .ToDictionaryAsync(p => p.Id);

            foreach (var item in order.Items)
                if (parts.TryGetValue(item.PartId, out var part))
                    part.QuantityInStock += item.Quantity;

            // Update status + history
            order.Status = OrderStatus.Cancelled;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = OrderStatus.Cancelled,
                ChangedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await LogAdminAction($"Cancelled order #{order.Id}");
            return NoContent();
        }
        private static (string name, string email, string phone, string address) MapCustomer(Customer? c)
        {
            if (c == null) return ("-", "-", "-", "-");

            var name = string.Join(" ", new[] { c.FirstName, c.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var address = string.Join(" • ", new[]
            {
                c.AddressLine1,
                c.AddressLine2,
                string.Join(", ", new[] { c.City, c.State }.Where(s => !string.IsNullOrWhiteSpace(s))),
                c.PostalCode,
                c.Country
            }  .Where(s => !string.IsNullOrWhiteSpace(s)));

            return (
                string.IsNullOrWhiteSpace(name) ? "-" : name,
                string.IsNullOrWhiteSpace(c.Email) ? "-" : c.Email!,
                string.IsNullOrWhiteSpace(c.Phone) ? "-" : c.Phone!,
                string.IsNullOrWhiteSpace(address) ? "-" : address
            );
        }


            [HttpGet("my")]
            [Authorize(Roles = Roles.Customer)]
            [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetMyOrders()
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var customer = await _db.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return NotFound("Customer profile not found.");

                var orders = await _db.Orders
                    .AsNoTracking()
                    .Where(o => o.CustomerId == customer.Id)
                    .Include(o => o.Customer)
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                var partIds = orders.SelectMany(o => o.Items.Select(i => i.PartId)).Distinct().ToList();
                var parts = await _db.Parts
                    .Where(p => partIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                var result = orders.Select(order =>
                {
                    var (name, email, phone, addr) = MapCustomer(order.Customer);

                    return new OrderResponseDto
                    {
                        Id = order.Id,
                        CustomerId = order.CustomerId,
                        CreatedAt = order.CreatedAt,
                        Subtotal = order.Subtotal,
                        Tax = order.Tax,
                        Total = order.Total,
                        Status = order.Status.ToString(),
                        Items = order.Items.Select(oi => new OrderItemResponseDto
                        {
                            PartId = oi.PartId,
                            PartName = parts[oi.PartId].Name,
                            Sku = parts[oi.PartId].Sku,
                            UnitPrice = oi.UnitPrice,
                            Quantity = oi.Quantity,
                            LineTotal = oi.UnitPrice * oi.Quantity
                        }).ToList(),

                        CustomerName = name,
                        CustomerEmail = email,
                        CustomerPhone = phone,
                        DeliveryAddress = addr
                    };
                });

                return Ok(result);
            }

        [HttpGet("recent")]
        [Authorize(Roles = $"{Roles.Administrator},{Roles.SalesAssistant}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetRecentOrders([FromQuery] int limit = 10)
        {
            var orders = await _db.Orders
                .Include(o => o.Items)
                .Include(o => o.Customer)
                .OrderByDescending(o => o.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var partIds = orders.SelectMany(o => o.Items.Select(i => i.PartId)).Distinct().ToList();
            var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            var result = orders.Select(order =>
            {
                var (name, email, phone, addr) = MapCustomer(order.Customer);

                return new OrderResponseDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    CreatedAt = order.CreatedAt,
                    Subtotal = order.Subtotal,
                    Tax = order.Tax,
                    Total = order.Total,
                    Status = order.Status.ToString(),
                    Items = order.Items.Select(oi => new OrderItemResponseDto
                    {
                        PartId = oi.PartId,
                        PartName = parts[oi.PartId].Name,
                        Sku = parts[oi.PartId].Sku,
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity,
                        LineTotal = oi.UnitPrice * oi.Quantity
                    }).ToList(),

                    CustomerName = name,
                    CustomerEmail = email,
                    CustomerPhone = phone,
                    DeliveryAddress = addr
                };
            });


            return Ok(result);
        }


        [HttpPost]
        [Authorize(Roles = Roles.Customer)] 
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest("Order must contain at least one item.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return NotFound("Customer profile not found.");

            var partIds = dto.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            foreach (var item in dto.Items)
            {
                if (!parts.ContainsKey(item.PartId))
                    return NotFound($"Part {item.PartId} not found.");
                if (item.Quantity <= 0)
                    return BadRequest("Quantity must be positive.");
                if (parts[item.PartId].QuantityInStock < item.Quantity)
                    return BadRequest($"Not enough stock for PartId {item.PartId}. Requested {item.Quantity}, available {parts[item.PartId].QuantityInStock}.");
            }

            decimal subtotal = 0m;
            var orderItems = new List<OrderItem>();
            foreach (var item in dto.Items)
            {
                var part = parts[item.PartId];
                subtotal += part.Price * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    PartId = part.Id,
                    Quantity = item.Quantity,
                    UnitPrice = part.Price
                });

                part.QuantityInStock -= item.Quantity; 
            }

            var tax = Math.Round(subtotal * TAX_RATE, 2, MidpointRounding.AwayFromZero);
            var total = subtotal + tax;
            var pm = (dto.PaymentMethod ?? "Cash").Trim();
            if (!string.Equals(pm, "Cash", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Unsupported payment method.");

            // shipping snapshot
            string? shipFirst = null, shipLast = null, shipLine1 = null, shipLine2 = null,
                    shipCity = null, shipState = null, shipPostal = null, shipCountry = null, shipPhone = null;

            if (dto.UseSavedAddress)
            {
                var hasSaved =
                    !string.IsNullOrWhiteSpace(customer.AddressLine1) ||
                    !string.IsNullOrWhiteSpace(customer.City) ||
                    !string.IsNullOrWhiteSpace(customer.PostalCode) ||
                    !string.IsNullOrWhiteSpace(customer.Country);

                if (!hasSaved)
                    return BadRequest("No saved address on file.");

                shipFirst = customer.FirstName;
                shipLast = customer.LastName;
                shipLine1 = customer.AddressLine1;
                shipLine2 = customer.AddressLine2;
                shipCity = customer.City;
                shipState = customer.State;
                shipPostal = customer.PostalCode;
                shipCountry = customer.Country;
                shipPhone = customer.Phone;
            }
            else
            {
                var a = dto.ShippingAddressOverride;
                if (a is null)
                    return BadRequest("Shipping address is required.");

                if (string.IsNullOrWhiteSpace(a.AddressLine1) ||
                    string.IsNullOrWhiteSpace(a.City) ||
                    string.IsNullOrWhiteSpace(a.PostalCode) ||
                    string.IsNullOrWhiteSpace(a.Country))
                    return BadRequest("AddressLine1, City, PostalCode and Country are required.");

                shipFirst = string.IsNullOrWhiteSpace(a.FirstName) ? customer.FirstName : a.FirstName;
                shipLast = string.IsNullOrWhiteSpace(a.LastName) ? customer.LastName : a.LastName;
                shipLine1 = a.AddressLine1;
                shipLine2 = a.AddressLine2;
                shipCity = a.City;
                shipState = a.State;
                shipPostal = a.PostalCode;
                shipCountry = a.Country;
                shipPhone = string.IsNullOrWhiteSpace(a.Phone) ? customer.Phone : a.Phone;
            }

            var order = new Order
            {
                CustomerId = customer.Id,
                CreatedAt = DateTime.UtcNow,
                Subtotal = subtotal,
                Tax = tax,
                Total = total,
                Status = OrderStatus.Pending,
                Items = orderItems,
                StatusHistory = new List<OrderStatusHistory>
                {
                    new OrderStatusHistory { Status = OrderStatus.Pending, ChangedAt = DateTime.UtcNow }
                },

                // snapshot fields 
                ShipFirstName = shipFirst,
                ShipLastName = shipLast,
                ShipAddressLine1 = shipLine1,
                ShipAddressLine2 = shipLine2,
                ShipCity = shipCity,
                ShipState = shipState,
                ShipPostalCode = shipPostal,
                ShipCountry = shipCountry,
                ShipPhone = shipPhone,
                ShippingMethod = dto.ShippingMethod,
                PaymentMethod = pm
            };

            order.Customer = customer;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            // display snapshot
            string SnapshotToString()
            {
                var segments = new[]
                {
            $"{order.ShipFirstName} {order.ShipLastName}".Trim(),
            order.ShipAddressLine1,
            order.ShipAddressLine2,
            string.Join(", ", new[] { order.ShipCity, order.ShipState }.Where(s => !string.IsNullOrWhiteSpace(s))),
            order.ShipPostalCode,
            order.ShipCountry
        }.Where(s => !string.IsNullOrWhiteSpace(s));
                return string.Join(" • ", segments);
            }

            var response = new OrderResponseDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CreatedAt = order.CreatedAt,
                Subtotal = order.Subtotal,
                Tax = order.Tax,
                Total = order.Total,
                Status = order.Status.ToString(),
                Items = order.Items.Select(oi => new OrderItemResponseDto
                {
                    PartId = oi.PartId,
                    PartName = parts[oi.PartId].Name,
                    Sku = parts[oi.PartId].Sku,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    LineTotal = oi.UnitPrice * oi.Quantity
                }).ToList(),
                CustomerName = string.Join(" ", new[] { order.ShipFirstName, order.ShipLastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                CustomerEmail = customer.Email!,
                CustomerPhone = order.ShipPhone ?? customer.Phone ?? "-",
                DeliveryAddress = SnapshotToString(),
                PaymentMethod = order.PaymentMethod
            };

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, response);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{Roles.Administrator},{Roles.SalesAssistant},{Roles.Customer}")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (User.IsInRole(Roles.Customer))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || order.Customer?.UserId != userId)
                    return Forbid();
            }

            return await CancelOrderCore(order);
        }


        [HttpGet("{id:int}")]
        [Authorize(Roles = $"{Roles.Administrator},{Roles.SalesAssistant},{Roles.Customer}")]
        [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id, [FromQuery] bool includeHistory = false)
        {
            IQueryable<Order> query = _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items); 

            if (includeHistory)
                query = query.Include(o => o.StatusHistory);

            var order = await query.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (User.IsInRole(Roles.Customer))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || order.Customer?.UserId != userId)
                    return Forbid()
            }

            var partIds = order.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            var (name, email, phone, addr) = MapCustomer(order.Customer);

            var dto = new OrderResponseDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CreatedAt = order.CreatedAt,
                Subtotal = order.Subtotal,
                Tax = order.Tax,
                Total = order.Total,
                Status = order.Status.ToString(),
                Items = order.Items.Select(oi => new OrderItemResponseDto
                {
                    PartId = oi.PartId,
                    PartName = parts[oi.PartId].Name,
                    Sku = parts[oi.PartId].Sku,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    LineTotal = oi.UnitPrice * oi.Quantity
                }).ToList(),
                StatusHistory = includeHistory
                    ? order.StatusHistory.OrderBy(h => h.ChangedAt).Select(h => new OrderStatusHistoryDto
                    {
                        Status = h.Status.ToString(),
                        ChangedAt = h.ChangedAt
                    }).ToList()
                    : null,
                CustomerName = name,
                CustomerEmail = email,
                CustomerPhone = phone,
                DeliveryAddress = addr
            };

            return Ok(dto);
        }




        [HttpGet("/api/customers/{customerId:int}/orders")]
        [Authorize(Roles = $"{Roles.Administrator},{Roles.SalesAssistant}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrdersForCustomer(int customerId)
        {
            var orders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var partIds = orders.SelectMany(o => o.Items.Select(i => i.PartId)).Distinct().ToList();
            var parts = await _db.Parts.Where(p => partIds.Contains(p.Id))
                                       .ToDictionaryAsync(p => p.Id);

            var result = orders.Select(order =>
            {
                var (name, email, phone, addr) = MapCustomer(order.Customer);

                return new OrderResponseDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    CreatedAt = order.CreatedAt,
                    Subtotal = order.Subtotal,
                    Tax = order.Tax,
                    Total = order.Total,
                    Status = order.Status.ToString(),
                    Items = order.Items.Select(oi => new OrderItemResponseDto
                    {
                        PartId = oi.PartId,
                        PartName = parts[oi.PartId].Name,
                        Sku = parts[oi.PartId].Sku,
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity,
                        LineTotal = oi.UnitPrice * oi.Quantity
                    }).ToList(),

                    CustomerName = name,
                    CustomerEmail = email,
                    CustomerPhone = phone,
                    DeliveryAddress = addr
                };
            });


            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = $"{Roles.Administrator},{Roles.SalesAssistant}")]
        [ProducesResponseType(typeof(IEnumerable<OrderResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAllOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? customerName = null,
            [FromQuery] int? customerId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var query = _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(customerName))
                query = query.Where(o => o.Customer.FirstName.Contains(customerName) || o.Customer.LastName.Contains(customerName));

            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId);

            if (from.HasValue)
                query = query.Where(o => o.CreatedAt >= from);

            if (to.HasValue)
                query = query.Where(o => o.CreatedAt <= to);

            var total = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var partIds = orders.SelectMany(o => o.Items.Select(i => i.PartId)).Distinct();
            var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            var results = orders.Select(order =>
            {
                var (name, email, phone, addr) = MapCustomer(order.Customer);

                return new OrderResponseDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    CreatedAt = order.CreatedAt,
                    Subtotal = order.Subtotal,
                    Tax = order.Tax,
                    Total = order.Total,
                    Status = order.Status.ToString(),
                    Items = order.Items.Select(oi => new OrderItemResponseDto
                    {
                        PartId = oi.PartId,
                        PartName = parts[oi.PartId].Name,
                        Sku = parts[oi.PartId].Sku,
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity,
                        LineTotal = oi.UnitPrice * oi.Quantity
                    }).ToList(),

                    CustomerName = name,
                    CustomerEmail = email,
                    CustomerPhone = phone,
                    DeliveryAddress = addr
                };
            });

            Response.Headers.Add("X-Total-Count", total.ToString());

            return Ok(results);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = $"{Roles.Administrator},{Roles.SalesAssistant}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
                return BadRequest("Invalid status value.");

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (order.Status == newStatus) return NoContent();

            if (newStatus == OrderStatus.Cancelled)
                return await CancelOrderCore(order);

            if (order.Status == OrderStatus.Cancelled)
                return BadRequest("A cancelled order cannot change status.");

            if (order.Status == OrderStatus.Delivered && newStatus != OrderStatus.Delivered)
                return BadRequest("Delivered orders cannot change status.");

            order.Status = newStatus;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = newStatus,
                ChangedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await LogAdminAction($"Changed order #{order.Id} status to {newStatus}");

            return NoContent();
        }

        private async Task LogAdminAction(string action)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userEmail = User.Identity?.Name ?? "unknown";

            _db.AdminLogs.Add(new AdminLog
            {
                Timestamp = DateTime.UtcNow,
                PerformedById = userId,
                PerformedByEmail = userEmail,
                Action = action
            });

            await _db.SaveChangesAsync();
        }
    }
}
