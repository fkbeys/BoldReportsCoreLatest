using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BoldReportsCore.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace BoldReportsCore.Controllers
{
    public class HomeController : Controller
    {
        private IMemoryCache _cache;
        private IWebHostEnvironment _hostingEnvironment;

        public HomeController(IMemoryCache memoryCache, IWebHostEnvironment hostingEnvironment)
        {
            _cache = memoryCache;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index()
        {
            ReportModel reportModel = new ReportModel(_hostingEnvironment.WebRootPath + "/Resources/Report");
            ViewBag.Model = reportModel.GetReports();
            return View();
        }

        public IActionResult ViewReport(string name)
        {
            ViewBag.ReportName = name;
            return View();
        }

        public IActionResult DesignReport(string name)
        {
            ViewBag.ReportName = name;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
