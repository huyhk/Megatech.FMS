using FMS.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Description;

namespace Megatech.FMS.WebAPI.Controllers
{
    public class ChangeLogsController : ApiController
    {
        private DataContext db = new DataContext();

        // GET: api/ChangeLogs
        public IQueryable<ChangeLog> GetChangeLogs()
        {
            return db.ChangeLogs;
        }

        // GET: api/ChangeLogs/5
        [ResponseType(typeof(ChangeLog))]
        public IHttpActionResult GetChangeLog(int id)
        {
            ChangeLog changeLog = db.ChangeLogs.Find(id);
            if (changeLog == null)
            {
                return NotFound();
            }

            return Ok(changeLog);
        }

        // PUT: api/ChangeLogs/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutChangeLog(int id, ChangeLog changeLog)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != changeLog.Id)
            {
                return BadRequest();
            }

            db.Entry(changeLog).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChangeLogExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/ChangeLogs
        [ResponseType(typeof(ChangeLog))]
        public IHttpActionResult PostChangeLog(ChangeLog changeLog)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.ChangeLogs.Add(changeLog);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = changeLog.Id }, changeLog);
        }

        // DELETE: api/ChangeLogs/5
        [ResponseType(typeof(ChangeLog))]
        public IHttpActionResult DeleteChangeLog(int id)
        {
            ChangeLog changeLog = db.ChangeLogs.Find(id);
            if (changeLog == null)
            {
                return NotFound();
            }

            db.ChangeLogs.Remove(changeLog);
            db.SaveChanges();

            return Ok(changeLog);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ChangeLogExists(int id)
        {
            return db.ChangeLogs.Count(e => e.Id == id) > 0;
        }
    }
}