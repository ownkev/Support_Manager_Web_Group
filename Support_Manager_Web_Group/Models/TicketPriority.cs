using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Support_Manager_Web_Group.Models 
{
    public class TicketPriority
    {
        [Key] // Primary Key
        public int PriorityID { get; set; }

        [Required(ErrorMessage = "Priority Name is required.")]
        [StringLength(50)]
        [Display(Name = "Priority Name")]
        public string PriorityName { get; set; }

        // Navigation property back to Tickets
        public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
    }
}
