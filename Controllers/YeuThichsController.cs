using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using DoAn.Models;

namespace DoAn.Controllers
{
    public class YeuThichsController : Controller
    {
        private Model1 db = new Model1();

        // GET: YeuThichs
        public ActionResult Index()
        {
            if (Session["ma"] == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var yeuThichs = db.YeuThichs.Include(y => y.KhachHang).Include(y => y.SanPham)
                .Where(y => y.makhachhang == (int)Session["ma"]);
            return View(yeuThichs.ToList());
        }

        // POST: YeuThichs/AddToFavorites
        [HttpPost]
        public ActionResult AddToFavorites(int masp)
        {
            if (Session["ma"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thêm sản phẩm yêu thích" });
            }

            try
            {
                var makhachhang = (int)Session["ma"];
                
                // Kiểm tra sản phẩm đã được yêu thích chưa
                var existingFavorite = db.YeuThichs
                    .FirstOrDefault(y => y.makhachhang == makhachhang && y.masp == masp);

                if (existingFavorite != null)
                {
                    // Nếu đã yêu thích thì xóa
                    db.YeuThichs.Remove(existingFavorite);
                    db.SaveChanges();
                    return Json(new { success = true, message = "Đã xóa khỏi danh sách yêu thích" });
                }

                // Nếu chưa yêu thích thì thêm mới
                var yeuThich = new YeuThich
                {
                    makhachhang = makhachhang,
                    masp = masp,
                    ngaythem = DateTime.Now
                };

                db.YeuThichs.Add(yeuThich);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã thêm vào danh sách yêu thích" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: YeuThichs/Count
        public ActionResult Count()
        {
            if (Session["ma"] == null)
            {
                return Json(new { count = 0 });
            }

            var count = db.YeuThichs.Count(y => y.makhachhang == (int)Session["ma"]);
            return Json(new { count = count });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 