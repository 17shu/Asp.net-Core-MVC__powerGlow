using Microsoft.AspNetCore.Mvc;
using Mysqlx.Cursor;
using Newtonsoft.Json;
using System.Diagnostics;
using web_MVC.Models;

namespace web_MVC.Controllers
{

  
        public class AIController : Controller
        {
            private readonly HttpClient _httpClient;

            public AIController(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            [HttpGet]
            public IActionResult Index()
            {
                ViewData["ShowSidebar"] = true;
                return View();
            }

            [HttpPost]
            public async Task<IActionResult> Index(string question)
            {
                // 將問題作為 URL 查詢參數發送
                var response = await _httpClient.GetAsync($"http://192.168.8.190/api/Sql/generate?question={Uri.EscapeDataString(question)}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonConvert.DeserializeObject<SqlResponse>(result);

                    ViewBag.SqlQuery = jsonResponse.SqlQuery;
                    ViewBag.Data = jsonResponse.Data;

                    // Extract column names for dropdowns
                    var columns = jsonResponse.Data.FirstOrDefault()?.Keys.ToList() ?? new List<string>();
                    ViewBag.Columns = columns;

                    return View("Index");
                }
                else
                {
                    ViewBag.Error = "Error executing SQL.";
                    return View("Index");
                }
            }

            public class SqlResponse
            {
                public string SqlQuery { get; set; }
                public IEnumerable<IDictionary<string, object>> Data { get; set; }
            }
        }
    


}
