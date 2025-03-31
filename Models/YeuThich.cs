using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAn.Models
{
    [Table("YeuThich")]
    public partial class YeuThich
    {
        [Key]
        public int mayeuthich { get; set; }
        public int makhachhang { get; set; }
        public int masp { get; set; }
        public DateTime ngaythem { get; set; }

        [ForeignKey("makhachhang")]
        public virtual KhachHang KhachHang { get; set; }

        [ForeignKey("masp")]
        public virtual SanPham SanPham { get; set; }
    }
} 