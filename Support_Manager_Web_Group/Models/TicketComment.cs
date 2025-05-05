using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Support_Manager_Web_Group.Models 
{
    // Represents a single comment on a support ticket
    public class TicketComment
    {
        [Key]
        public int CommentID { get; set; } // Primary Key

        [Required]
        public int TicketID { get; set; } // Foreign key to the Ticket table

        [Required]
        public string UserID { get; set; } // Foreign key to the AspNetUsers table (string Id)

        [Required(ErrorMessage = "Comment text cannot be empty.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Comment")]
        public string CommentText { get; set; }

        [Display(Name = "Date Added")]
        public DateTime DateCommented { get; set; }

        // --- Navigation Properties ---
        // Link back to the parent Ticket
        [ForeignKey("TicketID")]
        public virtual Ticket Ticket { get; set; }

        // Link back to the User who wrote the comment
        [ForeignKey("UserID")]
        public virtual ApplicationUser User { get; set; }

        // Constructor to set default date
        public TicketComment()
        {
            DateCommented = DateTime.Now;
        }
    }
}
