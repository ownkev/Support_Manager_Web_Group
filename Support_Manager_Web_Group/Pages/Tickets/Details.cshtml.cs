using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Support_Manager_Web_Group.Data;
using Support_Manager_Web_Group.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Support_Manager_Web_Group.Pages.Tickets
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<DetailsModel> logger)
        { _context = context; _userManager = userManager; _logger = logger; }

        public Ticket Ticket { get; set; }
        public bool CanPerformActions { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) { _logger.LogWarning("Ticket Details requested without ID."); return NotFound(); }
            _logger.LogInformation($"Loading details for Ticket ID: {id}");

            try
            {
                Ticket = await _context.Tickets
                    .Include(t => t.Status).Include(t => t.Priority)
                    .Include(t => t.Submitter).Include(t => t.Assignee)
                    .FirstOrDefaultAsync(m => m.TicketID == id);

                if (Ticket == null) { _logger.LogWarning($"Ticket ID {id} not found."); return NotFound(); }

                var currentUserId = _userManager.GetUserId(User);
                bool isITStaff = User.IsInRole("IT Support") || User.IsInRole("IT Manager");
                if (!isITStaff && Ticket.SubmittedByUserID != currentUserId) { _logger.LogWarning($"User {currentUserId} forbidden from ticket {id}."); return Forbid(); }

                CanPerformActions = isITStaff && Ticket.StatusID != 6; // Can IT action non-closed tickets

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading ticket details for ID {id}");
                TempData["ErrorMessage"] = "Error loading ticket details.";
                return RedirectToPage("./Index");
            }
        }
    }
}
