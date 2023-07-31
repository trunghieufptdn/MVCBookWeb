using BookWeb.Models;
namespace BookWeb.ViewModels;

public class OderDetailsVM
{ 
    public OrderHeader OrderHeader { get; set; }
    public IEnumerable<OrderDetails> OderDetails { get; set; } 
    
}