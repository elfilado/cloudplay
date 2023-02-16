using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CloudplayWebApp.Models
{
    public class CustomVM
    {
        [Key]
        [DisplayName("Name")]
        public string Name { get; set; } 
      
        [DisplayName("IP")]
        public string IP { get; set; }

        [Required]
        [DisplayName("Login")]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [DisplayName("Password")]
        public string Password { get; set; }

       
    }
}
