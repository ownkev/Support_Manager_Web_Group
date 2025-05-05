using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Support_Manager_Web_Group.Models
{
    // Custom user inheriting IdentityUser (string Id)
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [StringLength(50)]
        [Display(Name = "Employee ID")]
        public string? EmployeeID { get; set; } 

        // Navigation properties needed for EF Core relationships
        [InverseProperty("Submitter")]
        public virtual ICollection<Ticket> SubmittedTickets { get; set; } = new HashSet<Ticket>();

        [InverseProperty("Assignee")]
        public virtual ICollection<Ticket> AssignedTickets { get; set; } = new HashSet<Ticket>();
    }
}
