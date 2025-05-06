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
        public DbSet<TicketComment> TicketComments { get; set; } // Added DbSet

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ticket Configuration
            modelBuilder.Entity<Ticket>(entity => {
                entity.HasOne(d => d.Submitter).WithMany(p => p.SubmittedTickets).HasForeignKey(d => d.SubmittedByUserID).OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasOne(d => d.Assignee).WithMany(p => p.AssignedTickets).HasForeignKey(d => d.AssignedToUserID).IsRequired(false).OnDelete(DeleteBehavior.ClientSetNull);
                entity.HasOne(d => d.Status).WithMany(s => s.Tickets).HasForeignKey(d => d.StatusID);
                entity.HasOne(d => d.Priority).WithMany(p => p.Tickets).HasForeignKey(d => d.PriorityID);
                entity.Property(d => d.DateSubmitted).HasDefaultValueSql("GETDATE()");
                // Configure relationship TO comments FROM ticket
                entity.HasMany(d => d.Comments).WithOne(p => p.Ticket).HasForeignKey(p => p.TicketID).OnDelete(DeleteBehavior.Cascade); // Cascade delete comments if ticket deleted
            });

            // TicketComment Configuration (Link back to User)
            modelBuilder.Entity<TicketComment>(entity => {
                entity.ToTable("TicketComments"); // Optional: Explicit table name
                entity.HasOne(d => d.User)
                      .WithMany() // Assuming User doesn't need direct collection of comments
                      .HasForeignKey(d => d.UserID)
                      .OnDelete(DeleteBehavior.ClientSetNull); // Or Restrict
            });

            // Seeding (Status & Priority - keep if DB is new, remove if DB already has this data)
            modelBuilder.Entity<TicketStatus>().HasData( /* ... Status data ... */ );
            modelBuilder.Entity<TicketPriority>().HasData( /* ... Priority data ... */ );
        }
    }
}
