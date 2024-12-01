using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using CHBHTH.Models;
using Newtonsoft.Json;
using static CHBHTH.Models.SanPham;


namespace CHBHTH.Controllers
{
    public class DonhangsController : Controller
    {
        private QLbanhang db = new QLbanhang();
        private readonly HttpClient _httpClient;
        private readonly string _orderApiUrl = "order";
        // GET: Donhangs
        // Hiển thị danh sách đơn hàng
        public DonhangsController()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:5182/")
            };
        }
        public async Task<ActionResult> Index()
        {
            // Kiểm tra đang đăng nhập
            if (Session["use"] == null || Session["use"].ToString() == "")
            {
                return RedirectToAction("Dangnhap", "User");
            }

            TaiKhoan kh = (TaiKhoan)Session["use"];
            int maND = kh.MaNguoiDung;
            var orderId = Session["OrderId"]?.ToString();

            if (string.IsNullOrEmpty(orderId))
            {
                // Xử lý nếu OrderId không tồn tại trong session
                ModelState.AddModelError("", "Không tìm thấy OrderId trong session.");
            }

            // Chờ kết quả từ phương thức kiểm tra trạng thái thanh toán
            var paymentStatus = await CheckPaymentStatusAsync(orderId);

            if (paymentStatus != null)
            {
                // Thêm thông tin trạng thái thanh toán vào ViewBag hoặc ViewData để hiển thị
                ViewBag.PaymentStatus = paymentStatus;
            }
            else
            {
                ModelState.AddModelError("", "Không thể lấy thông tin trạng thái thanh toán.");
            }

            // Gọi API lấy danh sách đơn hàng và chờ kết quả
            var allOrders = await GetOrdersFromApiAsync();

            // Lọc các đơn hàng theo MaNguoiDung
            var donhangs = allOrders.Where(d => d.MaNguoiDung == maND).ToList();

            return View(donhangs); // Trả về danh sách đơn hàng cho view
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
        // GET: Donhangs/Details/5
        //Hiển thị chi tiết đơn hàng
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DonHang donhang = db.DonHangs.Find(id);
            var chitiet = db.ChiTietDonHangs.Include(d => d.SanPham).Where(d => d.MaDon == id).ToList();
            if (donhang == null)
            {
                return HttpNotFound();
            }
            // Gọi API để kiểm tra trạng thái thanh toán
            return View(chitiet);
        }
        private async Task<string> CheckPaymentStatusAsync(string orderId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Xây dựng URL với tham số orderId trong query string
                    string requestUrl = $"https://localhost:5182/order/CheckPaymentStatus?orderId={orderId}";

                    // Gửi yêu cầu POST tới API với tham số trong query string
                    var response = await client.PostAsync(requestUrl, null); // Không có body trong yêu cầu POST này

                    // Kiểm tra mã trạng thái của phản hồi
                    if (response.IsSuccessStatusCode)
                    {
                        // Nếu thành công, đọc dữ liệu trả về
                        var result = await response.Content.ReadAsStringAsync();

                        // Deserialize JSON kết quả trả về để lấy message
                        var responseObject = JsonConvert.DeserializeObject<dynamic>(result);
                        string message = responseObject?.message;
                        string maDon = Session["MaDon"]?.ToString();
                        // Kiểm tra nếu message là "Thành công."
                        if (message == "Thành công.")
                        {
                            // Lấy mã đơn hàng từ session
                            

                            if (!string.IsNullOrEmpty(maDon))
                            {
                                // Gọi API PUT để cập nhật TinhTrang của đơn hàng
                                bool updateStatus = await UpdateOrderStatusAsync(maDon, 1);

                                if (updateStatus)
                                {
                                    Session["MaDon"] = null;
                                    return "Cập nhật trạng thái đơn hàng thành công.";
                                }
                                else
                                {
                                    return "Không thể cập nhật trạng thái đơn hàng.";
                                }
                            }
                            else
                            {
                                return "Mã đơn hàng không hợp lệ.";
                            }
                        }
                        else
                        {
                            List<GioHang> gh = LayGioHang();
                            List<SanPham> sanPhams = GetSanPhams();
                            var productsToUpdate = new List<UpdateSoLuongSanPham>();
                            foreach (var item in gh)
                            {
                                var sanPham = sanPhams.FirstOrDefault(sp => sp.MaSP == item.iMasp);


                                // Tính số lượng cần cập nhật (số lượng trong giỏ hàng sẽ trừ đi số lượng trong kho)
                                int? newQuantity = sanPham.Soluong + item.iSoLuong;

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
                                return $"Cập nhật số lượng sản phẩm thất bại khi chưa thanh toán thành công. Vui lòng thử lại.";
                            }
                            bool updateStatus = await UpdateOrderStatusAsync(maDon, 0);

                            if (updateStatus)
                            {
                                Session["MaDon"] = null;
                                Session["GioHang"] = null;
                                return $"Trạng thái thanh toán không thành công. Dữ liệu trả về: {message}";
                            }
                            else
                            {
                                return "Không thể cập nhật trạng thái đơn hàng.";
                            }
                        }
                    }
                    else
                    {
                        // Nếu không thành công, trả về lỗi
                        string errorMessage = $"Lỗi khi gọi API: {response.StatusCode} - {response.ReasonPhrase}";
                        ModelState.AddModelError("", errorMessage);
                        return errorMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                string errorMessage = $"Lỗi khi gọi API: {ex.Message}";
                ModelState.AddModelError("", errorMessage);
                return errorMessage;
            }
        }
        private async Task<bool> UpdateOrderStatusAsync(string maDon, int tinhTrang)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Chuyển giá trị TinhTrang thành chuỗi và gửi như là raw body
                    var content = new StringContent(tinhTrang.ToString(), Encoding.UTF8, "application/json");

                    // Gửi yêu cầu PUT để cập nhật trạng thái đơn hàng
                    var response = await client.PutAsync($"https://localhost:5182/order/orders/{maDon}", content);

                    // Kiểm tra mã trạng thái của phản hồi
                    if (response.IsSuccessStatusCode)
                    {
                        return true;  // Thành công
                    }
                    else
                    {
                        // Đọc và trả về lỗi nếu không thành công
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Lỗi khi gọi API Update: {errorMessage}");
                        return false;  // Lỗi
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu có
                Console.WriteLine($"Lỗi khi gọi API Update: {ex.Message}");
                return false;
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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        private async Task<IEnumerable<DonHang>> GetOrdersFromApiAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_orderApiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var orders = JsonConvert.DeserializeObject<IEnumerable<DonHang>>(content);
                    return orders;
                }
                else
                {
                    return new List<DonHang>();
                }
            }
            catch (Exception ex)
            {
                return new List<DonHang>();
            }
        }
    }
}
