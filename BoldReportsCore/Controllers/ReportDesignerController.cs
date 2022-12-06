using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using BoldReports.Web.ReportViewer;
using BoldReports.Web.ReportDesigner;
using System.Net;
using BoldReportsCore.Models;
using BoldReports.Web;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace BoldReportsCore.Controllers
{
    [Route("api/[controller]/[action]")]
    public class ReportDesignerController : ControllerBase, BoldReports.Web.ReportDesigner.IReportDesignerController, IReportHelperSettings
    {
        private Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;
        private Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;

        internal ExternalServer Server { get; set; }

        public string ServerURL { get; set; }

        public ICredentials ReportServerCredential { get; set; }

        public ReportDesignerController(Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment)
        {
            _cache = memoryCache;
            _hostingEnvironment = hostingEnvironment;
            this.Server = new ExternalServer(_hostingEnvironment);
            this.ServerURL = "<dummy>";
            this.ReportServerCredential = new System.Net.NetworkCredential("dummy", "dummy");
        }

        [NonAction]
        public void InitializeSettings(ReportHelperSettings helperSettings)
        {
            helperSettings.ReportingServer = this.Server;
        }

        [NonAction]
        private string GetFilePath(string itemName, string key)
        {
            string targetFolder = this._hostingEnvironment.WebRootPath + "/";
            targetFolder += "Cache";

            if (!System.IO.Directory.Exists(targetFolder))
            {
                System.IO.Directory.CreateDirectory(targetFolder);
            }

            if (!System.IO.Directory.Exists(targetFolder + "/" + key))
            {
                System.IO.Directory.CreateDirectory(targetFolder + "/" + key);
            }

            return targetFolder + "/" + key + "/" + itemName;
        }

        [HttpGet]
        public object GetImage(string key, string image)
        {
            return ReportDesignerHelper.GetImage(key, image, this);
        }

        [HttpGet]
        public object GetResource(ReportResource resource)
        {
            return ReportHelper.GetResource(resource, this, _cache);
        }

        [NonAction]
        public void OnInitReportOptions(ReportViewerOptions reportOption)
        {
            reportOption.ReportModel.ReportServerUrl = this.ServerURL;
            reportOption.ReportModel.ReportServerCredential = this.ReportServerCredential;
        }

        [NonAction]
        public void OnReportLoaded(ReportViewerOptions reportOption)
        {
            //You can update report options here
        }

        [HttpPost]
        public object PostDesignerAction([FromBody] Dictionary<string, object> jsonResult)
        {
            return ReportDesignerHelper.ProcessDesigner(jsonResult, this, null, this._cache);
        }

        [HttpPost]
        public object PostFormDesignerAction()
        {
            return ReportDesignerHelper.ProcessDesigner(null, this, null, this._cache);
        }

        [HttpPost]
        public object PostFormReportAction()
        {
            return ReportHelper.ProcessReport(null, this, this._cache);
        }

        [HttpPost]
        public object PostReportAction([FromBody] Dictionary<string, object> jsonResult)
        {
            return ReportHelper.ProcessReport(jsonResult, this, this._cache);
        }

        [NonAction]
        public bool SetData(string key, string itemId, ItemInfo itemData, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (itemData.Data != null)
            {
                System.IO.File.WriteAllBytes(this.GetFilePath(itemId, key), itemData.Data);
            }
            else if (itemData.PostedFile != null)
            {
                var fileName = itemId;
                if (string.IsNullOrEmpty(itemId))
                {
                    fileName = System.IO.Path.GetFileName(itemData.PostedFile.FileName);
                }

                using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                {
                    itemData.PostedFile.OpenReadStream().CopyTo(stream);
                    byte[] bytes = stream.ToArray();
                    var writePath = this.GetFilePath(fileName, key);

                    System.IO.File.WriteAllBytes(writePath, bytes);
                    stream.Close();
                    stream.Dispose();
                }
            }

            return true;
        }

        [NonAction]
        public ResourceInfo GetData(string key, string itemId)
        {
            var resource = new ResourceInfo();
            resource.Data = System.IO.File.ReadAllBytes(this.GetFilePath(itemId, key));
            return resource;
        }

        [HttpPost]
        public void UploadReportAction()
        {
            ReportDesignerHelper.ProcessDesigner(null, this, this.Request.Form.Files[0], this._cache);
        }
    }
}

