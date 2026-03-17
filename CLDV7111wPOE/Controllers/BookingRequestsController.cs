using CLDV7111wPOE.Data;
using CLDV7111wPOE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CLDV7111wPOE.Controllers
{
    public class BookingRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BookingRequestsController> _logger;

        public BookingRequestsController(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BookingRequestsController> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private bool IsAuthenticated(out string role, out int userId)
        {
            role = _httpContextAccessor.HttpContext.Session.GetString("UserRole");
            userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
            return !string.IsNullOrEmpty(role) && userId > 0;
        }

        private bool IsAdminOrEmployee()
        {
            var role = _httpContextAccessor.HttpContext.Session.GetString("UserRole");
            return role == "Admin" || role == "Employee";
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAuthenticated(out string role, out int userId))
                return RedirectToAction("Login", "Account");

            if (role == "Customer")
            {
                var requests = await _context.BookingRequests
                    .Include(r => r.Venue)
                    .Where(r => r.CustomerId == userId)
                    .OrderByDescending(r => r.EventDate)
                    .ToListAsync();
                return View("CustomerRequests", requests);
            }
            else    
            {
                var requests = await _context.BookingRequests
                    .Include(r => r.Customer)
                    .Include(r => r.Venue)
                    .OrderBy(r => r.Status == "Pending" ? 0 : 1)
                    .ThenByDescending(r => r.EventDate)
                    .ToListAsync();
                return View("ManageRequests", requests);
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAuthenticated(out string role, out _))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var request = await _context.BookingRequests
                .Include(r => r.Customer)
                .Include(r => r.Venue)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null) return NotFound();

            if (role == "Customer" && request.CustomerId != _httpContextAccessor.HttpContext.Session.GetInt32("UserId"))
                return Forbid();

            return View(request);
        }

        public async Task<IActionResult> Create()
        {
            if (!IsAuthenticated(out string role, out int userId) || role != "Customer")
                return RedirectToAction("Login", "Account");

            ViewBag.Venues = await _context.Venues.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingRequest request, int venueId)
        {
            if (!IsAuthenticated(out string role, out int userId) || role != "Customer")
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                if (request.EventDate.Date < DateTime.Today)
                {
                    ModelState.AddModelError("EventDate", "Event date cannot be in the past.");
                    ViewBag.Venues = await _context.Venues.ToListAsync();
                    return View(request);
                }

                bool isBooked = await _context.BookingRequests
                    .AnyAsync(br => br.VenueId == venueId
                        && br.EventDate.Date == request.EventDate.Date
                        && br.Status == "Approved");

                if (isBooked)
                {
                    ModelState.AddModelError("", "This venue is already booked for the selected date.");
                    ViewBag.Venues = await _context.Venues.ToListAsync();
                    return View(request);
                }

                request.CustomerId = userId;
                request.VenueId = venueId;
                request.Status = "Pending";
                request.RequestDate = DateTime.Now;

                _context.Add(request);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New booking request created by user {userId} for venue {venueId} on {request.EventDate}");
                TempData["SuccessMessage"] = "Booking request submitted successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Venues = await _context.Venues.ToListAsync();
            return View(request);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdminOrEmployee())
                return Forbid();

            if (id == null) return NotFound();

            var request = await _context.BookingRequests
                .Include(r => r.Customer)
                .Include(r => r.Venue)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null) return NotFound();

            ViewBag.Venues = await _context.Venues.ToListAsync();
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BookingRequest request)
        {
            if (!IsAdminOrEmployee())
                return Forbid();

            if (id != request.RequestId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRequest = await _context.BookingRequests.FindAsync(id);
                    if (existingRequest == null) return NotFound();

                    if (existingRequest.EventDate.Date != request.EventDate.Date ||
                        existingRequest.VenueId != request.VenueId)
                    {
                        bool isBooked = await _context.BookingRequests
                            .AnyAsync(br => br.RequestId != id
                                && br.VenueId == request.VenueId
                                && br.EventDate.Date == request.EventDate.Date
                                && br.Status == "Approved");

                        if (isBooked && request.Status == "Approved")
                        {
                            ModelState.AddModelError("", "This venue is already booked for the selected date by another approved request.");
                            ViewBag.Venues = await _context.Venues.ToListAsync();
                            return View(request);
                        }
                    }

                    existingRequest.EventName = request.EventName;
                    existingRequest.EventDate = request.EventDate;
                    existingRequest.VenueId = request.VenueId;
                    existingRequest.Status = request.Status;

                    _context.Update(existingRequest);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Booking request {id} updated by {_httpContextAccessor.HttpContext.Session.GetString("UserName")}");
                    TempData["SuccessMessage"] = "Booking request updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingRequestExists(request.RequestId))
                        return NotFound();
                    else
                        throw;
                }
            }

            ViewBag.Venues = await _context.Venues.ToListAsync();
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, string newStatus)
        {
            if (!IsAdminOrEmployee())
                return Forbid();

            var request = await _context.BookingRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            if (newStatus != "Pending" && newStatus != "Approved" && newStatus != "Denied")
            {
                TempData["ErrorMessage"] = "Invalid status value.";
                return RedirectToAction(nameof(Index));
            }

            if (newStatus == "Approved")
            {
                bool isBooked = await _context.BookingRequests
                    .AnyAsync(br => br.RequestId != id
                        && br.VenueId == request.VenueId
                        && br.EventDate.Date == request.EventDate.Date
                        && br.Status == "Approved");

                if (isBooked)
                {
                    TempData["ErrorMessage"] = "Cannot approve: This venue is already booked for the selected date by another approved request.";
                    return RedirectToAction(nameof(Index));
                }
            }

            string oldStatus = request.Status;
            request.Status = newStatus;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Booking request {id} status changed from {oldStatus} to {newStatus} by {_httpContextAccessor.HttpContext.Session.GetString("UserName")}");
            TempData["SuccessMessage"] = $"Booking request status changed to {newStatus} successfully!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            return await ChangeStatus(id, "Approved");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int id)
        {
            return await ChangeStatus(id, "Denied");
        }

        public async Task<IActionResult> History()
        {
            if (!IsAdminOrEmployee())
                return Forbid();

            var requests = await _context.BookingRequests
                .Include(r => r.Customer)
                .Include(r => r.Venue)
                .OrderByDescending(r => r.RequestDate)
                .ThenByDescending(r => r.EventDate)
                .ToListAsync();

            return View(requests);
        }

        private bool BookingRequestExists(int id)
        {
            return _context.BookingRequests.Any(e => e.RequestId == id);
        }
    }
}