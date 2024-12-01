using CHBHTH.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Collections.Generic;

namespace CHBHTH.Controllers
{
    public class UserController : Controller
    {
        // Đường dẫn API tuyệt đối
        private readonly string apiBaseUrl = "https://localhost:5182/user";

        // ĐĂNG KÝ
        public ActionResult Dangky()
        {
            return View();
        }

        // ĐĂNG KÝ PHƯƠNG THỨC POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Dangky(TaiKhoan taiKhoan)
        {
            try
            {
                // Bỏ qua kiểm tra SSL (chỉ nên dùng trong môi trường phát triển)
                System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;

                if (ModelState.IsValid)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(apiBaseUrl); // Đảm bảo BaseAddress đúng

                        // Serialize đối tượng TaiKhoan thành JSON
                        var json = JsonConvert.SerializeObject(taiKhoan);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        // Gửi yêu cầu POST đến API Register với đường dẫn tuyệt đối
                        var response = await client.PostAsync($"{apiBaseUrl}/Register", content);

                        if (response.IsSuccessStatusCode)
                        {
                            ViewBag.RegOk = "Đăng ký thành công. Đăng nhập ngay!";
                            return View("Dangky");
                        }
                        else
                        {
                            var errorResponse = await response.Content.ReadAsStringAsync();
                            ViewBag.RegOk = $"Đăng ký thất bại: {errorResponse}";
                            return View("Dangky");
                        }
                    }
                }

                return View("Dangky");
            }
            catch (Exception ex)
            {
                ViewBag.RegOk = "Đã xảy ra lỗi: " + ex.Message;
                return View("Dangky");
            }
        }

        // ĐĂNG NHẬP
        [AllowAnonymous]
        public ActionResult Dangnhap()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Dangnhap(string userMail, string password)
        {
            try
            {
                // Bỏ qua kiểm tra SSL (chỉ nên dùng trong môi trường phát triển)
                System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;

                var loginRequest = new
                {
                    username = userMail,
                    password = password
                };

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(apiBaseUrl); // Đảm bảo BaseAddress đúng
                    var json = JsonConvert.SerializeObject(loginRequest);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Gửi yêu cầu POST đến API Login với đường dẫn tuyệt đối
                    var response = await client.PostAsync($"{apiBaseUrl}/Login", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var loginResponse = await response.Content.ReadAsStringAsync();
                        var loginResult = JsonConvert.DeserializeObject<LoginResponse>(loginResponse);

                        // Lưu token vào session
                        Session["token"] = loginResult.Token;

                        // Lưu thông tin người dùng vào session
                        Session["use"] = loginResult.User;

                        // Kiểm tra nếu là admin hoặc người dùng thông thường
                        if (userMail == "Admin@gmail.com")
                        {
                            return RedirectToAction("Index", "Admin/Home");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(errorResponse))
                        {
                            ViewBag.Fail = "Tài khoản hoặc mật khẩu không chính xác.";
                        }
                        else
                        {
                            ViewBag.Fail = $"Tài khoản hoặc mật khẩu không chính xác. Chi tiết: {errorResponse}";
                        }
                        return View("Dangnhap");
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Fail = "Đã xảy ra lỗi: " + ex.Message;
                return View("Dangnhap");
            }
        }
        public ActionResult Profile(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TaiKhoan taiKhoan = Session["use"] as TaiKhoan;
            if (taiKhoan == null)
            {
                return HttpNotFound();
            }
            return View(taiKhoan);
        }
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TaiKhoan taiKhoan = Session["use"] as TaiKhoan;
            if (taiKhoan == null)
            {
                return HttpNotFound();
            }
            var danhSachQuyen = new List<SelectListItem>
    {
        new SelectListItem { Text = "Admin", Value = "1" },
        new SelectListItem { Text = "User", Value = "2" }
    };
            ViewBag.IDQuyen = new SelectList(danhSachQuyen, "Value", "Text", taiKhoan.IDQuyen);
            return View(taiKhoan);
        }
        public async Task<ActionResult> UpdateProfile(TaiKhoan model)
        {
            // Kiểm tra nếu ModelState hợp lệ
            if (ModelState.IsValid)
            {
                // Lấy thông tin người dùng từ session
                TaiKhoan taiKhoan = Session["use"] as TaiKhoan;
                if (taiKhoan == null)
                {
                    return HttpNotFound();
                }

                // Gán giá trị MaNguoiDung và IDQuyen từ session
                model.MaNguoiDung = taiKhoan.MaNguoiDung;
                model.IDQuyen = taiKhoan.IDQuyen;

                // Tạo HttpClient để gọi API
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://localhost:5182/");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    // Chuyển đối tượng model thành JSON
                    var json = JsonConvert.SerializeObject(model);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Gửi PUT request tới API
                    HttpResponseMessage response = await client.PutAsync("user/Edit", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Nếu thành công, lấy thông tin người dùng mới và trả lại view
                        var updatedTaiKhoan = await response.Content.ReadAsAsync<TaiKhoan>();
                        ViewBag.Message = "Cập nhật thông tin thành công!";
                        return View("Edit", updatedTaiKhoan); // Trả về thông tin người dùng cập nhật lên view Edit
                    }
                    else
                    {
                        // Nếu thất bại, thông báo lỗi
                        ViewBag.Message = "Cập nhật không thành công, vui lòng thử lại!";
                    }
                }
            }

            // Nếu ModelState không hợp lệ, trả lại view Edit với thông báo lỗi
            ViewBag.Message = "Dữ liệu không hợp lệ!";
            return View("Edit", model);
        }

        // ĐĂNG XUẤT
        public ActionResult DangXuat()
        {
            Session["token"] = null;
            Session["use"] = null;
            return RedirectToAction("Index", "Home");
        }
    }
}
