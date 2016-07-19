using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace SalesUpload
{
    public class ServiceSalesUpload : IServiceSalesUpload
    {
        public void SalesSubmission()
        {
            XmlDocument xmlDoc = CreatePayload();
            CallWebService(xmlDoc);
        }

        public XmlDocument CreatePayload()
        {
            XmlDocument xmlPayload = CreateXmlDoc();
            XmlElement xmlEleSalesData = CreateSalesDataElement(xmlPayload);

            decimal cash, checks, external, total;
            int locationID;
            DateTime date;
            string connectionString = null;
            SqlConnection connection;
            SqlCommand command;
            string sql = null;
            SqlDataReader dataReader;

            connectionString = "Server=tcp:xt4mcbv09r.database.windows.net,1433;Data Source=xt4mcbv09r.database.windows.net;Initial Catalog=XMLSandBox;Persist Security Info=False;User ID=user;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
            sql = "select * from salesinfo where novaid = 'A-1561360' order by startdate";
            connection = new SqlConnection(connectionString);

            try
            {
                connection.Open();
                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    locationID = (int)dataReader["LocationID"];
                    date = (DateTime)dataReader["StartDate"];

                    if (dataReader["Cash"] != DBNull.Value)
                    {
                        cash = (decimal)dataReader["Cash"];
                    }
                    else
                    {
                        cash = 0;
                    }

                    if (dataReader["Checks"] != DBNull.Value)
                    {
                        checks = (decimal)dataReader["Checks"];
                    }
                    else
                    {
                        checks = 0;
                    }

                    if (dataReader["External"] != DBNull.Value)
                    {
                        external = (decimal)dataReader["External"];
                    }
                    else
                    {
                        external = 0;
                    }

                    total = cash + checks + external;

                    AppendSalesRecord(xmlPayload, xmlEleSalesData, locationID, date, total);

                    Console.WriteLine("Sales for location " + locationID + " on " + date + " where " + total);
                }
                dataReader.Close();
                command.Dispose();
                connection.Close();

                SetLoginUserID(xmlPayload, "SCL01277");
                SetActivationKey(xmlPayload, "SENOD-MKKJU-MWEBV-QKKAT");

                SaveXmlFile(xmlPayload, "salesdata.xml");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message);
            }
            return xmlPayload;
        }

        private XmlDocument CreateXmlDoc()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(new StringReader("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\" xmlns:arr=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><soapenv:Header/><soapenv:Body><tem:SalesSubmission/></soapenv:Body></soapenv:Envelope>"));
            return xmlDoc;
        }

        private XmlElement CreateSalesDataElement(XmlDocument xmlDoc)
        {
            XmlElement xmlEle = xmlDoc["soapenv:Envelope"];
            xmlEle = xmlEle["soapenv:Body"];
            xmlEle = xmlEle["tem:SalesSubmission"];
            XmlElement xmlEleSalesData = xmlDoc.CreateElement("tem", "Sales_Data", "http://tempuri.org/");
            //xmlEleSalesData.Prefix = "tem";
            xmlEle.AppendChild(xmlEleSalesData);
            return xmlEleSalesData;
        }

        private bool AppendSalesRecord(XmlDocument xmlDoc, XmlElement xmlEleSalesData, int nLocation, DateTime date, decimal total)
        {
            XmlElement xmlEle = xmlDoc.CreateElement("arr", "string", "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
            xmlEle.InnerText = ("GC0001") + (nLocation == 1 ? ",MO002393" : ",MO002404") + date.ToString(",yyyyMMdd") + ("," + total.ToString()) + (",Gross");
            xmlEleSalesData.AppendChild(xmlEle);
            return true;
        }

        private bool SetLoginUserID(XmlDocument xmlDoc, string loginUserID)
        {
            XmlElement xmlEle = xmlDoc["soapenv:Envelope"];
            xmlEle = xmlEle["soapenv:Body"];
            xmlEle = xmlEle["tem:SalesSubmission"];
            XmlElement xmlEleLoginUserID = xmlEle["tem:Login_UserID"];
            if (xmlEleLoginUserID == null)
            {
                xmlEleLoginUserID = xmlDoc.CreateElement("tem", "Login_UserID", "http://tempuri.org/");
                xmlEleLoginUserID.InnerText = loginUserID;
                xmlEle.AppendChild(xmlEleLoginUserID);
            }
            else
                xmlEleLoginUserID.InnerText = loginUserID;
            return true;
        }

        private bool SetActivationKey(XmlDocument xmlDoc, string activationKey)
        {
            XmlElement xmlEle = xmlDoc["soapenv:Envelope"];
            xmlEle = xmlEle["soapenv:Body"];
            xmlEle = xmlEle["tem:SalesSubmission"];
            XmlElement xmlEleActivationKey = xmlEle["tem:Activation_Key"];
            if (xmlEleActivationKey == null)
            {
                xmlEleActivationKey = xmlDoc.CreateElement("tem", "Activation_Key", "http://tempuri.org/");
                xmlEleActivationKey.InnerText = activationKey;
                xmlEle.AppendChild(xmlEleActivationKey);
            }
            else
                xmlEleActivationKey.InnerText = activationKey;
            return true;
        }

        private bool SaveXmlFile(XmlDocument xmlDoc, string filename)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            String dir = Path.GetDirectoryName(path);
            dir += "\\Xml_data";
            filename = Path.Combine(dir, filename);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir); // inside the if statement
            XmlWriter writer = XmlTextWriter.Create(filename, new XmlWriterSettings {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = System.Text.UnicodeEncoding.UTF8 });
            xmlDoc.Save(writer);
            writer.Close();
            return true;
        }

        private bool CallWebService(XmlDocument xmlDoc)
        {
            XmlNodeList xmlNodeListSalesData = xmlDoc.GetElementsByTagName("arr:string");
            XmlNodeList xmlNodeListLoginUserID = xmlDoc.GetElementsByTagName("tem:Login_UserID");
            XmlNodeList xmlNodeListActivationKey = xmlDoc.GetElementsByTagName("tem:Activation_Key");
            if (xmlNodeListSalesData != null && xmlNodeListSalesData.Count > 0
                && xmlNodeListLoginUserID != null && xmlNodeListLoginUserID.Count > 0
                && xmlNodeListActivationKey != null && xmlNodeListActivationKey.Count > 0)
            {
                string uid = xmlNodeListLoginUserID[0].InnerText;
                string actkey = xmlNodeListActivationKey[0].InnerText;
                string[] sales_data = new string[xmlNodeListSalesData.Count];
                int i = 0;
                foreach (XmlNode node in xmlNodeListSalesData)
                {
                    sales_data[i++] = node.InnerText;
                }

                DateTime dt = DateTime.Now;
                ServiceReferenceRetailer365.IwcfRetailer365Client CallWebService = new ServiceReferenceRetailer365.IwcfRetailer365Client();
                ServiceReferenceRetailer365.ServiceResponse res = CallWebService.SalesSubmission(sales_data, uid, actkey);

                string log = "";
                i = 0;
                foreach (string result in res.Result)
                {
                    log += (i++ > 0 ? "\r\n" : "") + result + " - " + dt.ToString();
                }
                foreach (string errormsg in res.ErrorMessage)
                    log += (i++ > 0 ? "\r\n" : "") + errormsg + " - " + dt.ToString();
                Log(log);
            }
            return true;
        }

        public static void Log(string log)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            String dir = Path.GetDirectoryName(path);
            dir += "\\Log";
            string filepath = Path.Combine(dir, "log.txt");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir); // inside the if statement
            using (FileStream fs = new FileStream(filepath, FileMode.Append))
            {
                StreamWriter sw = new StreamWriter(fs);
                //sw.WriteLine("[" + DateTime.Now.ToString() + "] " + log);
                sw.WriteLine(log);
                sw.Close();
            }
        }
    }
}
