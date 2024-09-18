namespace web_MVC.Models
{
    public class ShiftRange
    {
        public int Early { get; set; }
        public int Later { get; set; }
    }

    public class OptimizeRequest
    {
        public List<ChartDataModel> ToolData { get; set; }
        public Dictionary<string, ShiftRange> ToolShiftRanges { get; set; }
    }

    // 移動數據請求模型
    public class ShiftRequest
    {
        public List<ChartDataModel> OriginalData { get; set; }
        public Dictionary<string, int> NameShiftMap { get; set; }
    }

    public class ShiftedDataResult
    {
        public List<ChartDataModel> ShiftedData { get; set; }  // 移動後的數據
        public Dictionary<string, int> ShiftedOffsets { get; set; }  // 每個工具的位移量
    }

}



