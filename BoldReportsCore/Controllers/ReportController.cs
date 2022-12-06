using BoldReportsCore.Helper;
using BoldReportsCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BoldReportsCore.Controllers
{
    [Route("api/[controller]/[action]")]
    public class ReportController : ControllerBase
    {
        private Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;
        private string rootPath;

        public ReportController( Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            this.rootPath = _hostingEnvironment.WebRootPath;
        }

        [ActionName("GetReports")]
        [HttpPost]
        public object GetReports()
        {
            ReportModel reportModel = new ReportModel(_hostingEnvironment.WebRootPath + "/Resources/Report");
            return reportModel.GetReports();
        }

        [HttpGet]
        [ActionName("DataModel")]
        public object DataModel(bool sharedDataSet, string dataSetName)
        {
            ReportDataHelper helper = new ReportDataHelper(this.rootPath);
            return helper.GetData(sharedDataSet, dataSetName); ;
        }
    }
}
