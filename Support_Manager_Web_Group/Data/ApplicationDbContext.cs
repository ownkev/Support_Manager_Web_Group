using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Support_Manager_Web_Group.Models;

namespace Support_Manager_Web_Group.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketStatus> TicketStatuses { get; set; }
        public DbSet<TicketPriority> TicketPriorities { get; set; }
        // KnowledgeBase excluded

        // *** ADDED DbSet for TicketComments ***
        public DbSet<TicketComment> TicketComments { get; set; }
        // ************************************

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity config

            // Ticket Configuration
            modelBuilder.Entity<Ticket>(entity => {
                 entity.HasOne(d => d.Submitter).WithMany(p => p.SubmittedTickets).HasForeignKey(d => d.SubmittedByUserID).OnDelete(DeleteBehavior.ClientSetNull);
                 entity.HasOne(d => d.Assignee).WithMany(p => p.AssignedTickets).HasForeignKey(d => d.AssignedToUserID).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
                 entity.HasOne(d => d.Status).WithMany(s => s.Tickets).HasForeignKey(d => d.StatusID);
                 entity.HasOne(d => d.Priority).WithMany(p => p.Tickets).HasForeignKey(d => d.PriorityID);
                 entity.Property(d => d.DateSubmitted).HasDefaultValueSql("GETDATE()");
                 // Configure the relationship FROM Ticket TO Comments
                 entity.HasMany(d => d.Comments).WithOne(p => p.Ticket).HasForeignKey(p => p.TicketID).OnDelete(DeleteBehavior.Cascade); // Delete comments if ticket is deleted
             });

            // *** ADDED TicketComment Configuration ***
            modelBuilder.Entity<TicketComment>(entity => {
                entity.ToTable("TicketComments"); // Explicitly name table

                // Link back to User (one User has many Comments)
                // Assumes ApplicationUser does NOT need ICollection<TicketComment> Comments
                entity.HasOne(d => d.User)
                      .WithMany() // No corresponding collection property on User needed here
                      .HasForeignKey(d => d.UserID)
                      .OnDelete(DeleteBehavior.ClientSetNull); // Set UserID to null if user deleted (requires UserID column to be nullable in DB if user deletion is allowed)
                                                               // Or use DeleteBehavior.Restrict to prevent user deletion if they have comments
            });
            // *****************************************

            // Seed Statuses
            modelBuilder.Entity<TicketStatus>().HasData( /* ... as before ... */ );
            // Seed Priorities
            modelBuilder.Entity<TicketPriority>().HasData( /* ... as before ... */ );
        }
    }
}
