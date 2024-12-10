using Microsoft.AspNetCore.Mvc;
using Drone2.Models;
using Microsoft.EntityFrameworkCore;
using Drone2.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Microsoft.Identity.Client;


namespace Drone2.Controllers
{
    [Authorize]
    public class BoothController : Controller
    {
        private readonly ApplicationDBContext _db;

        public BoothController(ApplicationDBContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db)); // ตรวจสอบว่า db ไม่เป็น null
        }

        // แสดงรายการ Booth ทั้งหมดหรือค้นหา Booth ด้วย ID
        [AllowAnonymous]
        public IActionResult Index(int? searchId, DateTime? searchDate)
        {
            // Include Reserves เพื่อดึงข้อมูลการจองทั้งหมด
            var allBooths = _db.Booths.Include(b => b.Reserves).AsQueryable();

            // กรองตาม Booth ID หากระบุ
            if (searchId.HasValue)
            {
                allBooths = allBooths.Where(b => b.Id == searchId.Value);
            }

            // กรอง Booth ที่ไม่มีการจองในวันที่ค้นหา
            if (searchDate.HasValue)
            {
                allBooths = allBooths.Where(b =>
                    !b.Reserves.Any(r => searchDate.Value >= r.StartDate && searchDate.Value <= r.EndDate));
            }

            ViewBag.searchId = searchId;
            ViewBag.searchDate = searchDate;

            return View(allBooths.ToList());
        }

        [Authorize]
        [HttpGet]
        public IActionResult Reserve(int? boothId)
        {
            if (HttpContext.Session.GetString("UserRole") == "Guest")
            {
                return RedirectToAction("Login", "Account");
            }

            // สร้าง Model เปล่า หรือกำหนดค่าเริ่มต้นให้กับฟอร์ม
            var model = new Reserve();

            if (boothId.HasValue)
            {
                model.BoothId = boothId.Value;
            }

            return View(model); // ส่ง Model ไปยัง View
        }

        [Authorize]
        [HttpGet("Booth/Reserve/{boothId:int}")]
        public IActionResult Reserve(int boothId)
        {
            var booth = _db.Booths.FirstOrDefault(b => b.Id == boothId);
            if (booth == null)
            {
                return NotFound(); // หากไม่พบ Booth
            }

            var reserve = new Reserve
            {
                Id = _db.Reserves.Any() ? _db.Reserves.Max(r => r.Id) + 1 : 1, // สร้างค่า ID ใหม่อัตโนมัติ
                BoothId = boothId // กรอก BoothId ให้อัตโนมัติ
            };

            return View(reserve); // ส่ง Model ไปยัง View
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(Reserve obj)
        {
            // ตรวจสอบว่า ID ซ้ำหรือไม่
            if (_db.Reserves.Any(r => r.Id == obj.Id))
            {
                ModelState.AddModelError("Id", "This ID is already in use. Please choose another.");
            }

            // ตรวจสอบว่ามี BoothId หรือไม่
            var booth = _db.Booths.FirstOrDefault(b => b.Id == obj.BoothId);
            if (booth == null)
            {
                ModelState.AddModelError("BoothId", "The Booth ID you entered does not exist.");
            }

            // ตรวจสอบวันที่
            if (obj.StartDate > obj.EndDate)
            {
                ModelState.AddModelError("EndDate", "End date must be later than the start date.");
            }

            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            obj.ReservedBy = User.Identity?.Name ?? "DefaultUser";
            _db.Reserves.Add(obj);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Reservation successfully created!";
            return RedirectToAction("Index");
        }



        [Authorize]
        [HttpGet]
        [Route("Booth/UploadFile")]
        public IActionResult UploadFile()
        {
            if (HttpContext.Session.GetString("UserRole") == "Guest")
            {
                return RedirectToAction("Login", "Account"); // Redirect กลับไปหน้า Login
            }

            return View("UploadFile");
        }
        [Authorize]
        [HttpPost]
        [Route("Booth/UploadDetails")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDetails(IFormFile File, string Location, string Size, decimal PricePerDay)
        {
            // Validate Location
            if (!Regex.IsMatch(Location, @"^(?!\d+$)[a-zA-Z0-9\s]+$"))
            {
                ModelState.AddModelError("Location", "Location must contain at least one letter and cannot be numbers only.");
            }

            // Validate Size
            if (!Regex.IsMatch(Size, @"^(\d+)\sX\s(\d+)\sX\s(\d+)$"))
            {
                ModelState.AddModelError("Size", "Size must be in the format '20 X 20 X 30' and contain only positive numbers.");
            }
            else
            {
                var dimensions = Size.Split('X').Select(s => s.Trim()).Select(int.Parse).ToArray();
                if (dimensions.Any(d => d <= 0))
                {
                    ModelState.AddModelError("Size", "Size values must be greater than 0.");
                }
            }

            // Validate PricePerDay
            if (PricePerDay <= 0)
            {
                ModelState.AddModelError("PricePerDay", "Price per day must be greater than 0.");
            }

            // Validate File
            if (File != null)
            {
                if (!(File.ContentType == "image/jpeg" || File.ContentType == "image/png" || File.ContentType == "application/pdf"))
                {
                    ModelState.AddModelError("File", "Invalid file type. Only JPG, PNG, and PDF are allowed.");
                }
                else if (File.Length > 1000000)
                {
                    ModelState.AddModelError("File", "File size must be less than 1000 KB.");
                }
            }

            // Check ModelState
            if (!ModelState.IsValid)
            {
                return View("UploadDetails"); // กลับไปยัง View พร้อมข้อผิดพลาด
            }

            // Save File
            string uniqueFileName = null;
            if (File != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                uniqueFileName = Guid.NewGuid().ToString() + "_" + File.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await File.CopyToAsync(fileStream);
                }
            }

            // Save Booth Details
            Booth booth = new Booth
            {
                Location = Location,
                Size = Size,
                PricePerDay = (int)PricePerDay,
                FileName = uniqueFileName,
                FilePath = uniqueFileName != null ? "/uploads/" + uniqueFileName : null,
                UploadedBy = User.Identity?.Name ?? "DefaultUser"
            };

            _db.Booths.Add(booth);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Booth uploaded successfully!";
            return RedirectToAction("Index");
        }

        [Authorize]
        public async Task<IActionResult> Delete()
        {
            // ตรวจสอบสิทธิ์ของผู้ใช้
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Guest" || string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            // ดึง Username ของผู้ใช้ที่เข้าสู่ระบบ
            var username = User.Identity.Name;

            // ดึงข้อมูลบูธที่ผู้ใช้คนนี้อัปโหลด
            var uploadedBooths = await _db.Booths
                .Where(b => b.UploadedBy == username)
                .ToListAsync();

            // ดึงข้อมูลการจองที่ผู้ใช้คนนี้จอง
            var reservedBooths = await _db.Reserves
                .Include(r => r.Booth)
                .Where(r => r.ReservedBy == username)
                .ToListAsync();

            // ส่งข้อมูลไปยัง View
            var model = new Deleted
            {
                UploadedBooths = uploadedBooths,
                ReservedBooths = reservedBooths
            };

            return View(model);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var obj = _db.Booths.Find(id);
            if (obj == null)
            {
                return NotFound();
            }
            _db.Booths.Remove(obj);
            _db.SaveChanges();
            return RedirectToAction("Delete");
        }

        [Authorize]
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var obj = _db.Booths.Find(id);
            if (obj == null)
            {
                return NotFound();
            }
            return View(obj); // ส่งข้อมูลบูธไปยัง View
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Edit(Booth obj)
        {
            if (ModelState.IsValid) // ตรวจสอบว่า ModelState ถูกต้อง
            {
                // ค้นหา Booth เดิมในฐานข้อมูล
                var boothInDb = _db.Booths.Find(obj.Id);
                if (boothInDb != null)
                {
                    // อัปเดตค่าของฟิลด์ต่าง ๆ
                    boothInDb.Location = obj.Location;
                    boothInDb.Size = obj.Size;
                    boothInDb.PricePerDay = obj.PricePerDay;
                    boothInDb.UploadedBy = User.Identity.Name ?? "DefaultUser"; // เพิ่มการอัปเดต UploadedBy

                    _db.SaveChanges(); // บันทึกการเปลี่ยนแปลงลงฐานข้อมูล
                    return RedirectToAction("Index"); // ย้อนกลับไปยังหน้า Index
                }

                // ถ้าไม่พบ Booth ในฐานข้อมูล ให้ส่ง NotFound
                return NotFound();
            }

            return View(obj); // ส่งข้อมูลกลับไปยัง View หากมีข้อผิดพลาด
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            // ค้นหา Reservation ด้วย Id
            var reservation = _db.Reserves?.FirstOrDefault(r => r.Id == id); // หรือใช้ .Find() หากต้องการ
            if (reservation == null)
            {
                return NotFound();
            }

            // ค้นหา Booth ที่เกี่ยวข้อง
            var booth = _db.Booths?.FirstOrDefault(b => b.Id == reservation.BoothId);
            if (booth != null)
            {
                booth.IsBooked = false; // เปลี่ยนสถานะ Booth เป็น Available
            }

            // ลบ Reservation
            _db.Reserves?.Remove(reservation);

            // บันทึกการเปลี่ยนแปลง
            await _db.SaveChangesAsync();

            return RedirectToAction("Delete"); // หรือหน้าอื่นที่ต้องการ
        }




    }
}