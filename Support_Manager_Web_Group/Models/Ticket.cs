using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Support_Manager_Web_Group.Models // Ensure namespace matches
{
    public class Ticket
    {
        [Key]
        public int TicketID { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        // Foreign Key to ApplicationUser (string Id)
        [Required]
        [Display(Name = "Submitted By")]
        public string SubmittedByUserID { get; set; }

        [Display(Name = "Assigned To")]
        public string? AssignedToUserID { get; set; } // string FK, nullable

        [Required]
        [Display(Name = "Status")]
        public int StatusID { get; set; } // FK

        [Required] // Still required on model, but maybe not on Edit form input
        [Display(Name = "Priority")]
        public int PriorityID { get; set; } // FK

        [StringLength(100)]
        public string Category { get; set; }

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
        public virtual TicketPriority Priority { get; set; }

        // *** ADDED Navigation Property for Comments ***
        public virtual ICollection<TicketComment> Comments { get; set; }

        public Ticket()
        {
            DateSubmitted = DateTime.Now;
            StatusID = 1; // Default 'Open'
            PriorityID = 2; // Default 'Medium'
            Comments = new HashSet<TicketComment>(); // Initialize collection
        }
    }
}
