using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Wedding_yungching.Models;
using PagedList;

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

        #region 留言上傳

        //留言祝福
        public ActionResult NewPhotoUp()
        {
            return View();
        }

        //接收留言
        [HttpPost]
        public string RecvivePhoto(Uploadinfo uplist)
        {
            string message = "";
            try
            {
                string photoname = DateTime.Now.ToString("yyyyMMddHHmmss");//圖片名稱
                string path = ConfigurationManager.AppSettings["WeddingPhoto"] + "blessing/";//路徑
                //log
                //Utility.LogManager.WriteLog(Utility.LogType.Info, "Wedding", uplist.size);//寫入log
                if (uplist.photoname != null)
                {
                    uplist.size = uplist.size;
                    uplist.ex = uplist.ex;
                    //base64轉jpeg
                    string[] ext = uplist.photoname.Split(',');
                    //log
                    //Utility.LogManager.WriteLog(Utility.LogType.Info, "Wedding", "RecvivePhoto.【phone:" + uplist.size + "】,base64:" + ext[1]);//寫入log

                    //Base64ToImage
                    Image photo = Base64ToImage(ext[1]);
                    int Height = photo.Height;
                    int Width = photo.Width;
                    //如果大小不是預期的,重設大小並壓縮
                    if (Height != 1000)
                    {
                        //調整size並壓縮

                        int NewHeight = 1000;//固定高度
                        float NewValue = (float)NewHeight / (float)Height;
                        int NewWidth = (int)(Width * NewValue);

                        Bitmap bmpThumb = new Bitmap(NewWidth, NewHeight);
                        Graphics g = Graphics.FromImage(bmpThumb);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        //绘制图像
                        g.DrawImage(photo, 0, 0, NewWidth, NewHeight);
                        g.Dispose();
                        //壓縮
                        EncoderParameters myEncoderParameters = new EncoderParameters();
                        long[] qy = new long[1];
                        qy[0] = 80;//设置压缩的比例1-100  
                        EncoderParameter myEncoderParameter = new EncoderParameter(Encoder.Quality, qy);
                        myEncoderParameters.Param[0] = myEncoderParameter;
                        ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
                        ImageCodecInfo jpegICIinfo = null;
                        for (int x = 0; x < arrayICI.Length; x++)
                        {
                            if (arrayICI[x].FormatDescription.Equals("JPEG"))
                            {
                                jpegICIinfo = arrayICI[x];
                                break;
                            }
                        }

                        if (jpegICIinfo != null)
                        {
                            bmpThumb.Save(Server.MapPath(path + photoname + ".jpg"), jpegICIinfo, myEncoderParameters);
                        }

                        //將新的寬度代入
                        Width = NewWidth;
                    }
                    else
                    {

                        photo.Save(Server.MapPath(path + photoname + ".jpg"));
                    }

                    //存入資料庫
                    uplist.photoname = (path + photoname) + ".jpg";
                    uplist.type = "Wedding";
                    uplist.date = dt.ToString("yyyy/MM/dd HH:mm");

                    //以下參數為width:300%;margin-left:500px;
                    int check = Width > Height ? 1 : (Height - Width < 600 ? 2 : 3);

                    switch (check)
                    {
                        case 1://橫的
                            uplist.width = "320";
                            uplist.Mleft = "500";
                            break;
                        case 2://比列太長了
                            uplist.width = "125";
                            uplist.Mleft = "750";
                            break;
                        case 3://正常大小
                            uplist.width = "180";
                            uplist.Mleft = "650";
                            break;
                    }


                    userdb.Uploadinfo.Add(uplist);
                    userdb.SaveChanges();
                    message = "上傳完成";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return message;
        }

        //圖片Base64ToImage
        public Image Base64ToImage(string base64)
        {
            byte[] imageBytes = Convert.FromBase64String(base64);

            MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);

            return image;
        }

        #endregion

        #region 留言牆
        //留言牆
        public ActionResult NewPhotoWall()
        {
            List<Uploadinfo> findphoto = new List<Uploadinfo>();
            var photo = (from db in userdb.Uploadinfo where db.type == "Wedding" select db).ToList();
            //檔案路徑
            string pathphoto = ConfigurationManager.AppSettings["WeddingPhoto"] + "blessing/";//路徑;
            String[] FileCollection = Directory.GetFiles(Server.MapPath(pathphoto), "*.*");
            foreach (var item in FileCollection)
            {

                var search = photo.Where(r => item.Contains(r.photoname.Replace(pathphoto, ""))).FirstOrDefault();
                if (search != null)
                {
                    Uploadinfo path = new Uploadinfo()
                    {
                        id = search.id,
                        photoname = search.photoname,
                        date = search.date,
                        ex = search.ex,
                        size = search.size,
                    };
                    findphoto.Add(path);
                }

            }

            //確認是否有登入
            if (User.Identity.Name.ToLower() == "ethancheng") ViewBag.del = "givedel";

            return View(findphoto.OrderByDescending(r => r.date).ToList());
        }

        //刪除照片
        [Authorize]
        [HttpPost]
        public string delphoto(string id)
        {
            string message = "";
            try
            {
                var get = userdb.Uploadinfo.Find(int.Parse(id));
                userdb.Uploadinfo.Remove(get);
                userdb.SaveChanges();
                message = "完成";
            }
            catch (Exception ex)
            {

                return ex.Message;
            }

            return message;
        }
        #endregion

        #region 婚紗照
        //show婚紗照(若照片需要壓縮,則保持photo資料夾是空的,將照片copy至temp資料夾)
        public ActionResult weddingphoto()
        {
            return View();
        }

        //緍紗照分頁
        public ActionResult weddingphoto_show(int? page)
        {
            List<Uploadinfo> findphoto = new List<Uploadinfo>();
            //檔案路徑
            string pathphoto = ConfigurationManager.AppSettings["WeddingPhoto"] + "photo/";
            String[] FileCollection = Directory.GetFiles(Server.MapPath(pathphoto), "*.jpg");
            if (FileCollection.LongCount() != 0)
            {
                foreach (var item in FileCollection)
                {
                    Uploadinfo path = new Uploadinfo()
                    {
                        photoname = pathphoto + Path.GetFileName(item),
                    };
                    findphoto.Add(path);
                }

            }
            else//找尋temp資料夾並壓縮
            {
                string pathphoto2 = ConfigurationManager.AppSettings["WeddingPhoto"] + "temp/";
                String[] FileCollection2 = Directory.GetFiles(Server.MapPath(pathphoto2), "*.*");

                foreach (var item in FileCollection2)
                {

                    //檔名
                    string filename = Path.GetFileName(item);
                    //宣告圖片
                    Image photo = Image.FromFile(item);
                    //若size過大則先壓縮
                    int Height = photo.Height;
                    int Width = photo.Width;
                    //如果大小不是預期的,重設大小並壓縮
                    if (Height != 1500)
                    {
                        //調整size並壓縮
                        int NewHeight = 1500;//固定高度
                        float NewValue = (float)NewHeight / (float)Height;
                        int NewWidth = (int)(Width * NewValue);

                        Bitmap bmpThumb = new Bitmap(NewWidth, NewHeight);
                        Graphics g = Graphics.FromImage(bmpThumb);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        //绘制图像
                        g.DrawImage(photo, 0, 0, NewWidth, NewHeight);
                        g.Dispose();
                        //壓縮
                        EncoderParameters myEncoderParameters = new EncoderParameters();
                        long[] qy = new long[1];
                        qy[0] = 80;//设置压缩的比例1-100  
                        EncoderParameter myEncoderParameter = new EncoderParameter(Encoder.Quality, qy);
                        myEncoderParameters.Param[0] = myEncoderParameter;
                        ImageCodecInfo[] arrayICI = ImageCodecInfo.GetImageEncoders();
                        ImageCodecInfo jpegICIinfo = null;
                        for (int x = 0; x < arrayICI.Length; x++)
                        {
                            if (arrayICI[x].FormatDescription.Equals("JPEG"))
                            {
                                jpegICIinfo = arrayICI[x];
                                break;
                            }
                        }

                        if (jpegICIinfo != null)
                        {
                            filename = "New_" + filename;//檔名
                            bmpThumb.Save(Server.MapPath(pathphoto + filename), jpegICIinfo, myEncoderParameters);

                        }

                        Uploadinfo path = new Uploadinfo()
                        {
                            photoname = pathphoto + filename,
                        };
                        findphoto.Add(path);
                    }

                }
            }

            return PartialView("_WeddingPhotoShow", findphoto.ToPagedList(page ?? 1, 10));
        }

        #endregion

        #region 照片輪播
        //NewShow照片
        public ActionResult NewShow()
        {
            var photo = (from db in userdb.Uploadinfo where db.type == "Wedding" select db).OrderByDescending(r => r.date);
            ViewBag.count = photo.Count();
            ViewBag.photo = photo.ToList();
            return View();
        }

        #endregion

        #region 管理登入
        //管理者登入(新人)
        [Authorize]
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string LoginAccount, string Password)
        {

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
        #endregion

        #region 遊戲(使用者端)
        //遊戲(User端)login
        public ActionResult GameUserLoginView(string LoginAccount, string numberid)
        {

            SDuser user = userdb.SDuser.Where(x => x.numberid == numberid).FirstOrDefault();

            if (user != null)
            {
                Wedding_UserInfo wedding_user = userdb.Wedding_UserInfo.Where(x => x.username == LoginAccount).FirstOrDefault();
                if (wedding_user == null)
                {
                    //存入db
                    Wedding_UserInfo userinfo = new Wedding_UserInfo()
                    {
                        username = LoginAccount,
                        weddingname = user.adaccount,
                    };
                    userdb.Wedding_UserInfo.Add(userinfo);
                    userdb.SaveChanges();

                    //存入Cookies
                    UserDataHandler.LoginWeddingSaveToCookies(userinfo);

                    return RedirectToAction("GameUserView", "Wedding");
                }
                else
                {
                    ViewBag.error = "已經有人使用,請重新輸入!!";
                }

            }


            return View();
        }

        //遊戲開始(User端)
        [Authorize]
        public ActionResult GameUserView()
        {
            //代入使用者名字
            ViewBag.name = User.Identity.Name;
            return View();
        }

        //使用者送出答案
        [Authorize]
        public string SubAns(string ans, string question)
        {
            string message = "";
            try
            {
                FormsIdentity id = (FormsIdentity)User.Identity;
                FormsAuthenticationTicket ticket = id.Ticket;
                //確認是否已答題
                var UserAns = (from uans in userdb.Wedding_UserAns where uans.uid == question && uans.name == User.Identity.Name select uans).FirstOrDefault();
                if (UserAns != null) return message = "你回答過囉!!";

                //確認題目是否關閉
                var questionstate = from wedding in userdb.Wedding_Question where wedding.uid == question && wedding.name == ticket.UserData && wedding.state == "V" select wedding;
                if (questionstate.Count() == 0) return message = "此題目已關閉囉!!";

                Wedding_UserAns user = new Wedding_UserAns()
                {
                    name = User.Identity.Name,
                    ans = int.Parse(ans),
                    anstime = dt,
                    weddingname = ticket.UserData,
                    uid = question,
                    XorV = "X",
                };
                userdb.Wedding_UserAns.Add(user);
                userdb.SaveChanges();
                message = "完成";

            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            return message;
        }
        #endregion

        #region 遊戲(管理端)
        //遊戲(主控端)
        public ActionResult GameView()
        {

            return View();
        }

        //列出問題
        [Authorize]
        public ActionResult GameShow(int? page)
        {
            //獲取問題
            var userinfo = (from wedding in userdb.Wedding_Question where wedding.name == User.Identity.Name select wedding).OrderBy(r => r.num).ToList();

            return PartialView("_WeddingGameView", userinfo.ToPagedList(page ?? 1, 1));
        }

        //新增問題
        [HttpPost]
        [Authorize]
        public string CreatRequestion(string question, string item, string fraction)
        {
            string message = "";
            try
            {
                //確認題號
                int number = (from wedding in userdb.Wedding_Question where wedding.name == User.Identity.Name select wedding).Count();
                //唯一識別碼
                string uid = "Q" + dt.ToString("yyyyMMddHHmmss");
                Wedding_Question iteminfo = new Wedding_Question()
                {
                    uid = uid,
                    num = number + 1,
                    question = question,
                    item = item,
                    name = User.Identity.Name,
                    state = "X",
                    fraction = float.Parse(fraction),
                };
                // 存入DB
                userdb.Wedding_Question.Add(iteminfo);
                userdb.SaveChanges();
                message = "完成";
            }
            catch (Exception ex)
            {

                message = ex.Message;
            }

            return message;
        }

        //總名次統計
        [Authorize]
        public ActionResult allsum()
        {
            //獲取問題
            var userinfo = (from wedding in userdb.Wedding_UserAns where wedding.weddingname == User.Identity.Name && wedding.XorV == "V" select wedding).ToList();

            List<Wedding_UserAns> UserAns = userinfo.GroupBy(r => new { r.name }).Select(r => new Wedding_UserAns { name = r.Key.name, fraction = r.Sum(y => y.fraction) }).ToList();

            return PartialView("_WeedingGameOtherShow", UserAns.OrderByDescending(r => r.fraction).Take(10));
        }

        //測試
        [Authorize]
        [HttpPost]
        public string Gametest(string qtestuser)
        {
            string message = "";
            try
            {
                //取得目前已存在的user資料
                var userinfo = (from wedding in userdb.Wedding_UserAns where wedding.weddingname == User.Identity.Name select wedding).ToList();
                //獲取問題
                var question = (from wedding in userdb.Wedding_Question where wedding.name == User.Identity.Name select wedding).OrderBy(r => r.num).ToList();
                List<Wedding_UserAns> ans = new List<Wedding_UserAns>();
                // 答案取亂數
                string[] TestAns = { "1", "2", "3", "4" };
                Random Ran1 = new Random();
                foreach (var item in question)
                {
                    for (int i = 1; i <= int.Parse(qtestuser); i++)
                    {
                        string name = "樹林大雞哥A" + i;
                        if (userinfo.Where(r => r.name == name && r.uid == item.uid).Count() == 0)//避免資料重覆
                        {

                            Wedding_UserAns user = new Wedding_UserAns()
                            {
                                weddingname = User.Identity.Name,
                                uid = item.uid,
                                name = name,
                                ans = int.Parse(TestAns[Ran1.Next(0, 4)]),
                                XorV = "X",
                                anstime = dt.AddSeconds(i),
                            };
                            ans.Add(user);
                        }
                    }
                }
                userdb.Wedding_UserAns.AddRange(ans);
                userdb.SaveChanges();
                message = "完成";

            }
            catch (Exception ex)
            {

                message = ex.Message;
            }

            return message;
        }

        //清空所有答案
        [Authorize]
        [HttpPost]
        public string DelAllUserAns()
        {
            string message = "";
            try
            {
                //取得目前已存在的user資料
                var userinfo = (from wedding in userdb.Wedding_UserAns where wedding.weddingname == User.Identity.Name select wedding).ToList();
                //清除
                userdb.Wedding_UserAns.RemoveRange(userinfo);
                userdb.SaveChanges();
                message = "完成";
            }
            catch (Exception ex)
            {

                message = ex.Message;
            }

            return message;
        }

        #region 修改、刪除
        //show修改題目和選項
        [HttpGet]
        public JsonResult modify(string uid)
        {
            //確認題號
            var question = (from wedding in userdb.Wedding_Question where wedding.uid == uid select wedding).FirstOrDefault();

            return Json(question, JsonRequestBehavior.AllowGet);
        }

        //刪除或修改題目
        [HttpPost]
        public string DelOrModify(string uid, string question, string item, string qans, string qsum, string fraction, string type)
        {
            string message = "";
            try
            {
                //確認題號
                var qinfo = (from wedding in userdb.Wedding_Question where wedding.uid == uid select wedding).FirstOrDefault();
                switch (type)
                {
                    case "刪除":
                        userdb.Wedding_Question.Remove(qinfo);
                        break;
                    case "修改":
                        qinfo.question = question;
                        //qinfo.start = DateTime.Parse(strtime);
                        //qinfo.stop = DateTime.Parse(stotime);
                        qinfo.item = item;
                        //qinfo.ans = int.Parse(qans);
                        qinfo.sumans = qsum;
                        qinfo.fraction = float.Parse(fraction);
                        break;
                }
                userdb.SaveChanges();
                message = "完成";
            }
            catch (Exception ex)
            {

                message = ex.Message;
            }

            return message;
        }
        #endregion

        //問題時間開始or結束
        [HttpPost]
        [Authorize]
        public string GameSorT(string uid)
        {
            string message = "";
            try
            {
                //確認題號
                var question = (from wedding in userdb.Wedding_Question where wedding.uid == uid select wedding).FirstOrDefault();
                string state = question.state != "X" ? "stop" : "start";
                switch (state)
                {
                    case "start":
                        //question.start = dt;
                        question.state = "V";
                        userdb.SaveChanges();
                        break;
                    case "stop":
                        //question.stop = dt;
                        question.state = "X";
                        userdb.SaveChanges();
                        break;
                }

            }
            catch (Exception ex)
            {

                message = ex.Message;
            }

            return message;
        }

        //送出答案
        [HttpPost]
        [Authorize]
        public string SubmitAns(string uid, string ans, string allnum)
        {
            string message = "";
            try
            {
                //答案
                int newans = int.Parse(ans);
                //選項總數
                int newnum = int.Parse(allnum);
                //確認題號寫入答案
                var question = (from wedding in userdb.Wedding_Question where wedding.uid == uid select wedding).FirstOrDefault();
                question.ans = newans;

                //使用者發出的答案
                var UserAns = (from user in userdb.Wedding_UserAns
                               where user.weddingname == question.name && user.uid == uid
                               select user).ToList();
                
                //答案人數統計
                List<int> SumAns = new List<int>();
                //標記答對
                foreach (var username in UserAns)
                {

                    if (username.ans == newans)
                    {
                        username.XorV = "V";
                        username.fraction = Double.Parse(String.Format("{0:N6}", (question.fraction / ((username.anstime.Value.Hour * 3600 + username.anstime.Value.Minute * 60 + username.anstime.Value.Second) * 0.001))));
                    }
                    else
                    {
                        username.XorV = "X";
                    }

                    SumAns.Add(username.ans);

                }

                //記錄答案人數
                string alldb = "";
                for (int i = 1; i <= newnum; i++)
                {
                    int a = SumAns.Where(r => r == i).Count();
                    alldb += a != 0 ? a.ToString() + "," : "0,";
                }
                //存入question的db
                question.sumans = string.IsNullOrEmpty(alldb) ? "" : alldb.Substring(0, alldb.Length - 1);
                question.state = "X";
                userdb.SaveChanges();
                message = "完成;" + question.sumans;
            }
            catch (Exception ex)
            {

                message = ex.Message;
            }

            return message;
        }

        //單題名次View
        [Authorize]
        public ActionResult GameSumShow(string uid)
        {
            //獲取問題
            var userinfo = (from wedding in userdb.Wedding_Question where wedding.uid == uid select wedding).FirstOrDefault();

            //獲取答對名單
            var UserAns = (from user in userdb.Wedding_UserAns where user.uid == userinfo.uid && user.XorV == "V" select user).OrderBy(r => r.anstime).ToList();
            List<string> sum = string.IsNullOrEmpty(userinfo.sumans) ? new List<string>() : userinfo.sumans.Split(',').ToList();
            int ans1 = 0; int ans2 = 0; int ans3 = 0; int ans4 = 0; int ans5 = 0; int ans6 = 0;
            int i = 0;
            foreach (var item in sum)
            {
                switch (i)
                {
                    case 0:
                        ans1 += int.Parse(sum[i].ToString());
                        break;
                    case 1:
                        ans2 += int.Parse(sum[i].ToString());
                        break;
                    case 2:
                        ans3 += int.Parse(sum[i].ToString());
                        break;
                    case 3:
                        ans4 += int.Parse(sum[i].ToString());
                        break;
                    case 4:
                        ans5 += int.Parse(sum[i].ToString());
                        break;
                    case 5:
                        ans6 += int.Parse(sum[i].ToString());
                        break;
                }
                i++;

            }

            ViewBag.ans1 = ans1;
            ViewBag.ans2 = ans2;
            ViewBag.ans3 = ans3;
            ViewBag.ans4 = ans4;
            ViewBag.ans5 = ans5;
            ViewBag.ans6 = ans6;

            return PartialView("_WeddingSum", UserAns.Take(10));
        }


        #endregion

        #region 刮刮樂
        //刮刮樂
        [Authorize]
        public ActionResult NewRandomPhoto()
        {
            List<Uploadinfo> photo = (from db in userdb.Uploadinfo where db.type == "Wedding" select db).ToList();
            new Random().Next(photo.Count());
            var show = photo.OrderBy(_ => Guid.NewGuid()).First();
            ViewBag.photoname = show.photoname;
            ViewBag.size = show.size;
            ViewBag.ex = show.ex;
            ViewBag.date = show.date;
            return View();
        }
        #endregion

        //登入
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("NewPhotoWall", "Wedding");
            }
            return View();
        }

        //登出
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("NewPhotoWall", "Wedding");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                userdb.Dispose();
            }

            base.Dispose(disposing);
        }
    
    }
}