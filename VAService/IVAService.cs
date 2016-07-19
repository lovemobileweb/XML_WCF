using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace VAService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IVAService" in both code and config file together.
    [ServiceContract]
    public interface IVAService
    {
        [OperationContract]
        void Method1(string customerNumber, string contractNumber, string shopId, int locationId);
    }
}
