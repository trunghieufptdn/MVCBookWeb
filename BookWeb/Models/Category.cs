using System.ComponentModel.DataAnnotations;

namespace BookWeb.Models;

public class Category
{
    [Key]
    public int ID { get; set; }
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    
    [Required]
    public string Status { get; set; }
}