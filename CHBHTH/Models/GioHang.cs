using CHBHTH.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CHBHTH.Models
{
    public class GioHang
    {
        //private int iMaSP;

        //public int IMaSP
        //{
        //    get { return iMaSP; }
        //    set { iMaSP = value; }
        //}
        private QLbanhang db = new QLbanhang();
        public int iMasp { get; set; }
        public string sTensp { get; set; }
        public string sAnhBia { get; set; }
        public double dDonGia { get; set; }
        public int iSoLuong { get; set; }
        public double ThanhTien
        {
            get { return iSoLuong * dDonGia; }
        }
        //Hàm tạo cho giỏ hàng
        public GioHang(int Masp)
        {
            iMasp = Masp;
            SanPham sp = db.SanPhams.Single(n => n.MaSP == iMasp);
            sTensp = sp.TenSP;
            sAnhBia = sp.AnhSP;
            dDonGia = double.Parse(sp.GiaBan.ToString());
            iSoLuong = 1;
        }
        public class MomoPaymentResponse
        {
            public ResponseDetails Response { get; set; }
        }

        public class ResponseDetails
        {
            public string RequestId { get; set; }
            public int ErrorCode { get; set; }
            public string Message { get; set; }
            public string PayUrl { get; set; }
            // Các trường khác có thể được thêm vào nếu cần thiết
        }   
    }
}