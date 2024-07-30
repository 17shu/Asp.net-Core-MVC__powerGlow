using System.Collections.Generic;

namespace web_MVC.Models
{
    public class ChartViewModel
    {
        public List<string> Names { get; set; }
        public string SelectedOption { get; set; }
        public string SelectedChartType { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> SelectedNames { get; set; }

        public ChartViewModel()
        {
            Names = new List<string>
            {
                "TOOL1", "TOOL2", "TOOL3", "TOOL5", "TOOL6", "TOOL7", "TOOL8", "TOOL9", "TOOL10",
                "TOOL11", "TOOL12_", "TOOL13", "TOOL14", "TOOL15", "TOOL16", "TOOL17", "TOOL18",
                "TOOL19", "TOOL20", "AIRCOM", "TOTAL"
            };
            SelectedNames = new List<string>();
        }
    }

  
}
