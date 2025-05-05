using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Support_Manager_Web_Group.Models // Ensure namespace matches
{
    public class TicketStatus
    {
        [Key] // Primary Key
        public int StatusID { get; set; }

        [Required(ErrorMessage = "Status Name is required.")]
        [StringLength(50)]
        [Display(Name = "Status Name")]
        public string StatusName { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
    }
}
