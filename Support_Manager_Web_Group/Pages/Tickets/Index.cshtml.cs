using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System; // Added for DateTime
using System.Threading.Tasks; // Added for Task
using Microsoft.AspNetCore.Identity; // Added for UserManager, SignInManager
using Support_Manager_Web_Group.Models; // Added for ApplicationUser, Ticket
using Support_Manager_Web_Group.Data; // Added for ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Added for EF Core functions like CountAsync, Where
using System.Linq; // Added for LINQ methods like Where

namespace Support_Manager_Web_Group.Pages
{
    // No [Authorize] attribute needed here if we want logged-out users to see a version
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager; // To get current user
        private readonly ApplicationDbContext _context; // To query tickets

        // Property to hold the count of open tickets for the logged-in user
        public int OpenTicketCount { get; private set; } = 0; // Default to 0

        // Inject necessary services
        public IndexModel(ILogger<IndexModel> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager; // Store injected UserManager
            _context = context;         // Store injected DbContext
        }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Home page visited at {Time}", DateTime.UtcNow);

                    try
                    {
                        // Define which statuses are considered "Open" (adjust IDs if needed)
                        var openStatusIDs = new List<int> { 1, 2, 3, 4 }; // e.g., Open, Assigned, In Progress, Pending User

                        // Query the database for the count of open tickets submitted by this user
                        OpenTicketCount = await _context.Tickets
                            .Where(t => t.SubmittedByUserID == userId && // Match the user ID
                                        openStatusIDs.Contains(t.StatusID)) // Match open statuses
                            .CountAsync(); // Get the count efficiently

                        _logger.LogInformation("User {UserId} has {Count} open tickets.", userId, OpenTicketCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving open ticket count for User ID {UserId}", userId);
                        // Keep OpenTicketCount at 0 if there's an error
                        OpenTicketCount = 0;
                        // Optionally display an error message to the user via TempData or ViewData
                        // TempData["ErrorMessage"] = "Could not retrieve ticket count.";
                    }
                }
            }
            else
            {
                // User is not logged in, count remains 0
                OpenTicketCount = 0;
            }
        }
    }
}
