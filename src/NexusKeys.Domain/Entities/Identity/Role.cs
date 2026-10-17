using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GameStore.API.Entities
{
    public class Role
    {
        [Key]
        public int Id { get; set; }
        public string RoleName { get; set; } = null!;
        
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
