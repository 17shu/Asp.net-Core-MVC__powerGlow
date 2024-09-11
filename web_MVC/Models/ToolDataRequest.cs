namespace web_MVC.Models
{
    public class ToolDataRequest
    {
        public List<ToolModel> ToolData { get; set; }
    }

    public class ToolModel
    {
        public string ToolName { get; set; }       // 工具名稱，例如 TOOL1, TOOL2
        public int WindowBefore { get; set; }      // 該工具可向前移動的最大分鐘數
        public int WindowAfter { get; set; }       // 該工具可向後移動的最大分鐘數
        public List<DataPointModel> Data { get; set; }  // 工具的數據點，每分鐘一筆
    }

    public class DataPointModel
    {
        public DateTime Timestamp { get; set; }    // 每個數據點的時間戳
        public double Value { get; set; }          // 每個時間點的數據值
    }
}



