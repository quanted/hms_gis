using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

//
//
namespace PercentArea.Controllers
{
    public class GridCellController : ApiController
    {
        // GET api/GridCell/01020003            Method for taking catchment number
        [HttpGet]
        [Route("api/GridCell/{id:length(8)}")]
        public List<Object> Get(string id)
        {
            Models.GridCell cell = new Models.GridCell();
            return cell.CalculateDataTable(id);
        }

        [HttpPost]
        [Route("api/GridCell/")]
        public Task<HttpResponseMessage> Post()       //Method for taking .geojson
        {
            HttpRequestMessage request = this.Request;
            if (!request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            
            var provider = new MultipartFormDataStreamProvider("M:\\TransientStorage");

            var task = request.Content.ReadAsMultipartAsync(provider).
                ContinueWith<HttpResponseMessage>(o =>
                {
                    string output = "";
                    foreach (MultipartFileData file in provider.FileData)
                    {
                        string fileName = file.Headers.ContentDisposition.FileName;
                        if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                        {
                            fileName = fileName.Trim('"');
                        }
                        if (fileName.Contains(@"/") || fileName.Contains(@"\"))
                        {
                            fileName = Path.GetFileName(fileName);
                        }
                        Models.GridCell cell = new Models.GridCell();
                        string geojson = System.IO.File.ReadAllText(file.LocalFileName).Replace("\n", "");//provider.Contents.ToString();
                        output = JsonConvert.SerializeObject(cell.CalculateDataTable(geojson), Formatting.Indented);
                    }
                    // this is the file name on the server where the file was saved 

                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output)
                    };
                }
            );
            return task;
        }
    }
}
