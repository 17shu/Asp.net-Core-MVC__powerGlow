using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using web_MVC.Models;
using System.Windows;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;

namespace web_MVC.Controllers
{ 
    public class ChartViewController : Controller
    {
        private readonly string connectionString = "server=127.0.0.1;database=di_schemas;user id=root;port=3306;password=123456;AllowLoadLocalInfile=true;";
        private static Timer _timer;
        private static List<ChartDataModel> _cachedPowerData = new List<ChartDataModel>();
        private static List<ChartDataModel> _cachedEnergyData = new List<ChartDataModel>();
        private static int currentMinute = 0; // 初始化為當天的第一分鐘
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;


        public ChartViewController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;


        }
  
     

        public IActionResult Index()
        {
            return View();
        }

        

          public async Task<IActionResult> getPowerData(int currentMin)
        {
            var result = await FetchPowerData(currentMin);

            if (result is ContentResult contentResult)
            {
                return Content(contentResult.Content, "application/json");
            }
            else if (result is ObjectResult objectResult)
            {
                return Json(objectResult.Value);
            }
            else
            {
                return StatusCode(500, "Failed to fetch power data.");
            }
        }
        [HttpGet("GetPowerData")]
     
        public async Task<IActionResult> FetchPowerData(int currentMin)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_configuration["ApiUrl"]}/api/Api/GetPowerData?minuteOfDay={currentMin}";

            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }

            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

      public async Task<IActionResult> getEnergyData(int currentMin)
        {
            var result = await FetchEnergyData(currentMin);

            if (result is ContentResult contentResult)
            {
                return Content(contentResult.Content, "application/json");
            }
            else if (result is ObjectResult objectResult)
            {
                return Json(objectResult.Value);
            }
            else
            {
                return StatusCode(500, "Failed to fetch energy data.");
            }
        }
        [HttpGet("GetEnergyData")]
  
        public async Task<IActionResult> FetchEnergyData(int currentMin)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_configuration["ApiUrl"]}/api/Api/GetEnergyData?minuteOfDay={currentMin}";

            var response = await client.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsStringAsync();
                return Content(data, "application/json");
            }

            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }

    }
}
