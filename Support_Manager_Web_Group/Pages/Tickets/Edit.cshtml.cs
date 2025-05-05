using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Support_Manager_Web_Group.Data;
using Support_Manager_Web_Group.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims; // Needed for User ID
using System.ComponentModel.DataAnnotations; // For Display attribute

namespace Support_Manager_Web_Group.Pages.Tickets
{
    [Authorize(Roles = "IT Support,IT Manager")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<EditModel> logger)
        { _context = context; _userManager = userManager; _logger = logger; }

        [BindProperty]
        public Ticket Ticket { get; set; } = default!;

        // Property for adding a comment during edit
        [BindProperty]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Add Comment (Optional)")]
        public string NewCommentText { get; set; }

        // Properties for dropdown lists
        public SelectList StatusList { get; set; }
        public SelectList PriorityList { get; set; }
        public SelectList AssigneeList { get; set; }
        public SelectList CategoryList { get; set; }

        // Properties to display original read-only fields
        public string OriginalTitle { get; set; }
        public string OriginalDescription { get; set; }
        public string SubmitterInfo { get; set; }


        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) { return NotFound(); }

            Ticket = await _context.Tickets
                .Include(t => t.Submitter) // Include submitter for display
                .FirstOrDefaultAsync(m => m.TicketID == id);

            if (Ticket == null) { return NotFound(); }
            if (Ticket.StatusID == 6) { TempData["ErrorMessage"] = "Cannot edit a closed ticket."; return RedirectToPage("./Details", new { id = Ticket.TicketID }); }

            // Store read-only info
            OriginalTitle = Ticket.Title;
            OriginalDescription = Ticket.Description;
            SubmitterInfo = $"{Ticket.Submitter?.FullName} ({Ticket.Submitter?.Email})";

            await PopulateDropdownsAsync(Ticket.StatusID, Ticket.PriorityID, Ticket.AssignedToUserID, Ticket.Category);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove fields not intended to be bound or validated from the form POST
            ModelState.Remove("Ticket.Submitter"); ModelState.Remove("Ticket.Assignee");
            ModelState.Remove("Ticket.Status"); ModelState.Remove("Ticket.Priority");
            ModelState.Remove("Ticket.Title"); ModelState.Remove("Ticket.Description");
            ModelState.Remove("Ticket.DateSubmitted");
            ModelState.Remove("Ticket.DateResolved");
            // NewCommentText is bound separately and validated manually if needed

            // Check validation for fields that ARE bound (Category, PriorityID, StatusID, AssignedToUserID)
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Edit Ticket POST failed validation for TicketID: {Ticket?.TicketID}");
                await ReloadDataForErrorAsync();
                return Page();
            }

            // Fetch the existing ticket from DB to update
            var ticketToUpdate = await _context.Tickets.FindAsync(Ticket.TicketID);
            if (ticketToUpdate == null) { return NotFound(); }
            if (ticketToUpdate.StatusID == 6) { TempData["ErrorMessage"] = "Cannot save closed ticket."; return RedirectToPage("./Details", new { id = ticketToUpdate.TicketID }); }

            // Update allowed fields using TryUpdateModelAsync
            if (await TryUpdateModelAsync<Ticket>(
                 ticketToUpdate, "Ticket", // Prefix must match asp-for
                 t => t.Category, t => t.PriorityID, t => t.StatusID, t => t.AssignedToUserID))
            {
                _logger.LogInformation($"Attempting to update TicketID: {ticketToUpdate.TicketID}");

                // Handle DateResolved based on Status change
                int resolvedStatusId = 5; int closedStatusId = 6;
                if ((ticketToUpdate.StatusID == resolvedStatusId || ticketToUpdate.StatusID == closedStatusId) && ticketToUpdate.DateResolved == null)
                {
                    ticketToUpdate.DateResolved = DateTime.Now;
                }
                else if (ticketToUpdate.StatusID != resolvedStatusId && ticketToUpdate.StatusID != closedStatusId)
                {
                    ticketToUpdate.DateResolved = null;
                }

                // Add Comment if provided
                bool commentAdded = false;
                string commentAuditDetails = "";
                if (!string.IsNullOrWhiteSpace(NewCommentText))
                {
                    var userId = _userManager.GetUserId(User);
                    if (userId != null)
                    {
                        var comment = new TicketComment
                        {
                            TicketID = ticketToUpdate.TicketID,
                            UserID = userId,
                            CommentText = NewCommentText.Trim(),
                            DateCommented = DateTime.Now
                        };
                        _context.TicketComments.Add(comment);
                        commentAdded = true;
                        commentAuditDetails = $" Comment Added: {NewCommentText.Trim().Substring(0, Math.Min(NewCommentText.Trim().Length, 50))}";
                        _logger.LogInformation($"Adding comment to TicketID: {ticketToUpdate.TicketID} by User: {userId}");
                    }
                    else { _logger.LogWarning("Could not add comment - User ID null."); }
                }

                try
                {
                    await _context.SaveChangesAsync(); // Save Ticket changes AND Comment
                    _logger.LogInformation($"TicketID: {ticketToUpdate.TicketID} updated successfully.");
                    TempData["SuccessMessage"] = "Ticket updated successfully" + (commentAdded ? " and comment added." : ".");
                    // TODO: Add Audit Log Entry for Ticket Update (include comment details if added)
                    // DatabaseHelper.LogAudit("Ticket Updated", _userManager.GetUserId(User), ticketToUpdate.TicketID, $"Status->{ticketToUpdate.StatusID}, Prio->{ticketToUpdate.PriorityID}, Assignee->{ticketToUpdate.AssignedToUserID}.{commentAuditDetails}");
                    return RedirectToPage("./Details", new { id = ticketToUpdate.TicketID });
                }
                catch (DbUpdateConcurrencyException ex) { _logger.LogWarning(ex, $"Concurrency error updating TicketID: {ticketToUpdate.TicketID}"); ModelState.AddModelError(string.Empty, "Ticket modified by another user."); }
                catch (DbUpdateException ex) { _logger.LogError(ex, $"Database error updating TicketID: {ticketToUpdate.TicketID}"); ModelState.AddModelError(string.Empty, "Unable to save changes."); }
            }
            else { _logger.LogWarning($"TryUpdateModelAsync failed for TicketID: {Ticket?.TicketID}"); }

            // If TryUpdateModelAsync or save failed, repopulate and return page
            await ReloadDataForErrorAsync();
            return Page();
        }

        // Helper to reload dropdowns and non-posted data on error
        private async Task ReloadDataForErrorAsync()
        {
            var originalTicket = await _context.Tickets.AsNoTracking().Include(t => t.Submitter).FirstOrDefaultAsync(t => t.TicketID == Ticket.TicketID);
            if (originalTicket != null)
            {
                OriginalTitle = originalTicket.Title; OriginalDescription = originalTicket.Description;
                SubmitterInfo = $"{originalTicket.Submitter?.FullName} ({originalTicket.Submitter?.Email})";
            }
            else { OriginalTitle = "[N/A]"; OriginalDescription = "[N/A]"; SubmitterInfo = "[N/A]"; }
            await PopulateDropdownsAsync(Ticket.StatusID, Ticket.PriorityID, Ticket.AssignedToUserID, Ticket.Category);
        }

        // Helper to populate dropdowns
        private async Task PopulateDropdownsAsync(int currentStatusId, int currentPriorityId, string currentAssigneeId, string currentCategory)
        {
            try
            {
                StatusList = new SelectList(await _context.TicketStatuses.OrderBy(s => s.StatusID).ToListAsync(), nameof(TicketStatus.StatusID), nameof(TicketStatus.StatusName), currentStatusId);
                // Make Priority optional by adding a default item
                var priorities = await _context.TicketPriorities.OrderBy(p => p.PriorityID).Select(p => new { p.PriorityID, p.PriorityName }).ToListAsync();
                var priorityListItems = priorities.Select(p => new SelectListItem { Value = p.PriorityID.ToString(), Text = p.PriorityName }).ToList();
                priorityListItems.Insert(0, new SelectListItem { Value = "0", Text = "-- Select Priority --" }); // Use "0" for optional int
                PriorityList = new SelectList(priorityListItems, "Value", "Text", currentPriorityId);
                // Assignable Users
                var assignableUsers = await _userManager.Users
                                        .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && (r.Name == "IT Support" || r.Name == "IT Manager"))))
                                        .OrderBy(u => u.FullName).Select(u => new { UserID = u.Id, DisplayName = u.FullName }).ToListAsync();
                var userListWithUnassign = new List<object> { new { UserID = (string)null, DisplayName = "[ Unassign ]" } };
                userListWithUnassign.AddRange(assignableUsers);
                AssigneeList = new SelectList(userListWithUnassign, "UserID", "DisplayName", currentAssigneeId);
                // Categories
                var categories = new List<string> { "Hardware", "Software", "Network", "Account Request", "Other" };
                var categoryListItems = categories.Select(c => new SelectListItem { Value = c, Text = c }).ToList();
                categoryListItems.Insert(0, new SelectListItem { Value = "", Text = "-- Select Category --" });
                CategoryList = new SelectList(categoryListItems, "Value", "Text", currentCategory);
            }
            catch (Exception ex) { _logger.LogError(ex, "Failed to populate dropdowns for Edit page."); /* Set empty lists */ }
        }
    }
}
