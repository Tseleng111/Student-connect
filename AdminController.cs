using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using FirebaseAuth.Models;
using FirebaseAuth.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Firebase.Database;
using Firebase.Database.Query;

namespace FirebaseAuth.Controllers
{
    public class AdminController : Controller
    {
        private readonly IStudentPhotoService _photoService;
        private readonly EmailService _emailService;
        private readonly FirebaseStudentService _studentService;
        public AdminController(IStudentPhotoService photoService, EmailService emailService, FirebaseStudentService studentService)
        {
            _photoService = photoService;
            _emailService = emailService;
            _studentService = studentService;
        }

       

        private bool IsAdminAuthenticated()
        {
            var token = HttpContext.Session.GetString("_UserToken");
            var email = HttpContext.Session.GetString("_UserEmail");
            return !string.IsNullOrEmpty(token) && email != null && email.Contains("@");
        }

        // Helper method to populate full student details from Firebase
        private async Task<IEnumerable<StudentPhoto>> PopulateStudentDetailsAsync(IEnumerable<StudentPhoto> studentPhotos)
        {
            try
            {
                var firebase = new Firebase.Database.FirebaseClient("https://studentconnect-693f9-default-rtdb.firebaseio.com/");

                // Fetch all users under "Users"
                var firebaseUsers = await firebase.Child("Users").OnceAsync<dynamic>();
                var firebaseStudentPhotos = await firebase.Child("StudentPhotos").OnceAsync<dynamic>();

                var userDict = new Dictionary<string, dynamic>();
                foreach (var user in firebaseUsers)
                {
                    if (user.Key != null)
                        userDict[user.Key] = user.Object;
                }

                var photoDict = new Dictionary<string, dynamic>();
                foreach (var photo in firebaseStudentPhotos)
                {
                    if (photo.Object != null && photo.Object.StudentNumber != null)
                    {
                        string studentNum = photo.Object.StudentNumber.ToString();
                        photoDict[studentNum] = photo.Object;
                    }
                }

                var studentList = studentPhotos.ToList();

                foreach (var student in studentList)
                {
                    if (!string.IsNullOrEmpty(student.StudentNumber) && userDict.ContainsKey(student.StudentNumber))
                    {
                        var fbUser = userDict[student.StudentNumber];
                        student.FullName = fbUser.FullName ?? student.StudentNumber;
                        student.Course = fbUser.Course ?? "";

                        // --- Always get photo from StudentPhotos node ---
                        if (photoDict.ContainsKey(student.StudentNumber))
                        {
                            var fbPhoto = photoDict[student.StudentNumber];
                            var photoUrlFromPhotos = fbPhoto.PhotoUrl?.ToString();

                            if (!string.IsNullOrEmpty(photoUrlFromPhotos))
                            {
                                if (!photoUrlFromPhotos.StartsWith("data:image/") &&
                                    !photoUrlFromPhotos.StartsWith("http"))
                                {
                                    // Prefix to show as an image in <img src="...">
                                    student.PhotoUrl = $"data:image/png;base64,{photoUrlFromPhotos}";
                                }
                                else
                                {
                                    student.PhotoUrl = photoUrlFromPhotos;
                                }
                            }
                        }
                    }
                    else
                    {
                        student.FullName = student.StudentNumber ?? "";
                        student.Course = "";
                    }
                }

                return studentList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Firebase error: {ex.Message}");
                foreach (var student in studentPhotos)
                {
                    if (string.IsNullOrEmpty(student.FullName))
                        student.FullName = student.StudentNumber ?? "";
                    if (string.IsNullOrEmpty(student.Course))
                        student.Course = "";
                }
                return studentPhotos;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AdminConnect(string? studentNumber)
        {
            if (!IsAdminAuthenticated())
                return RedirectToAction("Login", "AdminConnect");

            ViewBag.ShowAdminStatusLink = true;

            IEnumerable<StudentPhoto> studentPhotos = await _photoService.GetAllApprovedPhotosAsync();
            studentPhotos = await PopulateStudentDetailsAsync(studentPhotos);

            if (!string.IsNullOrEmpty(studentNumber))
            {
                studentPhotos = studentPhotos
                    .Where(s => !string.IsNullOrEmpty(s.StudentNumber) &&
                                s.StudentNumber.Contains(studentNumber, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.StudentNumber = studentNumber;
            return View(studentPhotos);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (!IsAdminAuthenticated())
                return RedirectToAction("Login", "AdminConnect");

            if (string.IsNullOrEmpty(id))
                return NotFound();

            try
            {
                var studentPhotos = await _photoService.GetAllApprovedPhotosAsync();
                studentPhotos = await PopulateStudentDetailsAsync(studentPhotos);

                StudentPhoto? student = studentPhotos
                    .FirstOrDefault(s => s.Id == id || s.StudentNumber == id || (s.StudentNumber?.Contains(id) ?? false));

                if (student == null)
                    return NotFound();

                var studentCard = new StudentCardModel
                {
                    Photo = student,
                    Students = studentPhotos.ToList()
                };

                return View(studentCard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving student details: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveForPrinting(string studentNumber)
        {
            if (string.IsNullOrEmpty(studentNumber))
            {
                return BadRequest("Student number is required.");
            }

            // Connect to your Realtime Database
            var firebaseClient = new FirebaseClient(
                "https://studentconnect-693f9-default-rtdb.firebaseio.com/");

            // Locate the student by StudentNumber
            var allStudents = await firebaseClient
                .Child("StudentPhotos")
                .OnceAsync<StudentPhoto>();

            var target = allStudents.FirstOrDefault(s => s.Object.StudentNumber == studentNumber);
            if (target == null)
            {
                return NotFound("Student not found.");
            }

            // Update only the Status field
            await firebaseClient
                .Child("StudentPhotos")
                .Child(target.Key)
                .PatchAsync(new { Status = "Card Production" });

            TempData["Message"] = $"Student {studentNumber} status set to Card Production.";

            // Redirect back to details or admin list as you prefer
            return RedirectToAction("AdminConnect");
        }

    


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdminAuthenticated())
                return RedirectToAction("Login", "AdminConnect");

            try
            {
                var studentPhotos = await _photoService.GetAllApprovedPhotosAsync();
                studentPhotos = await PopulateStudentDetailsAsync(studentPhotos);

                return View(studentPhotos);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the student list.");
            }
        }

        private async Task<bool> StudentExistsAsync(string id)
        {
            try
            {
                var studentPhotos = await _photoService.GetAllApprovedPhotosAsync();

                if (Guid.TryParse(id, out Guid guidId))
                {
                    return studentPhotos.Any(s => s.Id == guidId.ToString());
                }

                return studentPhotos.Any(s => s.StudentNumber.Equals(id, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }
        [HttpGet]
        public async Task<IActionResult>StatusUpdate()
        {
            if (!IsAdminAuthenticated())
                return RedirectToAction("Login", "AdminConnect");

            var studentPhotos = await _photoService.GetAllApprovedPhotosAsync();

            // Optional: populate extra details if needed
            studentPhotos = await PopulateStudentDetailsAsync(studentPhotos);

            ViewBag.ShowAdminStatusLink = true;

            return View(studentPhotos); // this will look for Views/Admin/ViewStatus.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> StatusUpdate(string studentId, string newStatus)
        {
            if (!IsAdminAuthenticated())
                return RedirectToAction("Login", "AdminConnect");

            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(newStatus))
                return BadRequest("Missing student ID or status.");

            // Get student
            var studentPhotos = await _photoService.GetAllApprovedPhotosAsync();
            var student = studentPhotos.FirstOrDefault(s => s.Id == studentId);
            if (student == null)
                return NotFound();

            // Update the status (you need to implement this in your service)
            await _photoService.UpdateStatusAsync(studentId, newStatus);

            // Redirect back to the same page
            return RedirectToAction("StatusUpdate");
        }
        public async Task<IActionResult> SendStudentCardEmail(string studentNumber)
        {
            var studentEmail = await _studentService.GetStudentEmailAsync(studentNumber);
            if (string.IsNullOrEmpty(studentEmail))
                return NotFound("Student email not found");

            string subject = "Collect Your Student Card";
            string body = $"Dear Student,<br><br>Your student card is ready for collection.<br>" +
                           "Please collect it at the Artec Hall between 08:00 AM and 16:00 PM.<br><br>" +
                           "Regards,<br>Admin";

            await _emailService.SendEmailAsync(studentEmail, subject, body);

            // Set TempData to show the message on Details page
            TempData["Message"] = "Email sent successfully";

            // Redirect back to Details page
            return RedirectToAction("AdminConnect", new { id = studentNumber });
        }

        public async Task<IActionResult> SendPhotoIssueEmail(string studentNumber)
        {
            var studentEmail = await _studentService.GetStudentEmailAsync(studentNumber);
            if (string.IsNullOrEmpty(studentEmail))
                return NotFound("Student email not found");

            string subject = "Issue with Submitted Photo";
            string body = $"Dear Student,<br><br>Your submitted photo is either blurry or too large.<br>Please re-upload a clear photo.<br><br>Regards,<br>Admin";

            await _emailService.SendEmailAsync(studentEmail, subject, body);

            TempData["Message"] = "Email sent successfully";

            return RedirectToAction("Details", new { id = studentNumber });
        }

        [HttpGet]
        public async Task<IActionResult> StudentCard(string studentNumber)
        {
            if (!IsAdminAuthenticated())
                return RedirectToAction("Login", "AdminConnect");

            if (string.IsNullOrEmpty(studentNumber))
                return NotFound();

            // Get all student photos
            var studentPhotos = await _photoService.GetAllApprovedPhotosAsync();
            studentPhotos = await PopulateStudentDetailsAsync(studentPhotos);

            // Find the student by StudentNumber
            var studentPhoto = studentPhotos.FirstOrDefault(s => s.StudentNumber == studentNumber);
            if (studentPhoto == null)
                return NotFound();

            // Create StudentCardModel
            var studentCard = new StudentCardModel
            {
                Photo = studentPhoto, // assuming your StudentCardModel has a 'Photo' property of type StudentPhoto
                Students = studentPhotos.ToList() // for the email buttons list
            };

            return View(studentCard); // now your view receives the correct model type
        }

    }


}

