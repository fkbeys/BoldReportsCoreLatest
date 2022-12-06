using BoldReports.Utilities;
using BoldReports.Web.ReportDesigner.Internal.Util;
using BoldReportsCore.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace BoldReportsCore.Helper
{
    public class ReportDataHelper
    {
        public string resourcePath;

        public ReportDataHelper(string path)
        {
            this.resourcePath = path;
        }

        T DeseralizeObj<T>(Stream str)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlReader reader = XmlReader.Create(str);
            return (T)serializer.Deserialize(reader);
        }

        public BoldReports.RDL.DOM.Server.SharedDataSet GetDataSetFromFile(string dataSetName)
        {
            BoldReports.RDL.DOM.ReportSerializer serializer = new BoldReports.RDL.DOM.ReportSerializer();
            BoldReports.RDL.DOM.Server.SharedDataSet serverDataSet = null;

            using (FileStream fsSource = new FileStream(this.resourcePath + "/Resources/DataSet/"  + dataSetName +".rsd",
            FileMode.Open, FileAccess.Read))
            {
                //  serverDataSet = this.DeseralizeObj<BoldReports.RDL.DOM.Server.SharedDataSet>(fsSource);
                serverDataSet = serializer.GetSharedDataSet(fsSource);
            }

            return serverDataSet;
        }

        public BoldReports.ServerProcessor.DataSourceDefinition GetDataSourceFromFile(string dataSourceName)
        {
            BoldReports.ServerProcessor.DataSourceDefinition serverDataSource = null;

            using (FileStream fsSource = new FileStream(this.resourcePath + "/Resources/DataSource/" + dataSourceName + ".rds",
           FileMode.Open, FileAccess.Read))
            {
                serverDataSource = this.DeseralizeObj<BoldReports.ServerProcessor.DataSourceDefinition>(fsSource);
            }

            return serverDataSource;
        }

        public string GetSharedDataSource(string dataSourceName, string path)
        {
            BoldReports.ServerProcessor.DataSourceDefinition serverDataSource = null;

            using (FileStream fsSource = new FileStream(path,
            FileMode.Open, FileAccess.Read))
            {
                serverDataSource = this.DeseralizeObj<BoldReports.ServerProcessor.DataSourceDefinition>(fsSource);
            }

            BoldReports.RDL.DOM.DataSource datasource = new BoldReports.RDL.DOM.DataSource();
            datasource.Name = dataSourceName;
            datasource.ConnectionProperties = new BoldReports.RDL.DOM.ConnectionProperties();
            datasource.ConnectionProperties.ConnectString = serverDataSource.ConnectString;
            datasource.ConnectionProperties.DataProvider = "WebAPI";
            return this.SerializeDOM(datasource);
        }

        public ReportDesignerData GetData(bool sharedDataSet, string dataSetname)
        {
            List<BoldReports.RDL.DOM.ReportParameter> parameters = new List<BoldReports.RDL.DOM.ReportParameter>();

            ReportDesignerData data = new ReportDesignerData();
            string[] dataSets = dataSetname.Split(',');

            foreach (var dataSet in dataSets)
            {
                var dataSetFile = this.GetDataSetFromFile(dataSet);

                if (!sharedDataSet)
                {
                    data.DataSources.Add(this.GetDataSource(dataSetFile.DataSet.Query.DataSourceReference));
                }

                data.DataSets.Add(this.GetDataSet(sharedDataSet, dataSetFile, dataSet));

                var domParameters = this.GetParamaters(dataSetFile);

                if (domParameters != null && domParameters.Count > 0)
                {
                    parameters.AddRange(domParameters);
                }
            }

            List<string> paramNames = new List<string>();

            if (parameters != null && parameters.Count > 0)
            {
                foreach (var param in parameters)
                {
                    if (!paramNames.Contains(param.Name))
                    {
                        paramNames.Add(param.Name);
                        data.Parameters.Add(this.SerializeDOM(param));
                    }
                }
            }

            return data;
        }

        public string GetDataSource(string dataSourceName)
        {
            var dataSetInfoFromFile = this.GetDataSourceFromFile(dataSourceName);
            BoldReports.RDL.DOM.DataSource datasource = new BoldReports.RDL.DOM.DataSource();
            datasource.Name = dataSourceName;
            datasource.ConnectionProperties = new BoldReports.RDL.DOM.ConnectionProperties();
            datasource.ConnectionProperties.ConnectString = dataSetInfoFromFile.ConnectString;
            datasource.ConnectionProperties.DataProvider = dataSetInfoFromFile.Extension;

            if(!string.IsNullOrEmpty(dataSetInfoFromFile.Password))
            {
                datasource.SecurityType = BoldReports.RDL.DOM.SecurityType.Integrated;
                datasource.ConnectionProperties.UserName = dataSetInfoFromFile.UserName;
                datasource.ConnectionProperties.PassWord = dataSetInfoFromFile.Password;
            }

            return this.SerializeDOM(datasource);
        }

        public List<BoldReports.RDL.DOM.ReportParameter> GetParamaters(BoldReports.RDL.DOM.Server.SharedDataSet dataSetInfoFromFile)
        {
            List<BoldReports.RDL.DOM.ReportParameter> parameters = new List<BoldReports.RDL.DOM.ReportParameter>();

            if (dataSetInfoFromFile.DataSet.Query.DataSetParameters != null)
            {
                foreach (var dataSetParam in dataSetInfoFromFile.DataSet.Query.DataSetParameters)
                {
                    BoldReports.RDL.DOM.ReportParameter param = new BoldReports.RDL.DOM.ReportParameter();
                    param.Name = dataSetParam.Name;
                    param.Prompt = param.Name;
                    param.DataType = BoldReports.RDL.DOM.DataTypes.String;
                    string dataType = dataSetParam.DbType;

                    if (dataType.Equals("system.datetime"))
                    {
                        param.DataType = BoldReports.RDL.DOM.DataTypes.DateTime;
                    }
                    else if (dataType.Equals("system.single") || dataType.Equals("system.decimal"))
                    {
                        param.DataType = BoldReports.RDL.DOM.DataTypes.Float;
                    }
                    else if (dataType.Equals("system.int16") || dataType.Equals("system.int32") || dataType.Equals("system.int64"))
                    {
                        param.DataType = BoldReports.RDL.DOM.DataTypes.Integer;
                    }
                    else if (dataType.Equals("system.boolean"))
                    {
                        param.DataType = BoldReports.RDL.DOM.DataTypes.Boolean;
                    }
                    else if (dataType.Equals("system.double"))
                    {
                        param.DataType = BoldReports.RDL.DOM.DataTypes.Float;
                    }

                    param.Prompt = "regionid";
                    parameters.Add(param);
                }
            }
            else if (dataSetInfoFromFile.DataSet.Name == "TokenCustomers"
                || dataSetInfoFromFile.DataSet.Name == "TokenOrders")
            {
                BoldReports.RDL.DOM.ReportParameter param = new BoldReports.RDL.DOM.ReportParameter();
                param.Name = "Token";
                param.Prompt = param.Name;
                param.Hidden = true;
                param.DataType = BoldReports.RDL.DOM.DataTypes.String;
                param.DefaultValue = new BoldReports.RDL.DOM.DefaultValue();
                param.DefaultValue.Values = new BoldReports.RDL.DOM.Values();
                ReportModel model = new ReportModel("");
                param.DefaultValue.Values.Add(model.GetWebAPIToken());
                parameters.Add(param);
            }

            if (parameters.Count > 0)
            {
                return parameters;
            }

            return null;
        }

        public string GetDataSet(bool sharedDataSet, BoldReports.RDL.DOM.Server.SharedDataSet dataSetInfoFromFile, 
            string dataSetName)
        {
            BoldReports.RDL.DOM.DataSet dataSet = new BoldReports.RDL.DOM.DataSet();
            dataSet.Name = dataSetInfoFromFile.DataSet.Name;

            if (sharedDataSet)
            {
                dataSet.SharedDataSet = new BoldReports.RDL.DOM.SharedDataSet();
                dataSet.SharedDataSet.SharedDataSetReference = dataSetName;
            }
            else
            {
                dataSet.Query = new BoldReports.RDL.DOM.Query();
                dataSet.Query.DataSourceName = dataSetName;
                dataSet.Query.CommandType = dataSetInfoFromFile.DataSet.Query.CommandType;
                dataSet.Query.CommandText = dataSetInfoFromFile.DataSet.Query.CommandText;
                dataSet.Query.QueryDesignerState = dataSetInfoFromFile.DataSet.Query.QueryDesignerState;
            }

            //EliminateFieldsSpecialChar(dataSetInfoFromFile.DataSet.Fields);
            dataSet.Fields = dataSetInfoFromFile.DataSet.Fields;
            return this.SerializeDOM(dataSet);
        }

        public string SerializeDOM(object value, bool isDOM = true)
        {
            var jsonObj = Newtonsoft.Json.JsonConvert.SerializeObject(value,
                      new Newtonsoft.Json.JsonSerializerSettings
                      {
                          ContractResolver = new JSONContractResolver(),
                          TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
                      });

            if (isDOM)
            {
                return GetJSONWithoutAssembly(jsonObj).Replace("\'", "");
            }

            return jsonObj.Replace("\'", "");
        }

        internal string GetJSONWithoutAssembly(string input)
        {
            var assemblyName = typeof(BoldReports.RDL.DOM.ReportDefinition).Assembly.GetName().Name;
            return input.Replace("$type", "__type").Replace(", " + assemblyName, string.Empty);
        }

        private void EliminateFieldsSpecialChar(BoldReports.RDL.DOM.Fields fields)
        {
            Dictionary<string, int> columnNames = new Dictionary<string, int>();

            foreach (BoldReports.RDL.DOM.Field field in fields)
            {
                field.Name = EliminateSpecialChar(field.Name);
                this.UpdateColumnName(columnNames, field);
            }
        }

        public void UpdateColumnName(Dictionary<string, int> columnNames, BoldReports.RDL.DOM.Field field)
        {
            if (columnNames.ContainsKey(field.Name))
            {
                var key = field.Name;
                field.Name += columnNames[key] + 1;

                if (columnNames.ContainsKey(field.Name))
                {
                    this.UpdateColumnName(columnNames, field);
                }
                else
                {
                    columnNames[key] = columnNames[key] + 1;
                }
            }
            else
            {
                columnNames.Add(field.Name, 0);
            }
        }

        private string EliminateSpecialChar(string text)
        {
            try
            {
                return System.Text.RegularExpressions.Regex.Replace(text, @"([ #$%&()*+',./:;<=>?@\[\\\]^\{|}~_-])", "");
            }
            catch (Exception)
            {
            }
            return text;
        }
    }

    public class ReportDesignerData
    {
        public List<string> DataSources { get; set; }

        public List<string> DataSets { get; set; }

        public List<string> Parameters { get; set; }

        public ReportDesignerData()
        {
            this.DataSources = new List<string>();
            this.DataSets = new List<string>();
            this.Parameters = new List<string>();

        }
    }
}
