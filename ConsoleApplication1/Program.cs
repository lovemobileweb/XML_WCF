using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;


namespace DBGetData
{
    class Program
    {
        static void Main(string[] args)
        {
            decimal totalAmount, amount, taxPaid;
            int invoiceID, quantity, lineItemID;
            string productGroup;
            DateTime creationDate;

            string connectionString = null;
            SqlConnection connection;
            SqlCommand command;
            string sql = null;
            SqlDataReader dataReader;

            connectionString = "Server=tcp:xt4mcbv09r.database.windows.net,1433;Data Source=xt4mcbv09r.database.windows.net;Initial Catalog=XMLSandBox;Persist Security Info=False;User ID=user;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            sql = "select invoices.timestamp, invoices.invoiceid, invoices.subtotal, invoices.tax, invoices.total, invoice_products.productid, " +
                    "invoice_products.quantity, products.sku, invoice_products.productprice, invoice_products.productdiscount " +
                    "from invoices " +
                    "inner join invoice_products " +
                    "on invoices.invoiceid = invoice_products.invoiceid " +
                    "inner join products on invoice_products.productid = products.productid  where cast(invoices.timestamp as date) = cast(dateadd(day, -1, getdate()) as date) " +
                    "order by invoice_products.invoiceid";

            connection = new SqlConnection(connectionString);

            try
            {
                connection.Open();
                command = new SqlCommand(sql, connection);
                dataReader = command.ExecuteReader();
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

                    lineItemID = 1;
                    invoiceID = (int)dataReader["invoiceid"];
                    creationDate = (DateTime)dataReader["timestamp"];
                    totalAmount = (decimal)dataReader["total"];
                    lineItemID = (int)lineItemID + 1; // need to make sure that program understands lineItemID changes
                    amount = (decimal)dataReader["productprice"] - (decimal)dataReader["productdiscount"];
                    quantity = (int)dataReader["quantity"];
                    productGroup = (string)dataReader["SKU"];
                    taxPaid = (decimal)dataReader["tax"] > 0 ? 1 : 0;

                    Console.WriteLine("TimeStamp: " + creationDate + " invoiceID: " + invoiceID + " TotalAmount: " + totalAmount + "lineItemID: " + lineItemID + " Product: " + productGroup);
                }
                dataReader.Close();
                command.Dispose();
                connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Can not open connection ! ");
            }
        }


    }
}

