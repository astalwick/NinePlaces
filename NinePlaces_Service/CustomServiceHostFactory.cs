using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel.Web;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Threading;
using System.ServiceModel.Diagnostics;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.Xml;
using System.Text;
using System.Web.Security;
using Microsoft.ServiceModel.Web;

namespace NinePlaces_Service
{
    public class CustomServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            CustomHost result = new CustomHost(serviceType, true, baseAddresses[0]);
            result.Interceptors.Add(new XHttpMethodOverrideInterceptor());
            return result;
        }
    }

    public class CustomHost : WebServiceHost2
    {
        public CustomHost(Type serviceType, bool in_bSomething, params Uri[] baseAddresses)
            : base(serviceType, in_bSomething, baseAddresses)
        { }
        protected override void ApplyConfiguration()
        {
            base.ApplyConfiguration();
        }

        public class MyMapper : WebContentTypeMapper
        {
            public override WebContentFormat GetMessageFormatForContentType(string contentType)
            {
                return WebContentFormat.Raw; // always
            }
        }
        static Binding GetBinding()
        {
            CustomBinding result = new CustomBinding(new WebHttpBinding());
            WebMessageEncodingBindingElement webMEBE = result.Elements.Find<WebMessageEncodingBindingElement>();
            webMEBE.ContentTypeMapper = new MyMapper();
            return result;
        }
    }
}
