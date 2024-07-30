using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using web_MVC.Models;

namespace web_MVC.Controllers
{
    public class ChartController : Controller
    {
        private readonly string connectionString = "server=127.0.0.1;database=di_schemas;user id=root;port=3306;password=123456;AllowLoadLocalInfile=true;";

        public IActionResult Index()
        {
            var model = new ChartViewModel
            {
                StartDate = DateTime.Today.ToString("yyyy-MM-dd"),
                EndDate = DateTime.Today.ToString("yyyy-MM-dd")
            };
            return View(model);
        }

     

        [HttpPost]
        public IActionResult LoadData(ChartViewModel model)
        {
            if (string.IsNullOrEmpty(model.StartDate) || string.IsNullOrEmpty(model.EndDate) || model.SelectedNames == null || model.SelectedNames.Count == 0)
            {
                ViewBag.ErrorMessage = "Please provide all required inputs.";
                return View("Index", model);
            }

            if (!DateTime.TryParse(model.StartDate, out DateTime dateS) || !DateTime.TryParse(model.EndDate, out DateTime dateE))
            {
                ViewBag.ErrorMessage = "Please enter valid dates.";
                return View("Index", model);
            }

            if (dateS > dateE)
            {
                ViewBag.ErrorMessage = "Start date cannot be after end date.";
                return View("Index", model);
            }

            var chartData = GetData(model.SelectedOption, dateS, dateE, model.SelectedNames);
            return Json(chartData);
        }

        private List<ChartDataModel> GetData(string option, DateTime dateS, DateTime dateE, List<string> names)
        {
            var chartData = new List<ChartDataModel>();

            using (var con = new MySqlConnection(connectionString))
            {
                con.Open();

                foreach (var name in names)
                {
                    Console.WriteLine("@@@@@@"+name);
                    if (option == "power")
                    {
                        chartData.AddRange(GetPowerData(con, dateS, dateE, name));
                    }
                    else if (option == "energy")
                    {
                        chartData.AddRange(GetEnergyData(con, dateS, dateE, name));
                    }
                    else if (option == "Mix")
                    {
                        chartData.AddRange(GetPowerData(con, dateS, dateE, name));
                        chartData.AddRange(GetEnergyData(con, dateS, dateE, name));
                    }
                }
            }

            return chartData;
        }

        private List<ChartDataModel> GetPowerData(MySqlConnection con, DateTime dateS, DateTime dateE, string name)
        {
            var powerData = new List<ChartDataModel>();
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
                    powerData.Add(new ChartDataModel
                    {
                        Name = reader["Name"].ToString(),
                        Value = Convert.ToDouble(reader["max_value"]),
                        Datetime = reader["Datetime"].ToString()
                    });
                }
            }

            return powerData;
        }

        private List<ChartDataModel> GetEnergyData(MySqlConnection con, DateTime dateS, DateTime dateE, string name)
        {
            var energyData = new List<ChartDataModel>();
            var query2 = new MySqlCommand($@"
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

            query2.Parameters.AddWithValue("@DateS", dateS.ToString("yyyy-MM-dd")+"00:00:00");
            query2.Parameters.AddWithValue("@DateE", dateE.ToString("yyyy-MM-dd")+"23:59:00");
            query2.Parameters.AddWithValue("@Name", name + "_KWH");

            using (var reader = query2.ExecuteReader())
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

            return energyData;
        }
    }
}
