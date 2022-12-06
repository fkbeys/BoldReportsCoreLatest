using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using System.Xml;
using System.Net.Http;
using BoldReports.ServerProcessor;
using System.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace BoldReportsCore.Models
{
    public sealed class ExternalServer : ReportingServer
    {
        string basePath;
        string reportResourcePath;
        string dataSourceResourcePath;
        string dataSetResourcePath;

        private Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;
        
        public string reportType
        {
            get;
            set;
        }

        public ExternalServer(Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            basePath = _hostingEnvironment.WebRootPath;
            basePath = Path.Combine(this.basePath, "Resources");
            this.reportResourcePath = Path.Combine(this.basePath, "Report");
            this.dataSourceResourcePath = Path.Combine(this.basePath, "DataSource");
            this.dataSetResourcePath = Path.Combine(this.basePath, "DataSet");
        }

        #region ReportViewer and ReportDesigner overrides


        public override System.IO.Stream GetReport()
        {
            string reportPath = Path.Combine(this.reportResourcePath, this.ReportPath.TrimStart('\\').TrimStart('/'));

            if (!reportPath.Contains(".rdl"))
            {
                reportPath = reportPath + ".rdl";
            }

            if (File.Exists(reportPath))
            {
                return this.ReadFiles(reportPath);
            }

            return null;
        }

        public override DataSourceDefinition GetDataSourceDefinition(string dataSource)
        {
            if (dataSource.Contains(".rds"))
            {
                dataSource = dataSource.Replace(".rds", "");
            }

            string dataSourcePath = Path.Combine(this.dataSourceResourcePath, dataSource.TrimStart('\\').TrimStart('/') + ".rds");

            if (File.Exists(dataSourcePath))
            {
                var _sharedDatasetInfo = new SharedDatasetinfo();
                var stream = this.ReadFiles(dataSourcePath);
                int length = Convert.ToInt32(stream.Length);
                byte[] data = new byte[length];
                stream.Read(data, 0, length);
                stream.Close();
                return this.GetDataSourceDefinition(data, dataSource, null);
            }

            return null;
        }

        public override SharedDatasetinfo GetSharedDataDefinition(string dataSet)
        {
            if (dataSet.Contains(".rsd"))
            {
                dataSet = dataSet.Replace(".rsd", "");
            }

            string dataSetPath = Path.Combine(this.dataSetResourcePath, dataSet.TrimStart('\\').TrimStart('/') + ".rsd");

            if (File.Exists(dataSetPath))
            {
                var _sharedDatasetInfo = new SharedDatasetinfo();
                var stream = this.ReadFiles(dataSetPath);
                int length = Convert.ToInt32(stream.Length);
                byte[] data = new byte[length];
                stream.Read(data, 0, length);
                stream.Close();
                var _datasetStream = this.GetFileToStream(data);
                _sharedDatasetInfo.DataSetStream = _datasetStream;
                _sharedDatasetInfo.Guid = Guid.Empty.ToString();
                return _sharedDatasetInfo;
            }

            return null;
        }

        public override byte[] GetItemDefinition(string itemName)
        {
            string imagePath = Path.Combine(this.basePath, itemName);

            if (File.Exists(imagePath))
            {
                using (FileStream fileStream = File.OpenRead(imagePath))
                {
                    fileStream.Position = 0;
                    MemoryStream memStream = new MemoryStream();
                    memStream.SetLength(fileStream.Length);
                    fileStream.Read(memStream.GetBuffer(), 0, (int)fileStream.Length);
                    memStream.Position = 0;
                    return memStream.ToArray();
                }
            }

            return null;
        }

        #endregion


        #region ReportDesigner overrides

        public override List<CatalogItem> GetItems(string folderName, ItemTypeEnum type)
        {
            List<CatalogItem> _items = new List<CatalogItem>();

            string targetFolder = string.Empty;

            if (type == ItemTypeEnum.Folder || type == ItemTypeEnum.Report)
            {
                targetFolder = this.reportResourcePath;
            }

            if (type == ItemTypeEnum.DataSet)
            {
                foreach (var file in Directory.GetFiles(this.dataSetResourcePath))
                {
                    CatalogItem catalogItem = new CatalogItem();
                    catalogItem.Name = Path.GetFileNameWithoutExtension(file);
                    catalogItem.Type = ItemTypeEnum.DataSet;
                    catalogItem.Id = Regex.Replace(catalogItem.Name, @"[^0-9a-zA-Z]+", "_");
                    _items.Add(catalogItem);
                }
            }
            else if (type == ItemTypeEnum.DataSource)
            {
                foreach (var file in Directory.GetFiles(this.dataSourceResourcePath))
                {
                    CatalogItem catalogItem = new CatalogItem();
                    catalogItem.Name = Path.GetFileNameWithoutExtension(file);
                    catalogItem.Type = ItemTypeEnum.DataSource;
                    catalogItem.Id = Regex.Replace(catalogItem.Name, @"[^0-9a-zA-Z]+", "_");
                    _items.Add(catalogItem);
                }
            }
            else if (type == ItemTypeEnum.Folder)
            {
                foreach (var file in Directory.GetDirectories(targetFolder))
                {
                    CatalogItem catalogItem = new CatalogItem();
                    catalogItem.Name = Path.GetFileNameWithoutExtension(file);
                    catalogItem.Type = ItemTypeEnum.Folder;
                    catalogItem.Id = Regex.Replace(catalogItem.Name, @"[^0-9a-zA-Z]+", "_");
                    _items.Add(catalogItem);
                }
            }
            else if (type == ItemTypeEnum.Report)
            {
                string reportTypeExt = this.reportType == "RDLC" ? ".rdlc" : ".rdl";

                foreach (var file in Directory.GetFiles(targetFolder, "*" + reportTypeExt))
                {
                    CatalogItem catalogItem = new CatalogItem();
                    catalogItem.Name = Path.GetFileNameWithoutExtension(file);
                    catalogItem.Type = ItemTypeEnum.Report;
                    catalogItem.Id = Regex.Replace(catalogItem.Name, @"[^0-9a-zA-Z]+", "_");
                    _items.Add(catalogItem);
                }
            }

            return _items;
        }

        public override bool CreateReport(string reportName, string folderName, byte[] reportdata, out string exception)
        {
            exception = null;
            reportName = reportName.TrimStart('\\').TrimStart('/');

            if (!Path.HasExtension(reportName))
            {
                reportName += ".rdl";
            }

            string reportPath = Path.Combine(this.reportResourcePath, reportName);
            File.WriteAllBytes(reportPath, reportdata.ToArray());

            return true;
        }

        #endregion

        #region External Server Helpers

        private Stream ReadFiles(string filePath)
        {
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                fileStream.Position = 0;
                MemoryStream memStream = new MemoryStream();
                memStream.SetLength(fileStream.Length);
                fileStream.Read(memStream.GetBuffer(), 0, (int)fileStream.Length);
                return memStream;
            }
        }

        DataSourceDefinition GetDataSourceDefinition(byte[] dataSourceContent, string name, string guid)
        {
            var _dataSourceDefinition = new DataSourceDefinition();
            var _datasourceStream = this.GetFileToStream(dataSourceContent);
            StreamReader reader = new StreamReader(_datasourceStream);
            string text = reader.ReadToEnd();
            _datasourceStream.Position = 0;

            if (text.Contains("RptDataSource"))
            {
                var _ssrsDatasourceDefinition = this.DeseralizeObj<RptDataSource>(_datasourceStream);
                _dataSourceDefinition = this.GetSSRSSharedDataSourceDef(_ssrsDatasourceDefinition);
            }
            else
            {
                var _brDatasourceDefinition = this.DeseralizeObj<DataSourceDefinition>(_datasourceStream);
                _dataSourceDefinition = _brDatasourceDefinition;
            }

            return _dataSourceDefinition;
        }

        DataSourceDefinition GetSSRSSharedDataSourceDef(RptDataSource rptDataSourceDefinition)
        {
            var rptDefinition = new DataSourceDefinition();
            rptDefinition.ConnectString = rptDataSourceDefinition.ConnectionProperties.ConnectString;
            rptDefinition.Name = rptDataSourceDefinition.ConnectionProperties.Name;
            rptDefinition.WindowsCredentials = rptDataSourceDefinition.ConnectionProperties.WindowsCredentials;
            rptDefinition.UserName = rptDataSourceDefinition.ConnectionProperties.UserName;
            rptDefinition.UseOriginalConnectString = rptDataSourceDefinition.ConnectionProperties.UseOriginalConnectString;
            rptDefinition.Prompt = rptDataSourceDefinition.ConnectionProperties.Prompt;
            rptDefinition.Password = rptDataSourceDefinition.ConnectionProperties.Password;
            rptDefinition.Guid = rptDataSourceDefinition.ConnectionProperties.Guid;
            rptDefinition.OriginalConnectStringExpressionBased = rptDataSourceDefinition.ConnectionProperties.OriginalConnectStringExpressionBased;
            rptDefinition.ImpersonateUser = rptDataSourceDefinition.ConnectionProperties.ImpersonateUser;
            rptDefinition.Extension = rptDataSourceDefinition.ConnectionProperties.Extension;
            rptDefinition.EnabledSpecified = rptDataSourceDefinition.ConnectionProperties.EnabledSpecified;
            rptDefinition.Enabled = rptDataSourceDefinition.ConnectionProperties.Enabled;
            rptDefinition.CredentialRetrieval = rptDataSourceDefinition.ConnectionProperties.CredentialRetrieval;
            rptDefinition.ConnectString = rptDataSourceDefinition.ConnectionProperties.ConnectString;
            rptDefinition.ImpersonateUserSpecified = rptDataSourceDefinition.ConnectionProperties.ImpersonateUserSpecified;
            return rptDefinition;
        }

        T DeseralizeObj<T>(Stream str)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlReader reader = XmlReader.Create(str);
            return (T)serializer.Deserialize(reader);
        }

        private Stream GetFileToStream(byte[] _fileContent)
        {
            MemoryStream memStream = new MemoryStream();
            memStream.Write(_fileContent, 0, _fileContent.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            return memStream;
        }

        #endregion
    }


    public class RptDataSource
    {
        public ConnectionProperties ConnectionProperties { get; set; }
    }

    public class ConnectionProperties
    {
        public string Name { get; set; }
        public bool WindowsCredentials { get; set; }
        public string UserName { get; set; }
        public bool UseOriginalConnectString { get; set; }
        public string Prompt { get; set; }
        public string Password { get; set; }
        public string Guid { get; set; }
        public bool OriginalConnectStringExpressionBased { get; set; }
        public bool ImpersonateUser { get; set; }
        public string Extension { get; set; }
        public bool EnabledSpecified { get; set; }
        public bool Enabled { get; set; }
        public CredentialRetrievalEnum CredentialRetrieval { get; set; }
        public string ConnectString { get; set; }
        public bool ImpersonateUserSpecified { get; set; }
    }
}
