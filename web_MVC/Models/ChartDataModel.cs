using System;

namespace web_MVC.Models
{
    public class ChartDataModel
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public string Datetime { get; set; }

        public double Diff {  get; set; }

        public string color { get; set; }
    }

    public class ChartEnergyControl
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public double  perValue{ get; set; }

        public double Time { get; set; }

    }
}
