using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CHBHTH.Models;
using Newtonsoft.Json;
using static CHBHTH.Models.GioHang;
using static CHBHTH.Models.SanPham;

namespace CHBHTH.Controllers
{
    public class GioHangController : Controller
    {
        private readonly HttpClient _httpClient;
        private List<SanPham> _sanPham;
        private QLbanhang db = new QLbanhang();
        // GET: GioHang
        //Lấy giỏ hàng 
        public GioHangController()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:5182/")
            };

            // Gọi API và lưu vào _sanPham
            _sanPham = GetSanPhamsFromApi();
        }
        public List<GioHang> LayGioHang()
        {
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang == null)
            {
                //Nếu giỏ hàng chưa tồn tại thì mình tiến hành khởi tao list giỏ hàng (sessionGioHang)
                lstGioHang = new List<GioHang>();
                Session["GioHang"] = lstGioHang;
            }
            return lstGioHang;
        }

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
        //Thêm giỏ hàng
        public ActionResult ThemGioHang(int iMasp, string strURL)
        {
            SanPham sp = _sanPham.SingleOrDefault(n => n.MaSP == iMasp);
            if (sp == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            //Lấy ra session giỏ hàng
            List<GioHang> lstGioHang = LayGioHang();
            //Kiểm tra sp này đã tồn tại trong session[giohang] chưa
            GioHang sanpham = lstGioHang.Find(n => n.iMasp == iMasp);
            if (sanpham == null)
            {
                sanpham = new GioHang(iMasp);
                //Add sản phẩm mới thêm vào list
                lstGioHang.Add(sanpham);
                return Redirect(strURL);
            }
            else
            {
                sanpham.iSoLuong++;
                return Redirect(strURL);
            }
        }
        //Cập nhật giỏ hàng 
        public ActionResult CapNhatGioHang(int iMaSP, FormCollection f)
        {
            //Kiểm tra masp
            SanPham sp = _sanPham.SingleOrDefault(n => n.MaSP == iMaSP);
            //Nếu get sai masp thì sẽ trả về trang lỗi 404
            if (sp == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            //Lấy giỏ hàng ra từ session
            List<GioHang> lstGioHang = LayGioHang();
            //Kiểm tra sp có tồn tại trong session["GioHang"]
            GioHang sanpham = lstGioHang.SingleOrDefault(n => n.iMasp == iMaSP);
            //Nếu mà tồn tại thì chúng ta cho sửa số lượng
            if (sanpham != null)
            {
                sanpham.iSoLuong = int.Parse(f["txtSoLuong"].ToString());

            }
            return RedirectToAction("GioHang");
        }
        //Xóa giỏ hàng
        public ActionResult XoaGioHang(int iMaSP)
        {
            //Kiểm tra masp
            SanPham sp = _sanPham.SingleOrDefault(n => n.MaSP == iMaSP);
            //Nếu get sai masp thì sẽ trả về trang lỗi 404
            if (sp == null)
            {
                Response.StatusCode = 404;
                return null;
            }
            //Lấy giỏ hàng ra từ session
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sanpham = lstGioHang.SingleOrDefault(n => n.iMasp == iMaSP);
            //Nếu mà tồn tại thì chúng ta cho sửa số lượng
            if (sanpham != null)
            {
                lstGioHang.RemoveAll(n => n.iMasp == iMaSP);

            }
            if (lstGioHang.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("GioHang");
        }
        //Xây dựng trang giỏ hàng
        public ActionResult GioHang()
        {
            if (Session["GioHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            List<GioHang> lstGioHang = LayGioHang();
            return View(lstGioHang);
        }
        //Tính tổng số lượng và tổng tiền
        //Tính tổng số lượng
        private int TongSoLuong()
        {
            int iTongSoLuong = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                iTongSoLuong = lstGioHang.Sum(n => n.iSoLuong);
            }
            return iTongSoLuong;
        }
        //Tính tổng thành tiền
        private double TongTien()
        {
            double dTongTien = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                dTongTien = lstGioHang.Sum(n => n.ThanhTien);
            }
            return dTongTien;
        }
        //tạo partial giỏ hàng
        public ActionResult GioHangPartial()
        {
            if (TongSoLuong() == 0)
            {
                return PartialView();
            }
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            return PartialView();
        }
        //Xây dựng 1 view cho người dùng chỉnh sửa giỏ hàng
        public ActionResult SuaGioHang()
        {
            if (Session["GioHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            List<GioHang> lstGioHang = LayGioHang();
            return View(lstGioHang);

        }

        #region 
        //Xây dựng chức năng đặt hàng
        [HttpPost]
        public async Task<ActionResult> DatHang(FormCollection donhangForm)
        {
            // Kiểm tra đăng nhập
            if (Session["use"] == null || Session["use"].ToString() == "")
            {
                return RedirectToAction("Dangnhap", "User");
            }

            // Kiểm tra giỏ hàng
            if (Session["GioHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy thông tin từ form
            string diachinhanhang = donhangForm["Diachinhanhang"].ToString();
            string thanhtoan = donhangForm["MaTT"].ToString();
            int ptthanhtoan = Int32.Parse(thanhtoan);

            // Lấy tài khoản từ session
            TaiKhoan kh = Session["use"] as TaiKhoan;
            if (kh == null)
            {
                return RedirectToAction("Dangnhap", "User");
            }

            // Lấy giỏ hàng từ session
            List<GioHang> gh = LayGioHang();

            // Lấy tất cả sản phẩm từ API
            List<SanPham> sanPhams = GetSanPhams();
            if (sanPhams == null)
            {
                ModelState.AddModelError("", "Không thể lấy thông tin sản phẩm từ hệ thống. Vui lòng thử lại.");
                return View("GioHang", gh);
            }
            var productsToUpdate = new List<UpdateSoLuongSanPham>();
            // Kiểm tra số lượng sản phẩm trong giỏ hàng
            foreach (var item in gh)
            {
                var sanPham = sanPhams.FirstOrDefault(sp => sp.MaSP == item.iMasp);
                if (sanPham == null)
                {
                    ModelState.AddModelError("", "Sản phẩm không tồn tại trong hệ thống.");
                    return View("GioHang", gh);
                }

                // Kiểm tra số lượng sản phẩm có đủ trong kho không
                if (sanPham.Soluong < item.iSoLuong)
                {
                    ModelState.AddModelError("", $"Sản phẩm {sanPham.TenSP} chỉ còn {sanPham.Soluong} sản phẩm, vui lòng giảm số lượng.");
                    return View("GioHang", gh);
                }

                // Tính số lượng cần cập nhật (số lượng trong giỏ hàng sẽ trừ đi số lượng trong kho)
                int? newQuantity = sanPham.Soluong - item.iSoLuong;

                // Thêm thông tin sản phẩm và số lượng vào danh sách cần cập nhật
                productsToUpdate.Add(new UpdateSoLuongSanPham
                {
                    MaSP = item.iMasp,
                    SoLuong = newQuantity
                });
            }
            bool updateSuccess = await UpdateProductQuantitiesInCart(productsToUpdate);
            if (!updateSuccess)
            {
                ModelState.AddModelError("", "Cập nhật số lượng sản phẩm thất bại. Vui lòng thử lại.");
                return View("GioHang", gh);
            }
            // Tính tổng tiền
            decimal tongtien = gh.Sum(item => item.iSoLuong * (decimal)item.dDonGia);

            // Tạo đối tượng đơn hàng
            var newOrder = new
            {
                maNguoiDung = kh.MaNguoiDung,
                thanhToan = ptthanhtoan,
                diaChiNhanHang = diachinhanhang,
                orderDetails = gh.Select(item => new
                {
                    maSP = item.iMasp,
                    soLuong = item.iSoLuong,
                    donGia = (decimal)item.dDonGia,
                    thanhTien = item.iSoLuong * (decimal)item.dDonGia
                }).ToList() // Ensure orderDetails is a list of items
            };
            string token = Session["token"] as string;
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Dangnhap", "User");
            }
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://localhost:5182/order/");
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                // Serialize dữ liệu thành JSON
                string jsonData = JsonConvert.SerializeObject(newOrder);
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Gửi yêu cầu POST để thêm đơn hàng
                var response = httpClient.PostAsync("AddOrder", content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    // Xử lý lỗi nếu không thêm được đơn hàng
                    ModelState.AddModelError("", "Không thể tạo đơn hàng. Vui lòng thử lại sau.");
                    return View();
                }

                // Lấy thông tin đơn hàng vừa tạo (nếu cần)
                var createdOrder = JsonConvert.DeserializeObject<dynamic>(response.Content.ReadAsStringAsync().Result);

                if (createdOrder == null)
                {
                    ModelState.AddModelError("", "Không thể lấy thông tin đơn hàng.");
                    return View();
                }

                // Lấy mã đơn hàng từ phản hồi API (dựa trên mã đơn trả về từ API AddOrder)
                int maDon = createdOrder.maDon;

                // Nếu không thành công, hiển thị thông báo lỗi
                ViewBag.ErrorMessage = "Đã có lỗi xảy ra trong quá trình thanh toán!";
                // Gửi chi tiết đơn hàng tới API AddDetailsOrder
                foreach (var item in gh)
                {
                    var orderDetail = new
                    {
                        maSP = item.iMasp,
                        soLuong = item.iSoLuong,
                        donGia = (decimal)item.dDonGia,
                        thanhTien = item.iSoLuong * (decimal)item.dDonGia
                    };

                    // Serialize chi tiết đơn hàng thành JSON
                    string detailJson = JsonConvert.SerializeObject(orderDetail);
                    StringContent detailContent = new StringContent(detailJson, Encoding.UTF8, "application/json");

                    // Gửi yêu cầu POST để thêm chi tiết đơn hàng
                    var detailResponse = httpClient.PostAsync($"AddDetailsOrder/{maDon}", detailContent).Result;

                    if (!detailResponse.IsSuccessStatusCode)
                    {
                        // Xử lý lỗi nếu không thêm được chi tiết đơn hàng
                        ModelState.AddModelError("", "Không thể thêm chi tiết đơn hàng. Vui lòng thử lại sau.");
                        return View();
                    }
                }
                if (ptthanhtoan == 2)
                {
                    // Nếu MaTT = 1 (Thanh toán trực tuyến), lấy URL thanh toán
                    var responseJson = await GetPaymentUrl(kh.HoTen, maDon.ToString(), "Thanh toán cho đơn hàng", (double)tongtien);
                    if (string.IsNullOrEmpty(responseJson))
                    {
                        ModelState.AddModelError("", "Không thể tạo URL thanh toán.");
                        return View();
                    }
                    var momoResponse = JsonConvert.DeserializeObject<MomoPaymentResponse>(responseJson);
                    if (momoResponse?.Response?.ErrorCode == 0)
                    {
                        string paymentUrl = momoResponse.Response.PayUrl;

                        if (string.IsNullOrEmpty(paymentUrl))
                        {
                            ModelState.AddModelError("", "Không thể tạo URL thanh toán.");
                            return View();
                        }
                        Session["OrderId"] = momoResponse.Response.RequestId;
                        Session["MaDon"] = maDon;
                        return Redirect(paymentUrl);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Có lỗi xảy ra khi tạo đơn thanh toán.");
                        return View();
                    }
                }
                else
                {
                    Session["GioHang"] = null;
                    return RedirectToAction("Index", "Donhangs");
                }
            }
            //Session["GioHang"] = null;
            //// Chuyển hướng đến trang danh sách đơn hàng
            //return RedirectToAction("Index", "Donhangs");
        }

        #endregion
        public async Task<ActionResult> ThanhToanDonHang()
        {
            // Chuẩn bị danh sách phương thức thanh toán
            ViewBag.MaTT = new SelectList(new[]
            {
        new { MaTT = 1, TenPT = "Thanh toán tiền mặt" },
        new { MaTT = 2, TenPT = "Thanh toán chuyển khoản" }
    }, "MaTT", "TenPT", 1);

            // Kiểm tra đăng nhập
            if (Session["use"] == null || string.IsNullOrEmpty(Session["use"].ToString()))
            {
                return RedirectToAction("Dangnhap", "User");
            }

            // Lấy thông tin tài khoản từ session
            TaiKhoan taiKhoan = Session["use"] as TaiKhoan;
            if (taiKhoan == null)
            {
                return RedirectToAction("Dangnhap", "User");
            }

            // Lấy giỏ hàng từ session
            List<GioHang> gh = LayGioHang();

            // Tính tổng tiền
            decimal tongtien = gh.Sum(item => item.iSoLuong * (decimal)item.dDonGia);

            // Chuẩn bị đối tượng đơn hàng
            DonHang ddh = new DonHang
            {
                MaNguoiDung = taiKhoan.MaNguoiDung,
                NgayDat = DateTime.Now,
                DiaChiNhanHang = taiKhoan.Diachi
            };

            ViewBag.tongtien = tongtien;
            ViewBag.MaNguoiDung = taiKhoan.MaNguoiDung; // Truyền trực tiếp giá trị vào ViewBag
            return View(ddh);
        }
        private async Task<string> GetPaymentUrl(string fullName, string orderId, string orderInfo, double amount)
        {
            using (var client = new HttpClient())
            {
                var url = "https://localhost:5182/order/CreatePaymentUrl";
                var requestData = new
                {
                    fullName = fullName,
                    orderId = orderId,
                    orderInfo = orderInfo,
                    amount = amount
                };

                var json = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    // Gửi yêu cầu POST tới API
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Trả về trực tiếp nội dung phản hồi từ API dưới dạng chuỗi
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        // Xử lý khi API trả về lỗi
                        return JsonConvert.SerializeObject(new { error = "API trả về lỗi, không thể lấy dữ liệu." });
                    }
                }
                catch (Exception ex)
                {
                    // Xử lý ngoại lệ nếu có lỗi khi gọi API
                    return JsonConvert.SerializeObject(new { error = $"Có lỗi xảy ra khi gọi API: {ex.Message}" });
                }
            }
        }
        private List<SanPham> GetSanPhams()
        {
            var response = _httpClient.GetAsync("product").Result;
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<List<SanPham>>(content);
        }
        private async Task<bool> UpdateProductQuantitiesInCart(List<UpdateSoLuongSanPham> gioHang)
        {
            var updateRequest = gioHang.Select(item => new UpdateSoLuongSanPham
            {
                MaSP = item.MaSP,
                SoLuong = item.SoLuong
            }).ToList();

            // Gửi yêu cầu PUT đến API
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://localhost:5182/product/");
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Serialize dữ liệu thành JSON
                string jsonData = JsonConvert.SerializeObject(updateRequest);
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                // Gửi yêu cầu PUT
                var response = await httpClient.PutAsync("UpdateSoluong", content);
                if (response.IsSuccessStatusCode)
                {
                    return true; // Cập nhật thành công
                }
                else
                {
                    return false; // Xảy ra lỗi khi cập nhật
                }
            }
        }

    }
}