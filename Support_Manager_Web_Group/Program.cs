using Support_Manager_Web_Group.Data;
using Support_Manager_Web_Group.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// --- Add services ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false; // Keep false for easier testing
    options.Password.RequireDigit = false; // Simple password for testing
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4; // Simple password for testing
})
    .AddRoles<IdentityRole>() // Enable Role Management
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddRazorPages(options => {
    // Secure the Tickets folder by default
    options.Conventions.AuthorizeFolder("/Tickets");
    // Allow anonymous only to specific needed pages
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Privacy");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
    options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
});
builder.Services.AddAuthorization();

// --- Configure Pipeline ---
var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.UseMigrationsEndPoint(); }
else { app.UseExceptionHandler("/Error"); app.UseHsts(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Add Auth middleware
app.UseAuthorization(); // Add Authorization middleware
app.MapRazorPages();

// --- Seed Roles & Initial Admin ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Attempting to seed roles and admin user...");
    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        // Seed Roles
        string[] roleNames = { "Employee", "IT Support", "IT Manager" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation($"Role '{roleName}' created.");
            }
        }
        // Seed Admin
        string adminEmail = "admin@support.local"; string adminFullName = "Admin"; string adminPassword = "Password1"; // CHANGE PWD!
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = adminFullName, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                logger.LogInformation($"Admin user '{adminEmail}' created.");
                if (await roleManager.RoleExistsAsync("IT Manager")) { await userManager.AddToRoleAsync(adminUser, "IT Manager"); logger.LogInformation($"Admin assigned to 'IT Manager' role."); }
            }
            else { logger.LogError($"Error creating admin: {string.Join(", ", createResult.Errors.Select(e => e.Description))}"); }
        }
        else { logger.LogInformation($"Admin '{adminEmail}' exists."); if (!await userManager.IsInRoleAsync(adminUser, "IT Manager") && await roleManager.RoleExistsAsync("IT Manager")) { await userManager.AddToRoleAsync(adminUser, "IT Manager"); logger.LogInformation($"Existing admin assigned to 'IT Manager'."); } }
    }
    catch (Exception ex) { logger.LogError(ex, "Error seeding database."); }
}

app.Run();
