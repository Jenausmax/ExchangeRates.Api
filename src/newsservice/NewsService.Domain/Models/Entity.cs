using System.ComponentModel.DataAnnotations;

namespace NewsService.Domain.Models
{
    public class Entity
    {
        [Key]
        public int Id { get; set; }
    }
}
