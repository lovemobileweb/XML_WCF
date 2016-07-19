using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Xml;

namespace VAService
{
    public class LineItemData
    {
        public int mId { get; set; }
        public int mQuantity { get; set; }
        public decimal mAmount { get; set; }
        public decimal mTaxPaid { get; set; }
        public string mProductGroup { get; set; }

        public LineItemData(int id, int quantity, decimal amount, string productGroup, decimal taxPaid)
        {
            mId = id;
            mQuantity = quantity;
            mAmount = amount;
            mProductGroup = productGroup;
            mTaxPaid = taxPaid;
        }
    }

    public class VAService : IVAService
    {
        public void Method1(string customerNumber, string contractNumber, string shopId, int locationId)
        {
            if (customerNumber == null || contractNumber == null || shopId == null)
                return;
            XmlDocument xmlDoc = CreatePayload(customerNumber, contractNumber, shopId, locationId);
            CallWebService(xmlDoc);
        }

        private XmlDocument PostXMLData(string userAuth, string destinationUrl, XmlDocument xmlDoc)
        {
            try
            {
                WebRequest request = (WebRequest)WebRequest.Create(destinationUrl);
                request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(userAuth)));
                byte[] bytes;
                bytes = System.Text.Encoding.ASCII.GetBytes(xmlDoc.OuterXml);
                //request.ContentType = "text/xml; encoding='utf-8'";
                request.ContentLength = bytes.Length;
                request.Method = "POST";
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Close();
                HttpWebResponse response;
                response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream responseStream = response.GetResponseStream();
                    string responseStr = new StreamReader(responseStream).ReadToEnd();
                    XmlDocument xmlRes = new XmlDocument();
                    xmlRes.Load(new StringReader(responseStr));
                    return xmlRes;
                }
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream responseStream = errResp.GetResponseStream())
                {
                    string responseStr = new StreamReader(responseStream).ReadToEnd();
                    XmlDocument xmlRes = new XmlDocument();
                    xmlRes.Load(new StringReader(responseStr));
                    return xmlRes;
                }
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        private static void Log(string log)
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

        private bool CallWebService(XmlDocument xmlDoc)
        {
            string userAuth = "ci_502860:El3ments!";
            DateTime dt = DateTime.Now;
            XmlDocument res = PostXMLData(userAuth, "https://cixml.viennaairport.com/testretail/", xmlDoc);
            if (res == null)
                Log("Failed to call https://cixml.viennaairport.com/testretail/ - " + dt.ToString());
            else
            {
                XmlNodeList xmlNodeList = res.GetElementsByTagName("faultstring");
                if (xmlNodeList != null && xmlNodeList.Count > 0) // error
                    Log(xmlNodeList[0].InnerText + " - " + dt.ToString());
                else
                {
                    xmlNodeList = res.GetElementsByTagName("status");
                    XmlNodeList xmlNodeListId = res.GetElementsByTagName("id");
                    if (xmlNodeList != null && xmlNodeList.Count > 0 && xmlNodeListId != null && xmlNodeListId.Count > 0)
                        Log("ID: " + xmlNodeListId[0].InnerText + " " + xmlNodeList[0].InnerText + " - " + dt.ToString());
                    else
                        Log("Unknown result - " + dt.ToString());
                }
            }
            
            return true;
        }

        private XmlElement CreateXmlElement(XmlDocument xmlDoc, string name)
        {
            return xmlDoc.CreateElement(name);
        }

        private XmlDocument CreateXmlDoc()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(new StringReader("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:con=\"http://www.viennaairport.at/mach2/ifc/concessionaire\"><soapenv:Header/><soapenv:Body><con:salesReport></con:salesReport></soapenv:Body></soapenv:Envelope>"));
            return xmlDoc;
        }

        private XmlElement GetSalesReportElement(XmlDocument xmlDoc)
        {
            XmlElement xmlEle = xmlDoc["soapenv:Envelope"];
            xmlEle = xmlEle["soapenv:Body"];
            xmlEle = xmlEle["con:salesReport"];
            return xmlEle;
        }

        private XmlElement CreatePartnerElement(XmlDocument xmlDoc, XmlElement xmlEleSalesReport, string customerNumber, string contractNumber, string shopId)
        {
            XmlElement xmlElePartner = CreateXmlElement(xmlDoc, "partner");
            xmlElePartner.InnerXml = "<companyCode>VIE</companyCode>" +
                                    "<customerNumber>" + customerNumber + "</customerNumber>" +
                                    "<contractNumber>" + contractNumber + "</contractNumber>" +
                                    "<shopId>" + shopId + "</shopId>";
            xmlEleSalesReport.AppendChild(xmlElePartner);
            return xmlElePartner;
        }

        private static int ToJulianDay(DateTime date)
        {
            return date.DayOfYear;
        }

        private XmlElement SetBcbpData(XmlDocument xmlDoc, XmlElement xmlEleSalesInvoice)
        {
            XmlElement xmlEleBcbpData = CreateXmlElement(xmlDoc, "bcbpData");
            XmlElement xmlEle = CreateXmlElement(xmlDoc, "departure");
            xmlEle.InnerText = "ORD";
            xmlEleBcbpData.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "destination");
            xmlEle.InnerText = "MIA";
            xmlEleBcbpData.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "airline");
            xmlEle.InnerText = "UA";
            xmlEleBcbpData.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "flightNumber");
            xmlEle.InnerText = "098";
            xmlEleBcbpData.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "scheduleDate");
            DateTime dt = new DateTime(2013, 10, 30);
            xmlEle.InnerText = ToJulianDay(dt).ToString();
            xmlEleBcbpData.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "compartmentCode");
            xmlEle.InnerText = "F";
            xmlEleBcbpData.AppendChild(xmlEle);
            xmlEleSalesInvoice.AppendChild(xmlEleBcbpData);

            return xmlEleBcbpData;
        }

        private bool AppendInvoice(XmlDocument xmlDoc, XmlElement xmlEleSalesReport, int id, decimal totalAmount, DateTime creationDate, List<LineItemData> lineItems)
        {
            XmlElement xmlEleInvoice = CreateXmlElement(xmlDoc, "invoice");

            XmlElement xmlEle = CreateXmlElement(xmlDoc, "id");
            xmlEle.InnerText = id.ToString();
            xmlEleInvoice.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "totalAmount");
            xmlEle.InnerText = totalAmount.ToString();
            xmlEleInvoice.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "currency");
            xmlEle.InnerText = "EUR";
            xmlEleInvoice.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "creationDateTime");
            xmlEle.InnerText = creationDate.ToString("yyyy-MM-ddThh:mm:ss");
            xmlEleInvoice.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "loyaltyCardNumber");
            xmlEle.InnerText = "";
            xmlEleInvoice.AppendChild(xmlEle);
            xmlEle = CreateXmlElement(xmlDoc, "customerCount");
            xmlEle.InnerText = "1";
            xmlEleInvoice.AppendChild(xmlEle);
            foreach (LineItemData li in lineItems)
            {
                XmlElement xmlEleLineItem = CreateXmlElement(xmlDoc, "lineItem");
                xmlEle = CreateXmlElement(xmlDoc, "id");
                xmlEle.InnerText = (li.mId * 10).ToString();
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEle = CreateXmlElement(xmlDoc, "quantity");
                xmlEle.InnerText = li.mQuantity.ToString();
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEle = CreateXmlElement(xmlDoc, "amount");
                xmlEle.InnerText = li.mAmount.ToString();
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEle = CreateXmlElement(xmlDoc, "productGroup");
                xmlEle.InnerText = li.mProductGroup.ToString();
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEle = CreateXmlElement(xmlDoc, "promotionCode");
                xmlEle.InnerText = "000";
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEle = CreateXmlElement(xmlDoc, "employeePurchase");
                xmlEle.InnerText = "0";
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEle = CreateXmlElement(xmlDoc, "taxPaid");
                xmlEle.InnerText = li.mTaxPaid.ToString();
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEle = CreateXmlElement(xmlDoc, "airlineVoucher");
                xmlEle.InnerText = "0";
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEle = CreateXmlElement(xmlDoc, "cancelation");
                xmlEle.InnerText = "0";
                xmlEleLineItem.AppendChild(xmlEle);
                xmlEleInvoice.AppendChild(xmlEleLineItem);
            }

            xmlEleSalesReport.AppendChild(xmlEleInvoice);

            SetBcbpData(xmlDoc, xmlEleInvoice);

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
            XmlWriter writer = XmlTextWriter.Create(filename, new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                Encoding = System.Text.UnicodeEncoding.UTF8
            });
            xmlDoc.Save(writer);
            writer.Close();
            return true;
        }

        private XmlDocument CreatePayload(string customerNumber, string contractNumber, string shopId, int locationId)
        {
            XmlDocument xmlPayload = CreateXmlDoc();
            XmlElement xmlEleSalesReport = GetSalesReportElement(xmlPayload);
            CreatePartnerElement(xmlPayload, xmlEleSalesReport, customerNumber, contractNumber, shopId);

            decimal totalAmount = 0, amount = 0, taxPaid = 0;
            int prevInvoiceID = -1, invoiceID = 0, quantity = 0, lineItemID = 0;
            string productGroup;
            DateTime creationDate = DateTime.MinValue;

            string connectionString = null;
            SqlConnection connection;
            SqlCommand command;
            string sql = null;
            SqlDataReader dataReader;

            connectionString = "Server=tcp:xt4mcbv09r.database.windows.net,1433;Data Source=xt4mcbv09r.database.windows.net;Initial Catalog=XMLSandBox;Persist Security Info=False;User ID=user;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            sql = "select invoices.timestamp, invoices.locationid, invoices.invoiceid, invoices.subtotal, invoices.tax, invoices.total, invoice_products.productid, " +
                    "invoice_products.quantity, products.sku, invoice_products.productprice, invoice_products.productdiscount " +
                    "from invoices " +
                    "inner join invoice_products " +
                    "on invoices.invoiceid = invoice_products.invoiceid " +
                    "inner join products on invoice_products.productid = products.productid  where cast(invoices.timestamp as date) = cast(dateadd(day, -1, getdate()) as date) and invoices.locationid = " + locationId.ToString() + " " +
                    "order by invoice_products.invoiceid";

            connection = new SqlConnection(connectionString);

            try
            {
                connection.Open();
                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
                List<LineItemData> lineitems = null;
                lineItemID = 0;
                while (dataReader.Read())
                {
                    // < invoice >< id >?</ id >< totalAmount >?</ totalAmount >< currency > EUR </ currency >< currency > EUR </ currency >< creationDateTime >?</ creationDateTime >< loyaltyCardNumber >?</ loyaltyCardNumber >< customerCount >?</ customerCount >
                    //< !--1 or more repetitions: -->
                    //  < lineItem >
                    //     < id >?</ id >< quantity >?</ quantity >< amount >?</ amount >< productGroup >?</ productGroup >< taxPaid >?</ taxPaid >< airlineVoucher >?</ airlineVoucher >< cancelation >?</ cancelation >
                    //  </ lineItem >

                    // Don't know about this yet. We don't have the data in the DB yet. Please build a stub for it
                    //  < !--0 to 4 repetitions: -->
                    //  < bcbpData >< departure >?</ departure >< destination >?</ destination >< airline >?</ airline >< flightNumber >?</ flightNumber >< scheduleDate >?</ scheduleDate >< compartmentCode >?</ compartmentCode >

                    // This what the the dataReader has
                    // timestamp                    invoiceid   subtotal    tax     total   productid       quantity sku            productprice    productdiscount
                    // 2016 - 07 - 11 07:45:02.863  1375        150.00      30.00   180.00  8               1        RMC08000000    150.00          53.58

                    invoiceID = (int)dataReader["invoiceid"];
                    creationDate = (DateTime)dataReader["timestamp"];
                    totalAmount = (decimal)dataReader["total"];
                    //lineItemID = (int)lineItemID + 1; // need to make sure that program understands lineItemID changes
                    amount = (decimal)dataReader["productprice"] - (decimal)dataReader["productdiscount"];
                    quantity = (int)dataReader["quantity"];
                    productGroup = (string)dataReader["SKU"];
                    taxPaid = (decimal)dataReader["tax"] > 0 ? 1 : 0;

                    // make a list of lineItems
                    if (prevInvoiceID != invoiceID)
                    {
                        if (lineitems != null && prevInvoiceID != -1)
                            AppendInvoice(xmlPayload, xmlEleSalesReport, prevInvoiceID, totalAmount, creationDate, lineitems);
                        prevInvoiceID = invoiceID;
                        lineitems = new List<LineItemData>();
                        lineItemID = 0;
                    }
                    lineitems.Add(new LineItemData(++lineItemID, quantity, amount, productGroup, taxPaid));

                    Console.WriteLine("TimeStamp: " + creationDate + " invoiceID: " + invoiceID + " TotalAmount: " + totalAmount + "lineItemID: " + lineItemID + " Product: " + productGroup);
                }
                if (lineitems != null)
                    AppendInvoice(xmlPayload, xmlEleSalesReport, prevInvoiceID, totalAmount, creationDate, lineitems);

                dataReader.Close();
                command.Dispose();
                connection.Close();

                SaveXmlFile(xmlPayload, "va-sales.xml");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can not open connection ! ");
            }

            return xmlPayload;
        }
    }
}
