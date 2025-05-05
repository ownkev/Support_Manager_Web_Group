using Microsoft.AspNetCore.Mvc;
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
using System;
using System.ComponentModel.DataAnnotations; // For NewCommentText validation

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
        public List<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public bool CanPerformActions { get; set; }
        public bool CanComment { get; set; } // Can current user add comment?

        // Property for the new comment form
        [BindProperty]
        [Required(ErrorMessage = "Comment text cannot be empty.")]
        [Display(Name = "New Comment")]
        public string NewCommentText { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) { _logger.LogWarning("Ticket Details requested without ID."); return NotFound(); }
            _logger.LogInformation($"Loading details for Ticket ID: {id}");

            try
            {
                Ticket = await _context.Tickets
                    .Include(t => t.Status).Include(t => t.Priority)
                    .Include(t => t.Submitter).Include(t => t.Assignee)
                    // Eager load Comments AND the User who wrote each comment
                    .Include(t => t.Comments).ThenInclude(c => c.User)
                    .AsNoTracking() // Use AsNoTracking for read-only main ticket data
                    .FirstOrDefaultAsync(m => m.TicketID == id);

                if (Ticket == null) { _logger.LogWarning($"Ticket ID {id} not found."); return NotFound(); }

                // Assign comments to the separate property, ordered by date
                Comments = Ticket.Comments.OrderBy(c => c.DateCommented).ToList();

                // Authorization Check
                var currentUserId = _userManager.GetUserId(User);
                bool isITStaff = User.IsInRole("IT Support") || User.IsInRole("IT Manager");
                if (!isITStaff && Ticket.SubmittedByUserID != currentUserId) { _logger.LogWarning($"User {currentUserId} forbidden from ticket {id}."); return Forbid(); }

                // Determine Permissions
                CanPerformActions = isITStaff && Ticket.StatusID != 6; // Can IT action non-closed tickets
                CanComment = (isITStaff || Ticket.SubmittedByUserID == currentUserId) && Ticket.StatusID != 6; // IT or Submitter can comment if not closed

                _logger.LogInformation($"Loaded ticket {id}. Comment Count: {Comments.Count}. CanPerformActions: {CanPerformActions}. CanComment: {CanComment}");
                return Page();
            }
            catch (Exception ex) { _logger.LogError(ex, $"Error loading ticket details for ID {id}"); TempData["ErrorMessage"] = "Error loading ticket details."; return RedirectToPage("./Index"); }
        }

        // Handler for adding a new comment
        public async Task<IActionResult> OnPostAddCommentAsync(int? id)
        {
            if (id == null) { return NotFound("Ticket ID missing for comment."); }

            var userId = _userManager.GetUserId(User);
            if (userId == null) { return Challenge(); } // Not logged in

            // Fetch the ticket again to verify existence and status before adding comment
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) { return NotFound($"Ticket {id} not found when adding comment."); }

            // Re-verify permission to comment (in case state changed)
            bool isITStaff = User.IsInRole("IT Support") || User.IsInRole("IT Manager");
            if (ticket.StatusID == 6 || (!isITStaff && ticket.SubmittedByUserID != userId))
            {
                _logger.LogWarning($"User {userId} attempted to comment on ticket {id} without permission or while closed.");
                TempData["ErrorMessage"] = "You do not have permission to comment on this ticket or it is closed.";
                return RedirectToPage("./Details", new { id = id });
            }

            // Check only comment validation (Title/Desc etc. are not part of this post)
            if (string.IsNullOrWhiteSpace(NewCommentText))
            {
                // Need to reload data for page display if validation fails
                await ReloadDataForPageAsync(id.Value); // Reload Ticket and Comments
                ModelState.AddModelError(nameof(NewCommentText), "Comment text cannot be empty.");
                return Page();
            }


            var comment = new TicketComment
            {
                TicketID = id.Value,
                UserID = userId,
                CommentText = NewCommentText.Trim(), // Trim whitespace
                DateCommented = DateTime.Now
            };

            _context.TicketComments.Add(comment);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Comment added to Ticket {id} by User {userId}");
                TempData["SuccessMessage"] = "Comment added successfully.";
                // TODO: Audit Log for comment

                return RedirectToPage("./Details", new { id = id }); // Redirect back to details page to see new comment
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving comment for Ticket {id} by User {userId}");
                ModelState.AddModelError(string.Empty, "An error occurred while saving your comment.");
                await ReloadDataForPageAsync(id.Value); // Reload data before showing page again
                return Page();
            }
        }

        // Helper to reload data needed for the page display (used in OnGet and after POST errors)
        private async Task ReloadDataForPageAsync(int ticketId)
        {
            Ticket = await _context.Tickets
                .Include(t => t.Status).Include(t => t.Priority)
                .Include(t => t.Submitter).Include(t => t.Assignee)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.TicketID == ticketId);

            if (Ticket != null)
            {
                Comments = Ticket.Comments.OrderBy(c => c.DateCommented).ToList();
                var currentUserId = _userManager.GetUserId(User);
                bool isITStaff = User.IsInRole("IT Support") || User.IsInRole("IT Manager");
                CanPerformActions = isITStaff && Ticket.StatusID != 6;
                CanComment = (isITStaff || Ticket.SubmittedByUserID == currentUserId) && Ticket.StatusID != 6;
            }
            else
            {
                // Handle case where ticket might have been deleted between requests
                Comments = new List<TicketComment>(); // Ensure Comments is not null
            }
        }


        // Delete Handler (Keep from previous response if needed)
        // [Authorize(Roles = "IT Manager")]
        // public async Task<IActionResult> OnPostDeleteAsync(int? id) { ... }
    }
}
