using DoAn.Models;
using DoAn.PayMethod;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace DoAn.Controllers
{
    public class GioHangsController : Controller
    {
        private Model1 db = new Model1();

        // GET: GioHangs
        public ActionResult Index()
        {
            var gioHangs = db.GioHangs.Include(g => g.KhachHang).Include(g => g.SanPham);
            return View(gioHangs.ToList());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        public ActionResult RemoveCart(int magiohang)
        {
            GioHang gh = db.GioHangs.Where(s=>s.magiohang == magiohang).FirstOrDefault();
            db.GioHangs.Remove(gh);
            db.SaveChanges();
            Session["giohang"] = (int)Session["giohang"] - 1;
            return Json(new { success = true });
        }

        public ActionResult UpdateCart([Bind(Include = "magiohang,masp,makhachhang,soluong")] GioHang gioHang)
        {
            if (ModelState.IsValid)
            {
                db.Entry(gioHang).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true });
                
            }
            return RedirectToAction("Index");
        }

        public ActionResult ThanhToan()
        {
            var gioHangs = db.GioHangs.Include(g => g.KhachHang).Include(g => g.SanPham);
            KhachHang taikhoan = db.KhachHangs.Find((int)Session["ma"]);
            ViewBag.taikhoan = taikhoan;
            return View(gioHangs.ToList());
        }

        public ActionResult DatHang()
        {
            KhachHang taikhoan = db.KhachHangs.Find((int)Session["ma"]);
            List<GioHang> dssp = db.GioHangs.Where(s => s.makhachhang == taikhoan.makhachhang).ToList();
            decimal tongtien = 0;
            foreach (var item in dssp)
            {
                tongtien = tongtien + (decimal)(item.SanPham.giaban * item.soluong);
            }
            DonHang donHang = new DonHang();
            string thanhtoan = Request.Form["thanhtoan"];
            
            donHang.makhachhang = taikhoan.makhachhang;
            donHang.diachi = Request.Form["diachi"] +", " + Request.Form["phuongxa"] + ", " + Request.Form["huyen"] + "," + Request.Form["thanhpho"];
            donHang.tongtien = tongtien;
            donHang.trangthai = false;
            donHang.thanhtoan = thanhtoan == "banking" ? "Thanh toán VnPay" : "Thanh toán khi nhận hàng";
            donHang.ngaydat = DateTime.Now;
            donHang.ngaynhan = donHang.ngaydat.AddDays(3);     
            taikhoan.diachi = Request.Form["dienthoai"];
            taikhoan.tinh = Request.Form["thanhpho"];
            taikhoan.huyen = Request.Form["huyen"];
            taikhoan.xa = Request.Form["phuongxa"];
            taikhoan.thon = Request.Form["diachi"];
            donHang.dienthoai = Request.Form["dienthoai"];
            taikhoan.dienthoai = donHang.dienthoai;
            db.DonHangs.Add(donHang);
            db.SaveChanges();
            var strSanPham = "";
            foreach (var item in dssp)
            {
                DonHangChiTiet dhct = new DonHangChiTiet();
                dhct.madonhang = donHang.madonhang;
                dhct.masp = item.masp;
                dhct.soluong = item.soluong;
                dhct.gia = item.SanPham.giaban;
                var sanpham = db.SanPhams.Find(item.masp);
                sanpham.soluong = sanpham.soluong - item.soluong;

                //data to send mail
                strSanPham += "<tr>";
                strSanPham += "<td>" + item.SanPham.tensp + "</td>";
                strSanPham += "<td>" + item.soluong + "</td>";
                strSanPham += "<td>" + item.SanPham.giaban + "</td>";
                strSanPham += "</tr>";

                db.GioHangs.Remove(item);
                db.DonHangChiTiets.Add(dhct);
                db.SaveChanges();
            }
            Session["giohang"] = 0;

            if (thanhtoan == "banking")
            {
                string url = UrlPayment(1, donHang.madonhang);
                return Redirect(url);
            }
            else if (thanhtoan == "momo")
            {
                string url = UrlPayment(2, donHang.madonhang);
                return Redirect(url);
            }

            //send mail
            string contentCustomer = System.IO.File.ReadAllText(Server.MapPath("~/Content/Template/send2.html"));
            contentCustomer = contentCustomer.Replace("{{madon}}", donHang.madonhang.ToString());
            contentCustomer = contentCustomer.Replace("{{sanpham}}", strSanPham);
            contentCustomer = contentCustomer.Replace("{{hoten}}", taikhoan.hoten);
            contentCustomer = contentCustomer.Replace("{{ngaydat}}", donHang.ngaydat.ToString());
            contentCustomer = contentCustomer.Replace("{{tongtien}}", tongtien.ToString("#,##0"));
            contentCustomer = contentCustomer.Replace("{{diachi}}", donHang.diachi);
            contentCustomer = contentCustomer.Replace("{{dienthoai}}", taikhoan.dienthoai);
            contentCustomer = contentCustomer.Replace("{{email}}", taikhoan.email);
            DoAn.Common.MailHeper.sendEmail("An Phát Computer", "Đơn Hàng #" + donHang.madonhang, contentCustomer.ToString(), taikhoan.email);

            return RedirectToAction("Index", "DonHangs");
        }

        public ActionResult DatHang1()
        {

            List<DonHangChiTiet> dhct = db.DonHangChiTiets.ToList();
            foreach (var item in dhct)
            {
                db.DonHangChiTiets.Remove(item);
            }
            List<DonHang> dh = db.DonHangs.ToList();
            foreach (var item in dh)
            {
                db.DonHangs.Remove(item);
            }
            db.SaveChanges();

            return RedirectToAction("Index", "DonHangs");
        }

        public string UrlPayment(int typePayment, int orderID)
        {
            var urlPayment = "";
            DonHang donHang = db.DonHangs.FirstOrDefault(d => d.madonhang == orderID);

            if (typePayment == 1)
            {
                //VNPay
                //Get Config Info
                string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"]; //URL nhan ket qua tra ve 
                string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"]; //URL thanh toan cua VNPAY 
                string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"]; //Ma định danh merchant kết nối (Terminal Id)
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Secret Key

                //Build URL for VNPAY
                VnPayLibrary vnpay = new VnPayLibrary();

                vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", (donHang.tongtien * 100).ToString()); //Số tiền thanh toán. Số tiền không mang các ký tự phân tách thập phân, phần nghìn, ký tự tiền tệ. Để gửi số tiền thanh toán là 100,000 VND (một trăm nghìn VNĐ) thì merchant cần nhân thêm 100 lần (khử phần thập phân), sau đó gửi sang VNPAY là: 10000000
                
                vnpay.AddRequestData("vnp_BankCode", "VNPAYQR");
                vnpay.AddRequestData("vnp_CreateDate", donHang.ngaydat.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
                vnpay.AddRequestData("vnp_Locale", "vn");

                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: #DH" + donHang.madonhang);
                vnpay.AddRequestData("vnp_OrderType", "other"); //default value: other

                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", donHang.madonhang.ToString()); // Mã tham chiếu của giao dịch tại hệ thống của merchant. Mã này là duy nhất dùng để phân biệt các đơn hàng gửi sang VNPAY. Không được trùng lặp trong ngày

                urlPayment = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            }
            else if (typePayment == 2)
            {
                //Momo
                MomoLibrary momo = new MomoLibrary();
                var khachHang = db.KhachHangs.Find(donHang.makhachhang);
                urlPayment = momo.CreatePaymentRequest(
                    donHang.madonhang.ToString(),
                    "Thanh toan don hang: #DH" + donHang.madonhang,
                    (long)(donHang.tongtien * 100),
                    khachHang.hoten,
                    khachHang.email,
                    khachHang.dienthoai
                );
            }

            return urlPayment;
        }

        public ActionResult VnpayReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"]; //Chuỗi bí mật
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    //get all querystring data
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }
                //Lay danh sach tham so tra ve tu VNPAY
                //vnp_TxnRef: Ma don hang merchant gui VNPAY tai command=pay    
                //vnp_TransactionNo: Ma GD tai he thong VNPAY
                //vnp_ResponseCode:Response code from VNPAY: 00: Thanh cong, Khac 00: Xem tai lieu
                //vnp_SecureHash: HmacSHA512 cua du lieu tra ve

                long orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
                long vnpayTranId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];
                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        //Thanh toan thanh cong
                        var donHang = db.DonHangs.Find(orderId);
                        if (donHang != null)
                        {
                            donHang.trangthai = true;
                            db.SaveChanges();
                            ViewBag.ThanhToanThanhCong = true;
                            ViewBag.MaDonHang = donHang.madonhang;
                            ViewBag.SoTien = donHang.tongtien;
                        }
                    }
                    else
                    {
                        //Thanh toan khong thanh cong. Ma loi: vnp_ResponseCode
                        ViewBag.ThanhToanThanhCong = false;
                    }
                }
                else
                {
                    ViewBag.ThanhToanThanhCong = false;
                }
            }
            return View();
        }

        public ActionResult MomoReturn()
        {
            if (Request.QueryString.Count > 0)
            {
                string partnerCode = Request.QueryString["partnerCode"];
                string orderId = Request.QueryString["orderId"];
                string resultCode = Request.QueryString["resultCode"];
                string message = Request.QueryString["message"];

                if (resultCode == "0")
                {
                    //Thanh toan thanh cong
                    var donHang = db.DonHangs.Find(int.Parse(orderId));
                    if (donHang != null)
                    {
                        donHang.trangthai = true;
                        db.SaveChanges();
                        ViewBag.ThanhToanThanhCong = true;
                        ViewBag.MaDonHang = donHang.madonhang;
                        ViewBag.SoTien = donHang.tongtien;
                    }
                }
                else
                {
                    ViewBag.ThanhToanThanhCong = false;
                }
            }
            return View("VnpayReturn");
        }

        [HttpPost]
        public ActionResult MomoNotify()
        {
            try
            {
                string rawBody = new StreamReader(Request.InputStream).ReadToEnd();
                string signature = Request.Headers["X-Signature"];

                var momo = new MomoLibrary();
                if (momo.ValidateCallback(rawBody, signature))
                {
                    JObject data = JObject.Parse(rawBody);
                    string orderId = data["orderId"].ToString();
                    string resultCode = data["resultCode"].ToString();

                    if (resultCode == "0")
                    {
                        var donHang = db.DonHangs.Find(int.Parse(orderId));
                        if (donHang != null)
                        {
                            donHang.trangthai = true;
                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Log error
            }

            return new HttpStatusCodeResult(200);
        }

    }
}
