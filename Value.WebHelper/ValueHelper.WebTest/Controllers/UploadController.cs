using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ValueWebHelper.ValueUpload;
using ValueWebHelper.ValueUpload.UploadEvents;
using ValueWebHelper.ValueUpload.Infrastructure;

namespace ValueHelper.WebTest.Controllers
{
    public class UploadController : Controller
    {
        //
        // GET: /Upload/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult UploadFile()
        {
            var path = Server.MapPath("~/Uploads");
            var context = ControllerContext.HttpContext;
            ValueUpload upload = new ValueUpload(context, context.Request.ContentEncoding);
            upload.OnUploading += new Uploading(upload_OnUploading);
            UploadInfo info = upload.Save(path);
            if (info.Success)
            {
                return Content("Success");
            }
            else
            {
                return Content("NotOKd");
            }
        }

        private void upload_OnUploading(object sender, UploadingEventArgs e)
        {
            String progress = e.Progress;
        }

    }
}
