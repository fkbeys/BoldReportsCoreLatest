using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoldReports.Web.ReportViewer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using BoldReports.Writer;
using BoldReportsCore.Models;
using BoldReports.Web;
using System.Reflection;

namespace BoldReportsCore.Controllers
{
    [Route("api/[controller]/[action]")]
    public class ReportViewerController : ControllerBase, IReportController, IReportHelperSettings
    {
        private Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;

        internal ExternalServer Server { get; set; }

        public string ServerURL { get; set; }

        public ICredentials ReportServerCredential { get; set; }

        public ReportViewerController(Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment)
        {
            _cache = memoryCache;
            _hostingEnvironment = hostingEnvironment;
            this.Server = new ExternalServer(_hostingEnvironment);
            this.ServerURL = "<dummy>";
            this.ReportServerCredential = new System.Net.NetworkCredential("dummy", "dummy");
        }

        public void InitializeSettings(ReportHelperSettings helperSettings)
        {
            helperSettings.ReportingServer = this.Server;
        }

        [HttpPost]
        public object PostReportAction([FromBody] Dictionary<string, object> jsonResult)
        {
            return ReportHelper.ProcessReport(jsonResult, this, this._cache);
        }

        [ActionName("GetResource")]
        [AcceptVerbs("GET")]
        [AllowAnonymous]
        public object GetResource(ReportResource resource)
        {
            return ReportHelper.GetResource(resource, this, _cache);
        }

        [HttpPost]
        [AllowAnonymous]
        public object PostFormReportAction()
        {
            return ReportHelper.ProcessReport(null, this, this._cache);
        }

        public void OnInitReportOptions(ReportViewerOptions reportOption)
        {
            reportOption.ReportModel.ReportServerUrl = this.ServerURL;
            reportOption.ReportModel.ReportServerCredential = this.ReportServerCredential;
        }

        public void OnReportLoaded(ReportViewerOptions reportOption)
        {
        }

        [HttpGet]
        [ActionName("Export")]
        public IActionResult Export(string name)
        {
            BoldReports.Writer.ReportWriter writer = new BoldReports.Writer.ReportWriter();
            writer.ReportingServer = this.Server;
            writer.ReportServerUrl = this.ServerURL;
            writer.ReportServerCredential = this.ReportServerCredential;
            writer.ReportPath = name;
            MemoryStream memoryStream = new MemoryStream();
            writer.Save(memoryStream, WriterFormat.PDF);
            memoryStream.Position = 0;
            // Download the generated export document to the client side.
            memoryStream.Position = 0;
            FileStreamResult fileStreamResult = new FileStreamResult(memoryStream, "application/pdf");
            fileStreamResult.FileDownloadName = name + ".pdf";
            return fileStreamResult;
        }
    }
}