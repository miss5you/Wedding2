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
using Wedding_yungching.Models;

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

        //留言祝福
        public ActionResult NewPhotoUp()
        {
            return View();
        }

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
    }
}