
using System.Collections.Generic;
using MobileLinesManager.Models;

namespace MobileLinesManager.ViewModels
{
    public class OperatorViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ColorHex { get; set; }
        public ICollection<Group> Groups { get; set; }
        
        public int TotalLines
        {
            get
            {
                int total = 0;
                foreach (var group in Groups)
                {
                    total += group.Lines?.Count ?? 0;
                }
                return total;
            }
        }
    }
}
