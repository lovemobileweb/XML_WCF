using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml;

namespace SalesUpload
{
    [ServiceContract]
    public interface IServiceSalesUpload
    {
        [OperationContract]
        void SalesSubmission();
    }
}
