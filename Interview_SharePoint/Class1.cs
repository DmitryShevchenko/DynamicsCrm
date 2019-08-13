using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.Query;

namespace Interview_SharePoint
{
    public class PreAnnotationCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.Get<IPluginExecutionContext>();
            var service = serviceProvider.GetOrganizationService(context.UserId);

            try
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity entity)
                {
                    CreateFolderInSharepoint(entity);
                    UploadFileInSharePoint(entity);

                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }


        private void CreateFolderInSharepoint(Entity entity)
        {
            String relativePath = "lead/test lead_ 9a81460e-bf69-e511-80f3-c4346bad3608";

            Uri spSite = new Uri("https://dutpd41v7.sharepoint.com");

            string odataQuery = "_api/web/folders";

            byte[] content =
                Encoding.ASCII.GetBytes(@"{ '__metadata': { 'type': 'SP.Folder' }, 'ServerRelativeUrl': '" +
                                        relativePath + "'}");

            Uri url = new Uri($"{spSite}/{odataQuery}");

            var webRequest = (HttpWebRequest) WebRequest.Create(url);

            //Create the digest and pass the digest to header as shown below

            webRequest.Headers.Add("X-RequestDigest",
                "https://dutpd41v7.crm4.dynamics.com/;Username=dmshe@dutpd41v7.onmicrosoft.com;");

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);

            request.Method = "POST";

            request.Accept = "application/json;odata=verbose;charset=utf-8";

            request.AllowAutoRedirect = false;

            request.ContentLength = content.Length;

            var requestContent = content;

            using (Stream s = request.GetRequestStream())

            {
                s.Write(requestContent, 0, requestContent.Length);

                s.Close();
            }

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            StreamReader sr = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException(), Encoding.GetEncoding("utf-8"));

            byte[] responseStream = Encoding.UTF8.GetBytes(sr.ReadToEnd());

            Encoding.UTF8.GetString(responseStream, 0, responseStream.Length);
        }

        private void UploadFileInSharePoint(Entity entity)
        {
            String relativePath = string.Join(" ", entity.LogicalName, entity.LogicalName + "_", entity.Id);

            string leadLibraryName = entity.LogicalName;

            string destLocation = string.Join(" ", entity.LogicalName + "_", entity.Id);

            string fileName = "File";

            byte[] content;

            var defaultSite = "https://dutpd41v7.sharepoint.com";

            Uri spSite = new Uri(defaultSite);

            var fileContent = spSite.GetType().GetFields().Select(x => byte.Parse(x.GetValue(spSite) as string ?? throw new InvalidOperationException())).ToArray();

            content = fileContent;

            Uri url = new Uri(
                $"{defaultSite}/_api/web/GetFolderByServerRelativeUrl('/{leadLibraryName + "/" + destLocation}')/Files/add(url='{fileName}', overwrite=true)");

            var webRequest = (HttpWebRequest) WebRequest.Create(url);


            webRequest.Headers.Add("X-RequestDigest", "https://dutpd41v7.crm4.dynamics.com/;Username=dmshe@dutpd41v7.onmicrosoft.com;");

            webRequest.ContentLength = fileContent.Length;

            HttpWebRequest request = (HttpWebRequest) HttpWebRequest.Create(url);

            request.Method = "POST";

            request.Accept = "application/json;odata=verbose;charset=utf-8";

            request.AllowAutoRedirect = false;

            request.ContentLength = content.Length;
            var requestContent = content;

            using (Stream s = request.GetRequestStream())

            {
                s.Write(requestContent, 0, requestContent.Length);

                s.Close();
            }

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8"));

            byte[] responseStream = Encoding.UTF8.GetBytes(sr.ReadToEnd());

            Encoding.UTF8.GetString(responseStream, 0, responseStream.Length);
        }
    }
}