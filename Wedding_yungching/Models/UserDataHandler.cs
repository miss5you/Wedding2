using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Security;

namespace Wedding_yungching.Models
{
    public class UserDataHandler
    {
        //登入
        public static void LoginSaveToCookies(SDuser user)
        {
            FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                version: 1,
                name: user.adaccount,
                issueDate: DateTime.Now,
                expiration: DateTime.Now.AddHours(10),
                isPersistent: false,
                userData: user.name,//UserData用來儲存使用者編號
                cookiePath: FormsAuthentication.FormsCookiePath
                );

            string encryptTicket = FormsAuthentication.Encrypt(ticket);
            HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptTicket);
            cookie.HttpOnly = true;
            cookie.Expires = ticket.Expiration;
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static string Md5Hash(string password)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }
}