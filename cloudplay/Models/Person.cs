using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace cloudplay.Models
{
    /// <summary>
    /// Class qui respresente une personne
    /// </summary>
    public class Person
    {
        /// <summary>
        /// Id de la personne
        /// </summary>
        [Required]
        [Range(1, 100)]
        [DisplayName("ID")]
        public string Id {get; set; }

        /// <summary>
        /// Prenom de la personne
        /// </summary>
        [Required]
        [StringLength(20, MinimumLength = 2)]
        [DisplayName("First Name")]
        public string Firstname { get; set; }

        /// <summary>
        /// Nom de la personne
        /// </summary>
        [Required]
        [StringLength(40, MinimumLength = 2)]
        [DisplayName("Last Name")]
        public string Lastname { get; set; }

        /// <summary>
        /// Adresse mail de la personne
        /// </summary>
        [EmailAddress]
        [StringLength(40, MinimumLength = 6)]
        [DisplayName("Email")]
        public string Email { get; set; }

        /// <summary>
        /// Constructeur
        /// </summary>
        /// <param name="id"></param>
        /// <param name="firstname"></param>
        /// <param name="lastname"></param>
        /// <param name="email"></param>
        public Person(string id, string firstname, string lastname, string email)
        {
            this.Id = id;
            this.Firstname = firstname;
            this.Lastname = lastname;
            this.Email = email;
        }
    }
}
