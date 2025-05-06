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
using System.Security.Claims;
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

        [BindProperty]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comments")]
        public string? NewCommentText { get; set; } // Make nullable string for optional

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
                .Include(t => t.Submitter) // Include for display
                .FirstOrDefaultAsync(m => m.TicketID == id);

            if (Ticket == null) { return NotFound(); }
            if (Ticket.StatusID == 6) { TempData["ErrorMessage"] = "Cannot edit a closed ticket."; return RedirectToPage("./Details", new { id = Ticket.TicketID }); }

            OriginalTitle = Ticket.Title;
            OriginalDescription = Ticket.Description;
            SubmitterInfo = $"{Ticket.Submitter?.FullName} ({Ticket.Submitter?.Email})";

            await PopulateDropdownsAsync(Ticket.StatusID, Ticket.PriorityID, Ticket.AssignedToUserID, Ticket.Category);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove fields not bound from form or not editable
            ModelState.Remove("Ticket.Submitter"); ModelState.Remove("Ticket.Assignee");
            ModelState.Remove("Ticket.Status"); ModelState.Remove("Ticket.Priority");
            ModelState.Remove("Ticket.Title"); ModelState.Remove("Ticket.Description");
            ModelState.Remove("Ticket.DateSubmitted"); ModelState.Remove("Ticket.DateResolved");
            ModelState.Remove("Ticket.Comments"); // If collection exists

            // Check if the model state (for bound properties like StatusID, Category etc.) is valid
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Edit Ticket POST failed validation for TicketID: {Ticket?.TicketID}");
                await ReloadDataForErrorAsync();
                return Page();
            }

            // Fetch the existing ticket from DB to update selected fields
            var ticketToUpdate = await _context.Tickets.FindAsync(Ticket.TicketID);
            if (ticketToUpdate == null) { return NotFound(); }
            if (ticketToUpdate.StatusID == 6) { TempData["ErrorMessage"] = "Cannot save closed ticket."; return RedirectToPage("./Details", new { id = ticketToUpdate.TicketID }); }

            // --- Update allowed fields using TryUpdateModelAsync ---
            // PriorityID and Category are now implicitly optional due to model changes
            if (await TryUpdateModelAsync<Ticket>(
                 ticketToUpdate, "Ticket", // Prefix must match asp-for
                 t => t.Category, // Allowed fields to bind from submitted form
                 t => t.PriorityID,
                 t => t.StatusID,
                 t => t.AssignedToUserID))
            {
                _logger.LogInformation($"Attempting to update TicketID: {ticketToUpdate.TicketID}");

                // Handle DateResolved based on Status change
                int resolvedStatusId = 5; int closedStatusId = 6;
                bool justClosedOrResolved = (ticketToUpdate.StatusID == resolvedStatusId || ticketToUpdate.StatusID == closedStatusId);
                if (justClosedOrResolved && ticketToUpdate.DateResolved == null) { ticketToUpdate.DateResolved = DateTime.Now; }
                else if (!justClosedOrResolved) { ticketToUpdate.DateResolved = null; } // Clear if reopened

                // Add Comment if provided
                bool commentAdded = false;
                if (!string.IsNullOrWhiteSpace(NewCommentText))
                {
                    var userId = _userManager.GetUserId(User);
                    if (userId != null)
                    {
                        var comment = new TicketComment { TicketID = ticketToUpdate.TicketID, UserID = userId, CommentText = NewCommentText.Trim() };
                        _context.TicketComments.Add(comment);
                        commentAdded = true;
                        _logger.LogInformation($"Adding comment while editing TicketID: {ticketToUpdate.TicketID} by User: {userId}");
                    }
                    else { _logger.LogWarning("Could not add comment - User ID null."); }
                }

                try
                {
                    await _context.SaveChangesAsync(); // Saves Ticket update AND Comment add
                    _logger.LogInformation($"TicketID: {ticketToUpdate.TicketID} updated.");
                    TempData["SuccessMessage"] = "Ticket updated successfully" + (commentAdded ? " and comment added." : ".");
                    // TODO: Audit Log
                    return RedirectToPage("./Details", new { id = ticketToUpdate.TicketID });
                }
                catch (DbUpdateConcurrencyException ex) { /* ... Handle concurrency ... */ }
                catch (DbUpdateException ex) { /* ... Handle other DB errors ... */ }
            }
            else { _logger.LogWarning($"TryUpdateModelAsync failed for TicketID: {Ticket?.TicketID}"); }

            // If TryUpdateModelAsync or save failed, repopulate and return page
            await ReloadDataForErrorAsync();
            return Page();
        }

        private async Task ReloadDataForErrorAsync()
        {
            var originalTicket = await _context.Tickets.AsNoTracking().Include(t => t.Submitter).FirstOrDefaultAsync(t => t.TicketID == Ticket.TicketID);
            if (originalTicket != null)
            {
                OriginalTitle = originalTicket.Title; OriginalDescription = originalTicket.Description;
                SubmitterInfo = $"{originalTicket.Submitter?.FullName} ({originalTicket.Submitter?.Email})";
                // Repopulate dropdowns using potentially invalid submitted values from Ticket property
                await PopulateDropdownsAsync(Ticket.StatusID, Ticket.PriorityID, Ticket.AssignedToUserID, Ticket.Category);
            }
            else { /* Handle case where ticket deleted */ await PopulateDropdownsAsync(0, 0, null, null); }
        }

        private async Task PopulateDropdownsAsync(int currentStatusId, int currentPriorityId, string currentAssigneeId, string currentCategory)
        {
            try
            {
                StatusList = new SelectList(await _context.TicketStatuses.OrderBy(s => s.StatusID).ToListAsync(), nameof(TicketStatus.StatusID), nameof(TicketStatus.StatusName), currentStatusId);
                // Priorities - Allow no selection
                var priorities = await _context.TicketPriorities.OrderBy(p => p.PriorityID).Select(p => new { p.PriorityID, p.PriorityName }).ToListAsync();
                var priorityListItems = priorities.Select(p => new SelectListItem { Value = p.PriorityID.ToString(), Text = p.PriorityName }).ToList();
                // Add a default/optional item. Use empty string for value if PriorityID was nullable, use 0 if int and handle 0 on POST.
                priorityListItems.Insert(0, new SelectListItem { Value = "0", Text = "-- Select Priority (Optional) --" }); // Using 0 for non-required int
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
                categoryListItems.Insert(0, new SelectListItem { Value = "", Text = "-- Select Category (Optional) --" });
                CategoryList = new SelectList(categoryListItems, "Value", "Text", currentCategory);
            }
            catch (Exception ex) { _logger.LogError(ex, "Failed to populate dropdowns for Edit page."); /* Set empty lists */ }
        }
    }
}
