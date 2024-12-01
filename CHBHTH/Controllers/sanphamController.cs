using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;
using Newtonsoft.Json;
using CHBHTH.Models;

namespace CHBHTH.Controllers
{
    public class sanphamController : Controller
    {
        private readonly HttpClient _httpClient;
        private List<SanPham> _sanPham; // Biến lưu trữ danh sách sản phẩm

        public sanphamController()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:5182/")
            };

            // Gọi API và lưu vào _sanPham
            _sanPham = GetSanPhamsFromApi();
        }

        // GET: sanpham
        public ActionResult Index()
        {
            if (_sanPham == null || !_sanPham.Any())
            {
                return HttpNotFound("Không có sản phẩm nào");
            }

            return View(_sanPham);
        }

        public ActionResult suapartial()
        {
            if (_sanPham == null || !_sanPham.Any())
            {
                return null;
            }

            var ip = _sanPham.Where(n => n.MaLoai == 1).Take(4).ToList();
            return PartialView(ip);
        }

        public ActionResult raupartial()
        {
            if (_sanPham == null || !_sanPham.Any())
            {
                return null;
            }

            var ip = _sanPham.Where(n => n.MaLoai == 2).Take(4).ToList();
            return PartialView(ip);
        }

        public ActionResult dauanpartial()
        {
            if (_sanPham == null || !_sanPham.Any())
            {
                return null;
            }

            var ip = _sanPham.Where(n => n.MaLoai == 3).Take(4).ToList();
            return PartialView(ip);
        }

        public ActionResult xemchitiet(int Masp = 0)
        {
            if (_sanPham == null)
            {
                return HttpNotFound("Không tìm thấy sản phẩm");
            }

            var sanPham = _sanPham.SingleOrDefault(n => n.MaSP == Masp);
            if (sanPham == null)
            {
                return HttpNotFound("Không tìm thấy sản phẩm");
            }

            // Kiểm tra giá trị MaLoai và set tên loại tương ứng
            string tenLoai = string.Empty;

            if (sanPham.MaLoai == 1)
            {
                tenLoai = "Sữa";
            }
            else if (sanPham.MaLoai == 2)
            {
                tenLoai = "Rau";
            }
            else if (sanPham.MaLoai == 3)
            {
                tenLoai = "Dầu ăn";
            }

            // Gán giá trị tên loại vào ViewBag để hiển thị trong View
            ViewBag.TenLoai = tenLoai;

            return View(sanPham);
        }


        public ActionResult xemchitietdanhmuc(int MaLoai)
        {
            if (_sanPham == null || !_sanPham.Any())
            {
                return null;
            }

            var ip = _sanPham.Where(n => n.MaLoai == MaLoai).ToList();
            return PartialView(ip);
        }

        // Hàm gọi API để lấy danh sách sản phẩm
        private List<SanPham> GetSanPhamsFromApi()
        {
            var response = _httpClient.GetAsync("product").Result;
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<List<SanPham>>(content);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
