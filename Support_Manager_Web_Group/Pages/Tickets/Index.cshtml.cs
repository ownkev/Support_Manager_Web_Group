using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Support_Manager_Web_Group.Data;
using Support_Manager_Web_Group.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Support_Manager_Web_Group.Pages.Tickets
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<IndexModel> logger)
        { _context = context; _userManager = userManager; _logger = logger; }

        public IList<Ticket> Tickets { get; set; } = new List<Ticket>();
        public bool IsITStaff { get; set; }
        public bool IsITManager { get; set; } // Added for Delete button logic

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return;

            IsITStaff = User.IsInRole("IT Support") || User.IsInRole("IT Manager");
            IsITManager = User.IsInRole("IT Manager"); // Check manager role specifically
            _logger.LogInformation($"Loading tickets for User {userId}, IsITStaff: {IsITStaff}, IsManager: {IsITManager}");

            try
            {
                IQueryable<Ticket> ticketsQuery = _context.Tickets
                                                    .Include(t => t.Status)
                                                    .Include(t => t.Priority)
                                                    .Include(t => t.Submitter)
                                                    .Include(t => t.Assignee);

                if (!IsITStaff) // Filter for Employees
                {
                    ticketsQuery = ticketsQuery.Where(t => t.SubmittedByUserID == userId);
                }
                // TODO: Add filtering later

                Tickets = await ticketsQuery.OrderByDescending(t => t.DateSubmitted).ToListAsync();
                _logger.LogInformation($"Loaded {Tickets.Count} tickets.");
            }
            catch (Exception ex) { _logger.LogError(ex, "Error retrieving tickets for User ID {UserId}", userId); TempData["ErrorMessage"] = "Could not load tickets."; }
        }

        // Delete Handler (Only IT Manager)
        [Authorize(Roles = "IT Manager")]
        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            if (id == null) return NotFound("Ticket ID missing.");

            var currentUserId = _userManager.GetUserId(User);
            _logger.LogWarning($"User {currentUserId} attempting deletion of TicketID: {id}");

            var ticketToDelete = await _context.Tickets.FindAsync(id);
            if (ticketToDelete == null) { TempData["ErrorMessage"] = $"Ticket #{id} not found."; return RedirectToPage("./Index"); }

            try
            {
                _context.Tickets.Remove(ticketToDelete);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Ticket ID {id} deleted by User: {currentUserId}");
                TempData["SuccessMessage"] = $"Ticket #{id} deleted successfully.";
                // TODO: Audit Log
            }
            catch (DbUpdateException ex) { _logger.LogError(ex, $"Error deleting TicketID: {id}"); TempData["ErrorMessage"] = $"Error deleting ticket #{id}. Check related data."; }
            catch (Exception ex) { _logger.LogError(ex, $"Unexpected error deleting TicketID: {id}"); TempData["ErrorMessage"] = "Unexpected error deleting ticket."; }

            return RedirectToPage("./Index");
        }
    }
}
