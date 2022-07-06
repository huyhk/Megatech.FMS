using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using System.Web.Http;


namespace Megatech.FMS.WebAPI.Controllers
{

    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post()
        {
            var content = Request.Content.ReadAsMultipartAsync().Result;

            var files = HttpContext.Current.Request.Files;
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
