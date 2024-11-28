using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Drone2.Models;

namespace Drone2.Models
{
    public class Booth
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [RegularExpression(@"^(?!\d+$)[a-zA-Z0-9\s]+$", ErrorMessage = "Location must contain at least one letter and cannot be numbers only.")]
        public string? Location { get; set; }


        [Required(ErrorMessage = "Size is required.")]
        [RegularExpression(@"^(\d+)\sX\s(\d+)\sX\s(\d+)$", ErrorMessage = "Size must be in the format '20 X 20 X 30' and contain only positive numbers.")]
        public string? Size { get; set; }

        [Required(ErrorMessage = "Price per day is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Price per day must be greater than 0.")]
        public int PricePerDay { get; set; }
        public bool IsBooked { get; set; }
        public string? UploadedBy { get; set; } // Username ของผู้ที่อัปโหลด

        public string? FileName { get; set; } // ชื่อไฟล์ที่อัปโหลด
        public string? FilePath { get; set; } // เส้นทางไฟล์

        // ความสัมพันธ์กับ Reserve
        public ICollection<Reserve>? Reserves { get; set; }
    }



    public class Reserve
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string UserName { get; set; }
        

        [Required]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]
        public string? PhoneNumber { get; set; }
        [Required]
        public int BoothId { get; set; } // Foreign Key เชื่อมกับ Booth
        public Booth? Booth { get; set; }
        public string? ReservedBy { get; set; } // Username ของผู้ที่จอง

    }
    public class Deleted
    {
        public List<Booth>? UploadedBooths { get; set; } // บูธที่อัปโหลด
        public List<Reserve> ?ReservedBooths { get; set; } // บูธที่จอง
    }



}


