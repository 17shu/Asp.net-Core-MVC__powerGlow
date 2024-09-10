using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using web_MVC.Models;


namespace web_MVC.Controllers
{
    public class HistoryController:Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public HistoryController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        public IActionResult Index()
        {
            var model = new ChartViewModel();
            ViewData["ShowSidebar"] = true;
            return View(model);
        }

        public IActionResult Control()
        {
            return View();
        }
    

        [HttpGet]
        public async Task<IActionResult> GetHistory(string powerTool,string enTool, DateTime date)
        {
            var client = _httpClientFactory.CreateClient();

       


            var apiUrl = $"{_configuration["ApiUrl"]}/api/Api/GetPowerHistory?date={date:MM/dd/yyyy}&name={powerTool}";
            var apiUrl2 = $"{_configuration["ApiUrl"]}/api/Api/GetEnergyHistory?date={date:MM/dd/yyyy}&name={enTool}";

            Console.WriteLine(apiUrl);
            Console.WriteLine("/////////////" + apiUrl2);

            var response1 = await client.GetAsync(apiUrl);
            var response2 = await client.GetAsync(apiUrl2);

            if (response1.IsSuccessStatusCode && response2.IsSuccessStatusCode)
            {
                var jsonData1 = await response1.Content.ReadAsStringAsync();
                var jsonData2 = await response2.Content.ReadAsStringAsync();

                var combinedJsonData = $"{{\"powerData\": {jsonData1}, \"energyData\": {jsonData2}}}";

                return Content(combinedJsonData, "application/json");
            }

            return StatusCode((int)response1.StatusCode, "Failed to fetch history data.");
        }

    }
}
