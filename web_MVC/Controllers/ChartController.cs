using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using web_MVC.Models;

namespace web_MVC.Controllers
{
    public class ChartController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ChartController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IActionResult Index()
        {
            var model = new ChartViewModel
            {
                StartDate = DateTime.Today.ToString("yyyy-MM-dd"),
                EndDate = DateTime.Today.ToString("yyyy-MM-dd")
            };
            ViewData["ShowSidebar"] = true;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LoadData(ChartViewModel model)
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

            var chartData = await GetData(model.SelectedOption, dateS, dateE, model.SelectedNames);
            return Json(chartData);
        }

        private async Task<List<ChartDataModel>> GetData(string option, DateTime dateS, DateTime dateE, List<string> names)
        {
            var chartData = new List<ChartDataModel>();

            foreach (var name in names)
            {
                if (option == "power")
                {
                    var powerData = await GetData(dateS, dateE, name,"GetPowerHis");
                    chartData.AddRange(powerData);
                }
                else if (option == "energy")
                {
                    var energyData = await GetData(dateS, dateE, name, "GetEnergyHis");
                    chartData.AddRange(energyData);
                }
                else if (option == "Mix")
                {
                    var powerData = await GetData(dateS, dateE, name, "GetPowerHis");
                    var energyData = await GetData(dateS, dateE, name, "GetEnergyHis");
                    chartData.AddRange(powerData);
                    chartData.AddRange(energyData);
                }
            }

            return chartData;
        }

        public async Task<List<ChartDataModel>> GetData(DateTime dateS, DateTime dateE, string name,string option)
        {
            try
            {
                var result = await FetchData(dateS, dateE, name,option);
                if (result is ContentResult contentResult)
                {
                    var jsonData = contentResult.Content;
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // 忽略属性名称的大小写
                    };

                    var chartData = JsonSerializer.Deserialize<List<ChartDataModel>>(jsonData, options);

                    Console.WriteLine("??????????????"+chartData);
                    return chartData ?? new List<ChartDataModel>();
                }
            }
            catch (Exception ex)
            {
                // 记录错误日志
                Console.WriteLine($"Error fetching power data: {ex.Message}");
            }

            return new List<ChartDataModel>();
        }

        [HttpGet]
        public async Task<IActionResult> FetchData(DateTime dateS, DateTime dateE, string name, string option)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var apiUrl = $"{_configuration["ApiUrl"]}/api/Api/{option}?dateS={dateS}&dateE={dateE}&name={name}";

                Console.WriteLine("url~~~~~~   " + apiUrl);

                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    return Content(data, "application/json");
                }

                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                // 记录错误日志
                Console.WriteLine($"Error fetching power data: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching data.");
            }
        }

      
    }
}
