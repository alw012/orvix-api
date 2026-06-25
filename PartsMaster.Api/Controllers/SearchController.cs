using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartsMaster.Api.Data;

namespace PartsMaster.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{partNumber}")]
        public async Task<ActionResult> Search(string partNumber)
        {
            if (string.IsNullOrWhiteSpace(partNumber))
                return BadRequest(new { message = "أدخل رقم القطعة" });

            var searchTerm = partNumber.Trim().ToUpper();

            var results = await _context.Parts
                .Include(p => p.Store)
                .Where(p =>
                    p.Store!.IsActive &&
                    p.Quantity > 0 &&
                    p.PartNumber.ToUpper() == searchTerm
                )
                .Select(p => new
                {
                    p.Id,
                    p.PartNumber,
                    p.Type,
                    p.Quantity,
                    p.Price,
                    p.CompanyPrice,
                    Store = new
                    {
                        p.Store!.Id,
                        p.Store.Name,
                        p.Store.City,
                        p.Store.Phone,
                        p.Store.Phone2,
                        p.Store.Address,
                        p.Store.Location
                    }
                })
                .ToListAsync();

            if (!results.Any())
                return NotFound(new { message = "لا توجد نتائج لهذه القطعة" });

            var groupedByCity = results
                .GroupBy(r => r.Store.City)
                .Select(g => new
                {
                    City = g.Key,
                    TotalQuantity = g.Sum(r => r.Quantity),
                    Original = g.Where(r => r.Type == "أصلي" || string.IsNullOrEmpty(r.Type)).ToList(),
                    Commercial = g.Where(r => r.Type == "تجاري").ToList()
                })
                .OrderBy(g => g.City)
                .ToList();

            return Ok(new
            {
                PartNumber = searchTerm,
                TotalResults = results.Count,
                Cities = groupedByCity
            });
        }

        [HttpGet("{partNumber}/city/{city}")]
        public async Task<ActionResult> SearchByCity(string partNumber, string city)
        {
            var searchTerm = partNumber.Trim().ToUpper();

            var results = await _context.Parts
                .Include(p => p.Store)
                .Where(p =>
                    p.Store!.IsActive &&
                    p.Store.City == city &&
                    p.Quantity > 0 &&
                    p.PartNumber.ToUpper() == searchTerm
                )
                .Select(p => new
                {
                    p.Id,
                    p.PartNumber,
                    p.Type,
                    p.Quantity,
                    p.Price,
                    p.CompanyPrice,
                    Store = new
                    {
                        p.Store!.Id,
                        p.Store.Name,
                        p.Store.City,
                        p.Store.Phone,
                        p.Store.Phone2,
                        p.Store.Address,
                        p.Store.Location
                    }
                })
                .ToListAsync();

            if (!results.Any())
                return NotFound(new { message = $"لا توجد نتائج في {city}" });

            return Ok(new
            {
                PartNumber = searchTerm,
                City = city,
                Original = results.Where(r => r.Type == "أصلي" || string.IsNullOrEmpty(r.Type)).ToList(),
                Commercial = results.Where(r => r.Type == "تجاري").ToList()
            });
        }
    }
}
