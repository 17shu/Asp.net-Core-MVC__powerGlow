using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Xml.Linq;
using web_MVC.Models;

namespace web_MVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiController> _logger;
        public ApiController(IConfiguration configuration, ILogger<ApiController> logger)
        {
            _configuration = configuration;
          
            _logger = logger;
        }

        [HttpGet("GetPowerData")]

        public async Task<IActionResult> GetPowerData(int minuteOfDay)
        {
            try
            {
                var powerData = await FetchPowerData(minuteOfDay);
                return Ok(powerData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

       
        private async Task<List<ChartDataModel>> FetchPowerData(int minuteOfDay)
        {
            var powerdata = new List<ChartDataModel>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            Console.WriteLine(connectionString);

            try
            {
                using (var con = new MySqlConnection(connectionString))
                {
                    con.Open();
                    var prequery = new MySqlCommand(@"
                    SELECT DISTINCT Name, Value AS value, 
                    DATE_FORMAT(Datetime, '%Y-%m-%d %H:%i:00') AS Datetime
                    FROM di_schemas.powerdata_dmpower 
                    WHERE DATE(Datetime) = '2024-05-03' AND HOUR(Datetime) * 60 + MINUTE(Datetime) = @MinuteOfDay
                    GROUP BY Name, DATE_FORMAT(Datetime, '%Y-%m-%d %H:%i:00')
                    ORDER BY Datetime;", con);

                    prequery.Parameters.AddWithValue("@MinuteOfDay", minuteOfDay - 1);
                    var previousValues = new Dictionary<string, double>();
                    using(var reader = prequery.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader["Name"].ToString();
                            previousValues[name] = Convert.ToDouble(reader["value"]);
                        }
                    }
                    var query = new MySqlCommand(@"
                    SELECT DISTINCT Name, Value AS value, 
                    DATE_FORMAT(Datetime, '%Y-%m-%d %H:%i:00') AS Datetime
                    FROM di_schemas.powerdata_dmpower 
                    WHERE DATE(Datetime) = '2024-05-03' AND HOUR(Datetime) * 60 + MINUTE(Datetime) = @MinuteOfDay
                    GROUP BY Name, DATE_FORMAT(Datetime, '%Y-%m-%d %H:%i:00')
                    ORDER BY Datetime;", con);
                    query.Parameters.AddWithValue("@MinuteOfDay", minuteOfDay);

                    // 用來存儲每個名稱的上一個值

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string name = reader["Name"].ToString();
                            double currentValue = Convert.ToDouble(reader["Value"]);
                            double diff = 0;
                            string color = "";

                            // 檢查字典中是否已有該名稱的前一個值
                            if (previousValues.ContainsKey(name))
                            {
                                Console.WriteLine("pre: " + previousValues[name]+" cur: "+currentValue);
                                diff = currentValue - previousValues[name];
                                Console.WriteLine($"{name} ({diff}).........................");
                            }

                            // 設置顏色
                            if (diff >= 0.5)
                            {
                                color = "#FF0000";
                            }
                            else if (diff <= -0.5)
                            {
                                color = "#0066CC";
                            }

                            powerdata.Add(new ChartDataModel
                            {
                                Name = name,
                                Value = currentValue,
                                Datetime = reader["Datetime"].ToString(),
                                Diff = diff,
                                color = color
                            });

                            Console.WriteLine("Diff:  " + diff + "&&&&&&&&&&");
                            // 判斷是否需要呼叫 PostEventRecordAsync
                            if (Math.Abs(diff) >= 0.5)
                            {
                                Console.WriteLine("Call API***************************");
                                await PostEventRecordAsync(name.Split('_')[0], Convert.ToDateTime(reader["Datetime"]), diff, "di_schemas.powerevent");
                            }

                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while executing the database query: {ex.Message}", ex);
                throw;
            }

            return powerdata;
        }

        [HttpGet("GetEnergyData")]

        public async Task<IActionResult> GetEnergyData(int minuteOfDay)
        {
            Console.WriteLine("API!!!!!!!!!!!!!!!!!!");
            try
            {
                var energyData = await FetchEnergyData(minuteOfDay);
                return Ok(energyData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private async Task<List<ChartDataModel>> FetchEnergyData(int minuteOfDay)
        {
            Console.WriteLine("fetch!!!!!!!!!!!!!!!!!!!!!!!!!" + minuteOfDay);
            var energyData = new List<ChartDataModel>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            Console.WriteLine(connectionString);
            try
            {
                using (var con = new MySqlConnection(connectionString))
                {
                    con.Open();
                    var query = new MySqlCommand(@"
                SELECT 
                    t1.Name,
                    t1.Value AS postMinVal,
                    t2.Value AS preMinVal,
                    t1.Value - t2.Value AS diff,
                    DATE_FORMAT(t1.Datetime, '%Y-%m-%d %H:%i:00') AS Datetime
                FROM 
                    di_schemas.powerdata_energy AS t1
                JOIN 
                    di_schemas.powerdata_energy AS t2
                ON  
                    t1.Name = t2.Name AND 
                    DATE(t1.Datetime) = DATE(t2.Datetime) AND 
                    t2.Datetime = DATE_SUB(t1.Datetime, INTERVAL 15 MINUTE)
                WHERE 
                    DATE(t1.Datetime) = '2024-05-01' AND 
                    HOUR(t1.Datetime) * 60 + MINUTE(t1.Datetime) = @MinuteOfDay
                GROUP BY 
                    t1.Name, 
                    Datetime;", con);
                    query.Parameters.AddWithValue("@MinuteOfDay", minuteOfDay);

                    var previousValues = new Dictionary<string, double>();

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string currentName = reader["Name"].ToString();
                            double currentValue = Convert.ToDouble(reader["diff"]);
                            double currentDiff;
                            string color = "";

                            // 檢查字典中是否已存在該名稱的前一個值
                            if (previousValues.ContainsKey(currentName))
                            {
                                currentDiff = currentValue - previousValues[currentName];
                            }
                            else
                            {
                                // 如果沒有前一個值，則設置為當前值
                                currentDiff = 0;
                            }

                            if (currentDiff >= 0.5) { color = "#FF0000"; }
                            else if (currentDiff <= -0.5) { color = "#0066CC"; }
                            else { color = ""; }
                            energyData.Add(new ChartDataModel
                            {
                                Name = currentName.Split('_')[0],
                                Value = currentValue,
                                Datetime = reader["Datetime"].ToString(),
                                Diff = currentDiff,
                                color = color
                            });


                            previousValues[currentName] = currentValue;
                            if (currentDiff >= 0.5 || currentDiff <= -0.5)
                            {
                                await PostEventRecordAsync(currentName.Split('_')[0], Convert.ToDateTime(reader["Datetime"]), currentDiff, "di_schemas.energyevent");
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while executing the database query: {ex.Message}", ex);
                throw;
            }

            return energyData;
        }

        [HttpGet("GetPowerHis")]
        public IActionResult GetPowerHisData(DateTime dateS, DateTime dateE, string name)
        {
            try
            {
                var powerData = FetchPowerHisData(dateS, dateE, name);
                return Ok(powerData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private List<ChartDataModel> FetchPowerHisData(DateTime dateS, DateTime dateE, string name)
        {
            Console.WriteLine("fetch!!!!!!!!!!!!!!!!!!!!!!!!!");
            var powerdata = new List<ChartDataModel>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            Console.WriteLine(connectionString);
            try
            {
                using (var con = new MySqlConnection(connectionString))
                {
                    con.Open();
                    var query = new MySqlCommand($@"
                       SELECT 
                            t1.Name AS Name,
                            t1.Value AS max_value,
                            t1.Datetime AS time
                        FROM
                            di_schemas.powerdata_dmpower t1
                        JOIN (
                            SELECT 
                                Name,
                                DATE(Datetime) AS date,
                                MAX(CAST(Value AS DOUBLE)) AS max_value  -- 將 Value 轉換為 DOUBLE
                            FROM
                                di_schemas.powerdata_dmpower
                            WHERE
                                Datetime BETWEEN @DateS AND @DateE
                                AND Name = @Name
                            GROUP BY
                                Name,
                                DATE(Datetime)
                        ) t2 ON t1.Name = t2.Name 
                           AND DATE(t1.Datetime) = t2.date
                           AND CAST(t1.Value AS DOUBLE) = t2.max_value  -- 確保比較的是數值
                        WHERE
                            t1.Name = @Name
                        GROUP BY 
                            DATE(t1.Datetime)
                        ORDER BY
                            t1.Datetime;
                        ;", con);
                    query.Parameters.AddWithValue("@DateS", dateS.ToString("yyyy-MM-dd") + " 00:00:00");
                    query.Parameters.AddWithValue("@DateE", dateE.ToString("yyyy-MM-dd") + " 23:59:59");
                    query.Parameters.AddWithValue("@Name", name + "_Demand_KW");

                    Console.WriteLine(query.CommandText+ dateS.ToString("yyyy-MM-dd") + " 00:00:00       "+  dateE.ToString("yyyy-MM-dd") + " 23:59:59     "+name+"Demand_Kw");

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            powerdata.Add(new ChartDataModel
                            {
                                Name = reader["Name"].ToString(),
                                Value = Convert.ToDouble(reader["max_value"]),
                                Datetime = reader["time"].ToString()
                            });
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while executing the database query: {ex.Message}", ex);
                throw;
            }

            return powerdata;

        }

        [HttpGet("GetEnergyHis")]
        public IActionResult GetEnergyHisData(DateTime dateS, DateTime dateE, string name)
        {
            try
            {
                var energyData = FetchEnergyHisData(dateS, dateE, name);
                return Ok(energyData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private List<ChartDataModel> FetchEnergyHisData(DateTime dateS, DateTime dateE, string name)
        {
            Console.WriteLine("fetch!!!!!!!!!!!!" + name);
            var energyData = new List<ChartDataModel>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            Console.WriteLine(connectionString);
            try
            {
                using (var con = new MySqlConnection(connectionString))
                {
                    con.Open();
                    var query = new MySqlCommand($@"
                SELECT 
                    Name,
                    DATE(Datetime) AS Datetime,
                    MAX(Value) - MIN(Value) AS difference
                FROM 
                    di_schemas.powerdata_energy
                WHERE
                    Datetime BETWEEN @DateS AND @DateE AND Name = @Name
                GROUP BY 
                    DATE(Datetime);", con);
                    query.Parameters.AddWithValue("@DateS", dateS.ToString("yyyy-MM-dd") + "00:00:00");
                    query.Parameters.AddWithValue("@DateE", dateE.ToString("yyyy-MM-dd") + "23:59:00");
                    query.Parameters.AddWithValue("@Name", name + "_KWH");

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            energyData.Add(new ChartDataModel
                            {
                                Name = reader["Name"].ToString(),
                                Value = Convert.ToDouble(reader["difference"]),
                                Datetime = reader["Datetime"].ToString()
                            });
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while executing the database query: {ex.Message}", ex);
                throw;
            }

            return energyData;
        }

        [HttpGet("GetPowerHistory")]
        public async Task<IActionResult> GetPowerHistoryData(DateTime date, string name)
        {
            try
            {
                var powerData =await FetchPowerHistoryData(date, name);
                return Ok(powerData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private async Task<List<ChartDataModel>> FetchPowerHistoryData(DateTime date, string names)
        {
            var powerData = new List<ChartDataModel>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");

            try
            {
                using (var con = new MySqlConnection(connectionString))
                {
                   await con.OpenAsync();

                    // 構建 Name 列表的佔位符

                    var query = new MySqlCommand($@"
                    SELECT 
                        t1.Name,
                        t1.Value,
                        t2.Value as t2V,
                        t1.Datetime,
                        t2.Datetime as t2D,
                        t2.Value - t1.Value as Diff
                    FROM 
                        di_schemas.powerdata_dmpower as t1
                    JOIN
                        di_schemas.powerdata_dmpower as t2
                    ON
                        t1.Name = t2.Name AND
                        DATE(t1.Datetime) = DATE(t2.Datetime) AND
                        t2.Datetime = DATE_ADD(t1.Datetime, INTERVAL 1 MINUTE) 
                    WHERE
                        DATE(t1.Datetime) = @Date AND 
                        t1.Name IN ({names})
                    Group By
                        t1.Name,t1.Datetime
                    ORDER BY 
                        t1.Datetime;", con);

                    query.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));


                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string color = "";
                            double diff = Convert.ToDouble(reader["Diff"]);
                            if (diff >= 0.5) { color = "#FF0000"; }
                            else if (diff <= -0.5) { color = "#0066CC"; }
                            else { color = ""; }
                            powerData.Add(new ChartDataModel
                            {
                                Name = reader["Name"].ToString().Split("_")[0],
                                Value = Convert.ToDouble(reader["Value"]),
                                Datetime = reader["Datetime"].ToString(),
                                Diff = diff,
                                color=color
                            });
                            //if (Math.Abs(diff) >= 0.5)
                            //{
                            //    Console.WriteLine("Call API***************************");
                            //    await PostEventRecordAsync(reader["Name"].ToString().Split('_')[0], Convert.ToDateTime(reader["Datetime"]), diff, "di_schemas.powerevent");
                            //}

                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while executing the database query: {ex.Message}", ex);
                throw;
            }

            return powerData;
        }


        [HttpGet("GetEnergyHistory")]

        public async Task<IActionResult> GetEnergyHistory(DateTime date, string name)
        {
            Console.WriteLine("API!!!!!!!!!!!!!!!!!!");
            try
            {
                var energyData = await FetchEnergyHistory(date, name);
                return Ok(energyData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private async Task<List<ChartDataModel>> FetchEnergyHistory(DateTime date, string name)
        {
            var energyData = new List<ChartDataModel>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            Console.WriteLine(connectionString);

            try
            {
                using (var con = new MySqlConnection(connectionString))
                {
                    await con.OpenAsync();
                    var query = new MySqlCommand($@"
                SELECT 
                    t1.Name,
                    t1.Value,
                    t1.Value - t2.Value AS diff,
                    DATE_FORMAT(t2.Datetime, '%Y-%m-%d %H:%i:00') AS Datetime
                FROM 
                    di_schemas.powerdata_energy AS t1
                JOIN 
                    di_schemas.powerdata_energy AS t2
                ON  
                    t1.Name = t2.Name AND 
                    DATE(t1.Datetime) = DATE(t2.Datetime) AND 
                    t2.Datetime = DATE_SUB(t1.Datetime, INTERVAL 15 MINUTE)
                WHERE 
                    DATE(t1.Datetime) = @Date and 
                    t1.Name in ({name})
                GROUP BY 
                    t1.Name, 
                    Datetime
                Order By
                    Datetime;", con);

                    query.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));

                
                    var previousValues = new Dictionary<string, double>();

                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string currentName = reader["Name"].ToString();
                            double currentValue = Convert.ToDouble(reader["diff"]);
                            double currentDiff;
                            string color="";

                            // 檢查字典中是否已存在該名稱的前一個值
                            if (previousValues.ContainsKey(currentName))
                            {
                                currentDiff = currentValue - previousValues[currentName];
                            }
                            else
                            {
                                // 如果沒有前一個值，則設置為當前值
                                currentDiff = 0;
                            }

                            if(currentDiff >= 0.5) { color = "#FF0000"; }
                            else if(currentDiff<= -0.5) { color = "#0066CC"; }
                            else { color = ""; }
                            energyData.Add(new ChartDataModel
                            {
                                Name = currentName.Split('_')[0],
                                Value = currentValue,
                                Datetime = reader["Datetime"].ToString(),
                                Diff = currentDiff,
                                color = color
                            });


                            previousValues[currentName] = currentValue;
                            //if (currentDiff >= 0.5 || currentDiff <= -0.5)
                            //{
                            //    await PostEventRecordAsync(currentName.Split('_')[0], Convert.ToDateTime(reader["Datetime"]), currentDiff, "di_schemas.energyevent");
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while executing the database query: {ex.Message}", ex);
                throw;
            }
            Console.WriteLine("?????????"+energyData.Count);
            return energyData;
        }


        [HttpPost("eventRecord")]
        public async  void EventRecord(string name,DateTime time,double value, string table)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            using (var con = new MySqlConnection(connectionString))
            {
               await con.OpenAsync();
                    var query = new MySqlCommand($@"
                    INSERT ignore INTO {table}
                    (Name, Time, Value)
                    VALUES
                    (@Name, @Time, @Value);", con);

                    query.Parameters.AddWithValue("@Name", name);
                    query.Parameters.AddWithValue("@Time", time);
                    query.Parameters.AddWithValue("@Value", value);


                    var rowsAffected = await query.ExecuteNonQueryAsync();
                    _logger.LogInformation($"Rows affected: {rowsAffected}");
                
            }
        }



        [HttpPost]
        private async Task PostEventRecordAsync(string name, DateTime time, double value, string table)
        {
            using (var client = new HttpClient())
            {
                var requestUri = $"{_configuration["ApiUrl"]}/api/Api/eventRecord?name={name}&time={time}&value={value}&table={table}";

                // 手動構建 JSON 字符串
                var jsonContent = $"{{\"Name\":\"{name}\",\"Time\":\"{time:yyyy-MM-ddTHH:mm:ss}\",\"Value\":{value}}}";
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(requestUri, content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response: {responseContent}");
                }
                else {
                    Console.WriteLine($"Response: success!");
                }
            }
        }
        
        [HttpGet("GetEvent")]

        public IActionResult GetEvent(string table,DateTime time,string name)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            var eventData = new List<ChartDataModel>();

            try
            {
                using (var con = new MySqlConnection(connectionString))
                {

                    con.Open();
                    var query = new MySqlCommand($@"
                    select * from {table}
                    where 
                    Name in ({name}) and 
                    Time like @date
                    Order By
                    Name,Time,Value desc,Name;", con);


                    Console.WriteLine(query.CommandText);
                    query.Parameters.AddWithValue("@name", name);
                    if (time.TimeOfDay != new TimeSpan(0, 0, 0))
                    {
                        query.Parameters.AddWithValue("@date", time.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    else
                    {
                        query.Parameters.AddWithValue("@date", time.ToString("yyyy-MM-dd")+'%');
                    }
                    

                  

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            eventData.Add(new ChartDataModel
                            {
                                Name = reader["Name"].ToString(),
                                Datetime = reader["Time"].ToString(),
                                Value = Convert.ToDouble(reader["Value"])
                            });

                        }
                        Console.WriteLine(eventData.Count + "??????????");
                    }


                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while executing the database query: {ex.Message}", ex);
                throw;
            }


            return Ok(eventData);
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] Dictionary<string, string> data)
        {
            string name = data["name"];
            string email = data["email"];
            string pw = data["pw"];

            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            using (var con = new MySqlConnection(connectionString))
            {
                con.Open();
                var query = new MySqlCommand($@"
                INSERT INTO di_schemas.company (Name, registedTime) 
                SELECT @name, NOW()
                FROM DUAL
                WHERE NOT EXISTS (
                    SELECT 1 FROM di_schemas.company WHERE Name = @name
                );", con);
                query.Parameters.AddWithValue("@name", name);
                query.ExecuteNonQuery();
                var c_id = 0;
                query = new MySqlCommand(@"SELECT c_id FROM di_schemas.company WHERE Name = @name;",con);
                query.Parameters.AddWithValue("@name", name);
                using (var reader = query.ExecuteReader())
                {
                    while (reader.Read()) {  c_id = Convert.ToInt32(reader["c_id"]);}
                   
                }

                query = new MySqlCommand(@"SELECT COUNT(*) as c FROM di_schemas.users WHERE Email = @Email;", con);
                query.Parameters.AddWithValue("@Email",email);

                using(var reader = query.ExecuteReader())
                {
                    
                    while (reader.Read())
                    {
                        var count = Convert.ToInt32(reader["c"]);
                        if (count > 0)
                        {
                            return BadRequest(new { message = "Email already exists. Please use a different email address." });
                        }
                    }
                }

                query = new MySqlCommand($@"
                    INSERT IGNORE INTO di_schemas.users (c_id, Company, Email, Password, registedTime)
                    VALUES({c_id}, @name, @Email, @Pw, NOW());", con);

                query.Parameters.AddWithValue("@name", name);
                query.Parameters.AddWithValue("@Email", email);
                query.Parameters.AddWithValue("@Pw", pw);


          

               
                Console.WriteLine(query.CommandText);
                var rowsAffected = query.ExecuteNonQuery();
                _logger.LogInformation($"Rows affected: {rowsAffected}");

                return Ok(); // 返回成功响应
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] Dictionary<string, string> data)
        {
            var name = data["name"];
            var pw = data["pw"];
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            using (var con = new MySqlConnection(connectionString)) { 
            
                con.Open();
                var query = new MySqlCommand(@"
                select count(*) as c from di_schemas.users where Company = @name or Email = @name", con);
                query.Parameters.AddWithValue("@name", name);

                Console.WriteLine("query: "+query.CommandText+"  name:"+name );
                using (var reader = query.ExecuteReader())
                {
                    var count = 0;
                    while (reader.Read()) { 
                        count = Convert.ToInt32(reader["c"]);
                        Console.WriteLine("count:"+count);
                    }
                   
                    if (count > 0) {
                        query = new MySqlCommand(@"select count(*) as c from di_schemas.users where Password = @pw", con);
                        query.Parameters.AddWithValue("@pw", pw);
                        reader.Close();
                        using (var reader2 = query.ExecuteReader())
                        {
                            while (reader2.Read())
                            {
                                count = Convert.ToInt32(reader2["c"]);
                            }
                            if (count > 0) {
                                return Ok();
                            }
                            else
                            {
                                return BadRequest(new { message = "wrong password!" });
                            }

                        }
                    }
                    else
                    {
                        return BadRequest(new { message = "the account doesn't exist"});
                    }
                }
            
            
            
            }

        }


        [HttpGet("Demand")]

        public IActionResult GetDemandPower(DateTime start, DateTime end)
        {
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            var datas = new List<DP>();
            try
            {
                using (var con = new MySqlConnection(connectionString))
                {
                    con.Open();
                    var query = new MySqlCommand(@"select Max(Value) as value,Name,Time from di_schemas.powerevent where Date(Time) between @start and @end and Time(Time) between '02:00:00' and '23:59:00' and Name = 'TOTAL' Group by Date(Time);
                ", con);
                    query.Parameters.AddWithValue("@start", start);
                    query.Parameters.AddWithValue("@end", end);

                    Console.WriteLine("dates: " + start);

                    Console.WriteLine("query:" + query.CommandText);
                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine("name:" + reader["Name"].ToString());

                            datas.Add(new DP
                            {
                                Name = reader["Name"].ToString(),
                                Value = Convert.ToDouble(reader["value"].ToString()),
                                Datetime = Convert.ToDateTime(reader["Time"].ToString())

                            });


                        }


                    }

                }

                return Ok(datas);
            }
            catch (Exception ex) {

                _logger.LogError($"An error occurred while executing the database query: {ex.Message}", ex);
                throw;
            }
          
        }

    }
}
