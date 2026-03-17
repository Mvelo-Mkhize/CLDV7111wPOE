using Microsoft.AspNetCore.Mvc;
using CLDV7111wPOE.Data;
using Microsoft.EntityFrameworkCore;

namespace CLDV7111wPOE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalVenues = await _context.Venues.CountAsync();
            ViewBag.TotalEvents = await _context.Events.CountAsync();

            var recentBookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .Include(b => b.User)
                .OrderByDescending(b => b.BookingDate)
                .Take(5)
                .ToListAsync();

            return View(recentBookings);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}