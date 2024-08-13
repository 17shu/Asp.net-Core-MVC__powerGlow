using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

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
            Console.WriteLine("!!!!!!!!" + _configuration.GetConnectionString("MySqlConnection"));
            _logger = logger;
        }

        [HttpGet("GetPowerData")]

        public IActionResult GetPowerData(int minuteOfDay)
        {
            Console.WriteLine("API!!!!!!!!!!!!!!!!!!");
            try
            {
                var powerData = FetchPowerData(minuteOfDay);
                return Ok(powerData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private List<ChartDataModel> FetchPowerData(int minuteOfDay)
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
                    var query = new MySqlCommand(@"
                    SELECT DISTINCT Name, Value AS value, 
                    DATE_FORMAT(Datetime, '%Y-%m-%d %H:%i:00') AS Datetime
                    FROM di_schemas.powerdata_dmpower 
                    WHERE DATE(Datetime) = '2024-05-03' AND HOUR(Datetime) * 60 + MINUTE(Datetime) = @MinuteOfDay
                    GROUP BY Name, DATE_FORMAT(Datetime, '%Y-%m-%d %H:%i:00')
                    ORDER BY Datetime;", con);
                    query.Parameters.AddWithValue("@MinuteOfDay", minuteOfDay);

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            powerdata.Add(new ChartDataModel
                            {
                                Name = reader["Name"].ToString().Split('_')[0],
                                Value = Convert.ToDouble(reader["value"]),
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

            return powerdata;

        }
        [HttpGet("GetEnergyData")]

        public IActionResult GetEnergyData(int minuteOfDay)
        {
            Console.WriteLine("API!!!!!!!!!!!!!!!!!!");
            try
            {
                var energyData = FetchEnergyData(minuteOfDay);
                return Ok(energyData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private List<ChartDataModel> FetchEnergyData(int minuteOfDay)
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
                    DATE(t1.Datetime) = '2024-05-23' AND 
                    HOUR(t1.Datetime) * 60 + MINUTE(t1.Datetime) = @MinuteOfDay
                GROUP BY 
                    t1.Name, 
                    Datetime;", con);
                    query.Parameters.AddWithValue("@MinuteOfDay", minuteOfDay);

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            energyData.Add(new ChartDataModel
                            {
                                Name = reader["Name"].ToString().Split('_')[0],
                                Value = Convert.ToDouble(reader["diff"]),
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

        [HttpGet("GetPowerHis")]
        public IActionResult GetPowerHisData(DateTime dateS, DateTime dateE, string name)
        {
            Console.WriteLine("API!!!!!!!!!!!!!!!!!!");
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
                        SELECT DISTINCT
                            t1.Name as Name,
                            t1.Value AS max_value,
                            t1.Datetime 
                        FROM 
                            di_schemas.powerdata_dmpower t1
                        JOIN (
                            SELECT DISTINCT
                                DATE(Datetime) AS date,
                                MAX(CAST(Value AS DOUBLE)) AS max_value
                            FROM 
                                di_schemas.powerdata_dmpower
                            WHERE 
                                Datetime BETWEEN @DateS AND @DateE
                                AND Name = @Name
                            GROUP BY 
                                DATE(Datetime)
                        ) t2 ON DATE(t1.Datetime) = t2.date 
                           AND t1.Value = t2.max_value
                        GROUP BY
                            t2.date
                        ORDER BY 
                            t1.Datetime;", con);
                    query.Parameters.AddWithValue("@DateS", dateS.ToString("yyyy-MM-dd") + " 00:00:00");
                    query.Parameters.AddWithValue("@DateE", dateE.ToString("yyyy-MM-dd") + " 23:59:59");
                    query.Parameters.AddWithValue("@Name", name + "_Demand_KW");

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            powerdata.Add(new ChartDataModel
                            {
                                Name = reader["Name"].ToString(),
                                Value = Convert.ToDouble(reader["max_value"]),
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
        public IActionResult GetPowerHistoryData(DateTime date, string name)
        {
            try
            {
                var powerData = FetchPowerHistoryData(date, name);
                return Ok(powerData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private List<ChartDataModel> FetchPowerHistoryData(DateTime date, string names)
        {
            Console.WriteLine("fetch!!!!!!!!!!!!????????" + string.Join(",", names));
            var powerData = new List<ChartDataModel>();
            var connectionString = _configuration.GetConnectionString("MySqlConnection");
            Console.WriteLine(">>>>>>>>" + date + "<<<<<<<<<<<" + string.Join(",", names));

            try
            {
                using (var con = new MySqlConnection(connectionString))
                {
                    con.Open();

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


                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string color = "";
                            double diff = Convert.ToDouble(reader["Diff"]);
                            if (diff >= 0.5) { color = "#FF0000"; }
                            else if (diff <= -0.5) { color = "#0066CC"; }
                            else { color = ""; }
                            powerData.Add(new ChartDataModel
                            {
                                Name = reader["Name"].ToString(),
                                Value = Convert.ToDouble(reader["Value"]),
                                Datetime = reader["Datetime"].ToString(),
                                Diff = diff,
                                color=color
                            });
                        }
                        Console.WriteLine(powerData + "!!!!!!!!!!");
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

        public IActionResult GetEnergyHistory(DateTime date, string name)
        {
            Console.WriteLine("API!!!!!!!!!!!!!!!!!!");
            try
            {
                var energyData = FetchEnergyHistory(date, name);
                return Ok(energyData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while fetching power data: {ex.Message}", ex);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

        }

        private List<ChartDataModel> FetchEnergyHistory(DateTime date, string name)
        {
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

                    Console.WriteLine("Generated SQL Query: " + query.CommandText);
                    foreach (MySqlParameter parameter in query.Parameters)
                    {
                        Console.WriteLine($"{parameter.ParameterName}: {parameter.Value}");
                    }

                    // 使用字典來保存每個 Name 的上一個值
                    var previousValues = new Dictionary<string, double>();

                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
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

                            // 更新該名稱的前一個值
                            previousValues[currentName] = currentValue;
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



    }
}
