using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Support_Manager_Web_Group.Data;
using Support_Manager_Web_Group.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Support_Manager_Web_Group.Pages.Tickets
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<CreateModel> logger)
        { _context = context; _userManager = userManager; _logger = logger; }

        [BindProperty]
        public Ticket Ticket { get; set; } = new Ticket();

        public SelectList PriorityList { get; set; }
        public SelectList CategoryList { get; set; }

        public async Task OnGetAsync()
        {
            await PopulateDropdownsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            ModelState.Remove("Ticket.Submitter"); ModelState.Remove("Ticket.Assignee");
            ModelState.Remove("Ticket.Status"); ModelState.Remove("Ticket.Priority");
            ModelState.Remove("Ticket.SubmittedByUserID");

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(); return Page();
            }

            Ticket.SubmittedByUserID = userId;
            // Defaults set in constructor

            _context.Tickets.Add(Ticket);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ticket #{Ticket.TicketID} created successfully!";
                // TODO: Audit Log
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving ticket for User ID {UserId}.", userId);
                ModelState.AddModelError(string.Empty, "Unable to save ticket.");
                await PopulateDropdownsAsync(); return Page();
            }
        }

        private async Task PopulateDropdownsAsync()
        {
            try
            {
                var priorities = await _context.TicketPriorities.OrderBy(p => p.PriorityID).Select(p => new { p.PriorityID, p.PriorityName }).ToListAsync();
                PriorityList = new SelectList(priorities, nameof(TicketPriority.PriorityID), nameof(TicketPriority.PriorityName), Ticket?.PriorityID ?? 2);
                var categories = new List<string> { "Hardware", "Software", "Network", "Account Request", "Other" };
                CategoryList = new SelectList(categories, Ticket?.Category);
            }
            catch (Exception ex) { _logger.LogError(ex, "Failed to populate dropdowns."); /* Set empty lists */ }
        }
    }
}
