using Firebase.Auth;
using FirebaseAuth.Models;
using FirebaseAuth.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Claims;
using Firebase.Database.Query;


namespace FirebaseAuth.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FirebaseAuthProvider auth;

        // If you already have some service to get all students, inject it here
        private readonly FirebaseStudentService _studentService;

        public HomeController(ILogger<HomeController> logger, FirebaseStudentService studentService)
        {
            _logger = logger;
            auth = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyCLgrVbwCc1tx2o4_sssp_0Al1rhILlkK8"));
            _studentService = studentService;
        }

        public IActionResult Index()
        {
            var token = HttpContext.Session.GetString("_UserToken");
            var studentNumber = HttpContext.Session.GetString("_StudentNumber");
            var userName = HttpContext.Session.GetString("_UserName");

            if (token != null)
            {
                ViewBag.StudentNumber = studentNumber;
                ViewBag.UserName = userName ?? $"Student {studentNumber}";
                ViewBag.WelcomeMessage = $"Welcome, {userName ?? studentNumber}!";
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            if (!ModelState.IsValid)
                return View(loginModel);

            try
            {
                string emailAddress;
                bool isAdmin = loginModel.UserIdentifier.Contains("@");

                if (isAdmin)
                {
                    emailAddress = loginModel.UserIdentifier;
                }
                else
                {
                    emailAddress = $"{loginModel.UserIdentifier}@stud.cut.ac.za";
                }

                _logger.LogInformation($"Attempting to sign in user: {emailAddress}");

                var fbAuthLink = await auth.SignInWithEmailAndPasswordAsync(emailAddress, loginModel.Password);
                string token = fbAuthLink.FirebaseToken;

                if (token != null)
                {
                    // Set basic session values for all users
                    HttpContext.Session.SetString("_UserToken", token);
                    HttpContext.Session.SetString("_UserIdentifier", loginModel.UserIdentifier);
                    HttpContext.Session.SetString("_UserEmail", emailAddress);
                    HttpContext.Session.SetString("_UserRole", isAdmin ? "Admin" : "Student");

                    // Set identifier for both students and admins
                    if (isAdmin)
                    {
                        HttpContext.Session.SetString("_AdminEmail", loginModel.UserIdentifier);
                        HttpContext.Session.SetString("_StudentNumber", "ADMIN_USER"); // Allow admins to access student functions
                    }
                    else
                    {
                        HttpContext.Session.SetString("_StudentNumber", loginModel.UserIdentifier);
                    }

                    if (fbAuthLink.User != null)
                    {
                        try
                        {
                            _logger.LogInformation("Fetching user data from Firebase Database...");

                            var firebase = new Firebase.Database.FirebaseClient("https://studentconnect-693f9-default-rtdb.firebaseio.com/");

                            // Fetch all students first
                            var allStudents = await firebase
                                .Child("Users")
                                .OnceAsync<Student>();

                            _logger.LogInformation($"Retrieved {allStudents?.Count ?? 0} users from database");

                            Student studentFromDb = null;

                            if (isAdmin)
                            {
                                // Admin login with Email
                                studentFromDb = allStudents
                                    .Select(x => x.Object)
                                    .FirstOrDefault(s => s.Email == loginModel.UserIdentifier);
                            }
                            else
                            {
                                // Student login with StudentNumber
                                studentFromDb = allStudents
                                    .Select(x => x.Object)
                                    .FirstOrDefault(s => s.StudentNumber != null && s.StudentNumber.ToString() == loginModel.UserIdentifier);
                            }

                            // Set session values
                            var fullName = studentFromDb?.FullName ?? loginModel.UserIdentifier;
                            var course = studentFromDb?.Course ?? (isAdmin ? "Administrator" : "Unknown");

                            HttpContext.Session.SetString("_UserName", fullName);
                            HttpContext.Session.SetString("_StudentCourse", course);
                            HttpContext.Session.SetString("_UserId", fbAuthLink.User.LocalId);

                            _logger.LogInformation($"User data set - Name: {fullName}, Course: {course}");
                        }
                        catch (Exception dbEx)
                        {
                            _logger.LogWarning($"Could not fetch user data from database: {dbEx.Message}");
                            // Continue with login even if database fetch fails
                            HttpContext.Session.SetString("_UserName", loginModel.UserIdentifier);
                            HttpContext.Session.SetString("_StudentCourse", isAdmin ? "Administrator" : "Unknown");
                        }
                    }

                    _logger.LogInformation($"User {loginModel.UserIdentifier} signed in successfully as {(isAdmin ? "Admin" : "Student")}");

                    // Redirect based on user type
                    if (isAdmin)
                        return RedirectToAction("AdminConnect", "Admin");
                    else
                        return RedirectToAction("StudentConnect");
                }
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError($"Firebase authentication error: {ex.Message}");
                _logger.LogError($"Response data: {ex.ResponseData}");

                try
                {
                    var firebaseEx = JsonConvert.DeserializeObject<FirebaseError>(ex.ResponseData);
                    string errorMessage = firebaseEx?.error?.message ?? "Authentication failed.";
                    ModelState.AddModelError(string.Empty, errorMessage);
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Authentication failed. Please check your credentials.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during sign in: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                ModelState.AddModelError(string.Empty, $"An unexpected error occurred: {ex.Message}");
            }

            return View(loginModel);
        }

        public IActionResult StudentConnect()
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login");

            ViewBag.UserName = HttpContext.Session.GetString("_UserName") ?? "Student";
            ViewBag.StudentNumber = HttpContext.Session.GetString("_StudentNumber");
            ViewBag.UserEmail = HttpContext.Session.GetString("_UserEmail");
            ViewBag.WelcomeMessage = $"Welcome, {ViewBag.UserName}!";

            return View();
        }

        public IActionResult Assessments()
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login");

            ViewBag.UserName = HttpContext.Session.GetString("_UserName") ?? "Student";
            return View();
        }

        public IActionResult Timetables()
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login");

            ViewBag.UserName = HttpContext.Session.GetString("_UserName") ?? "Student";
            return View();
        }

        public IActionResult Accounts()
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login");

            ViewBag.UserName = HttpContext.Session.GetString("_UserName") ?? "Student";
            ViewBag.StudentNumber = HttpContext.Session.GetString("_StudentNumber");
            ViewBag.UserEmail = HttpContext.Session.GetString("_UserEmail");
            return View();
        }

        public IActionResult StudentProfile()
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login");

            ViewBag.UserName = HttpContext.Session.GetString("_UserName") ?? "Student";
            ViewBag.StudentNumber = HttpContext.Session.GetString("_StudentNumber");
            ViewBag.UserEmail = HttpContext.Session.GetString("_UserEmail");
            return View();
        }

        public IActionResult UploadPhoto()
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login");

            ViewBag.ShowTrackStatus = true;  // ✅ Make the link appear
            return View();

           
        }


        [HttpPost]
        public async Task<IActionResult> UploadPhoto(IFormFile photo, string photoData)
        {
            if (!IsUserAuthenticated())
                return RedirectToAction("Login");

            

            try
            {
                var studentNumber = HttpContext.Session.GetString("_StudentNumber") ?? "Unknown";
                var userName = HttpContext.Session.GetString("_UserName") ?? "Unknown";
                string base64Image = null;

                if (photo != null && photo.Length > 0)
                {
                    // Convert gallery photo to Base64
                    using (var ms = new MemoryStream())
                    {
                        await photo.CopyToAsync(ms);
                        var bytes = ms.ToArray();
                        base64Image = Convert.ToBase64String(bytes);
                    }
                }
                else if (!string.IsNullOrEmpty(photoData))
                {
                    // Camera photo (already Base64)
                    base64Image = photoData.Split(',')[1]; // remove "data:image/png;base64,"
                }
                else
                {
                    ModelState.AddModelError("photo", "Please select a photo to upload.");
                    return View();
                }

                // Save metadata + image to Firebase Realtime Database
                var firebase = new Firebase.Database.FirebaseClient("https://studentconnect-693f9-default-rtdb.firebaseio.com/"); // your DB URL
                var newPhoto = new StudentPhoto
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentNumber = studentNumber,
                    FullName = userName,
                    PhotoUrl = base64Image,
                    SubmissionDate = DateTime.Now
                };

                // This ensures it's stored under "StudentPhotos" node
                await firebase
                    .Child("StudentPhotos")
                    .Child(newPhoto.Id)
                    .PutAsync(newPhoto);

                TempData["SuccessMessage"] = "Photo uploaded successfully!";
                return RedirectToAction("UploadSuccess");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading photo: {ex.Message}");
                ModelState.AddModelError("photo", "Failed to upload photo. Please try again.");
                return View();
            }
        }





        public IActionResult LogOut()
        {
            var userIdentifier = HttpContext.Session.GetString("_UserIdentifier");
            HttpContext.Session.Clear();
            _logger.LogInformation($"User {userIdentifier} logged out successfully");
            TempData["InfoMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        private bool IsUserAuthenticated()
        {
            var token = HttpContext.Session.GetString("_UserToken");
            return !string.IsNullOrEmpty(token);
        }

        [HttpGet]
        public IActionResult CheckAuthStatus()
        {
            return Json(new { isAuthenticated = IsUserAuthenticated() });
        }

        [HttpGet]
        public IActionResult GetUserInfo()
        {
            if (!IsUserAuthenticated())
            {
                return Json(new { success = false, message = "Not authenticated" });
            }

            return Json(new
            {
                success = true,
                userName = HttpContext.Session.GetString("_UserName") ?? "Student",
                studentNumber = HttpContext.Session.GetString("_StudentNumber"),
                userEmail = HttpContext.Session.GetString("_UserEmail"),
                userPhoto = HttpContext.Session.GetString("_UserPhoto")
            });
        }

        public IActionResult UploadSuccess()
        {
            return View();
        }

        

        [HttpPost]
        public IActionResult StudentDetails(string StudentNumber, string Name, DateTime SubmissionDate)
        {
            var student = new Student
            {
                StudentNumber = StudentNumber,
                FullName = Name,
                //SubmissionDate = SubmissionDate
            };

            return View(student); // Show details view
        }
    }
}
