using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.ServiceModel.Web;
using System.ServiceModel.Channels;

namespace NinePlaces_Service
{
    /// <summary>
    /// Request interceptor to process the X-HTTP-Method-Override header.
    /// Allows us to tunnel a PUT or DELETE request through a more-standard POST request.
    /// 
    /// This is also a potential Auth-Check location.
    /// </summary>
    public class XHttpMethodOverrideInterceptor : RequestInterceptor
    {
        public XHttpMethodOverrideInterceptor() : base(true) { }

        public override void ProcessRequest(ref RequestContext requestContext)
        {
            if (requestContext == null || requestContext.RequestMessage == null)
            {
                return;
            }

            Message message = requestContext.RequestMessage;
            HttpRequestMessageProperty reqProp = (HttpRequestMessageProperty)message.Properties[HttpRequestMessageProperty.Name];
            string overrideVal = reqProp.Headers["X-HTTP-Method-Override"];

            if (!string.IsNullOrEmpty(overrideVal))
            {
                reqProp.Method = overrideVal;
            }
        }
    }
}
