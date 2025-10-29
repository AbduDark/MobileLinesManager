
using System.Collections.Generic;
using MobileLinesManager.Models;

namespace MobileLinesManager.ViewModels
{
    public class OperatorViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ColorHex { get; set; }
        public ICollection<Category> Categories { get; set; }
        
        public int TotalLines
        {
            get
            {
                int total = 0;
                foreach (var category in Categories)
                {
                    total += category.Lines?.Count ?? 0;
                }
                return total;
            }
        }
    }
}
