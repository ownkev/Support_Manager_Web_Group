using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Support_Manager_Web_Group.Models
{
    public class Ticket
    {
        [Key]
        public int TicketID { get; set; }

        [Required(ErrorMessage = "Title is required.")] // Keep Required
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required.")] // Keep Required
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required]
        public string SubmittedByUserID { get; set; } // Required FK

        public string? AssignedToUserID { get; set; } // Nullable FK

        [Required] // Status is always required
        [Display(Name = "Status")]
        public int StatusID { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public int PriorityID { get; set; }

        [StringLength(100)]
        public string? Category { get; set; } // Allow null/empty category explicitly

        [Display(Name = "Date Submitted")]
        [DataType(DataType.DateTime)]
        public DateTime DateSubmitted { get; set; }

        [Display(Name = "Date Resolved")]
        [DataType(DataType.DateTime)]
        public DateTime? DateResolved { get; set; }

        // --- Navigation Properties ---
        [ForeignKey("SubmittedByUserID")]
        public virtual ApplicationUser Submitter { get; set; }

        [ForeignKey("AssignedToUserID")]
        public virtual ApplicationUser Assignee { get; set; }

        [ForeignKey("StatusID")]
        public virtual TicketStatus Status { get; set; }

        [ForeignKey("PriorityID")]
        public virtual TicketPriority Priority { get; set; } // Navigation Property

        public virtual ICollection<TicketComment> Comments { get; set; }

        public Ticket()
        {
            DateSubmitted = DateTime.Now;
            StatusID = 1; // Default 'Open'
            PriorityID = 2; // Default 'Medium' (can be changed)
            Comments = new HashSet<TicketComment>(); // Initialize collection
        }
    }
}
