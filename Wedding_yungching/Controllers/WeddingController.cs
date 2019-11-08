using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Wedding_yungching.Controllers
{
    public class WeddingController : Controller
    {
        private SDpay userdb = new SDpay();
        DateTime dt = DateTime.Now; // 取得現在時間
        // GET: Wedding
        public ActionResult Index()
        {
            return View();
        }

        //管理者登入(新人)
        [Authorize]
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string LoginAccount, string Password)
        {

            // User user = db.User.Where(x => x.LoginAccount == LoginAccount && x.Password == pwd).FirstOrDefault();
            SDuser user = userdb.SDuser.Where(x => x.adaccount == LoginAccount && (x.state == 512 || x.state == 444)).FirstOrDefault();

            if (user != null)
            {
                string pwd = UserDataHandler.Md5Hash(Password);
                if (user.Password == pwd)
                {
                    UserDataHandler.LoginSaveToCookies(user);
                    return RedirectToAction("NewPhotoWall", "Wedding");
                }
                else
                {
                    ViewBag.error = "帳號或密碼錯誤，請重新輸入。";
                }
            }
            else
            {
                ViewBag.error = "帳號或密碼錯誤，請重新輸入。";
            }

            return View();
        }

    }
}