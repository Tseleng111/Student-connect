using FirebaseAuth.Models;
using FirebaseAuth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace FirebaseAuth.Controllers
{
    public class StudentController : Controller
    {
        private readonly FirebaseStudentService _firebaseService;

        public StudentController(FirebaseStudentService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult StudentConnect()
        {
            ViewBag.ShowTrackStatus = true; // Only true on dashboard
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required");
            }
            else if (model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters long");
            }
            else if (!Regex.IsMatch(model.Password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                ModelState.AddModelError("Password", "Password must contain at least one special character");
            }
            // End of password validation

            if (!ModelState.IsValid)
                return View(model);
            // Determine email for Firebase login
            string email;
            if (model.UserIdentifier.Contains("@"))
            {
                // Admin login (typed email)
                email = model.UserIdentifier;
            }

            else
            {
                // Student login (typed student number)
                email = $"{model.UserIdentifier}@stud.cut.ac.za";
            }

            // Validate login using the Firebase service
            var student = await _firebaseService.ValidateStudentAsync(email, model.Password!);

            if (student != null)
            {
                // Optional: store session variables
                HttpContext.Session.SetString("_UserEmail", email);
                HttpContext.Session.SetString("_UserIdentifier", model.UserIdentifier);
                HttpContext.Session.SetString("_UserName", student.FullName ?? model.UserIdentifier);
                HttpContext.Session.SetString("_UserToken", Guid.NewGuid().ToString());

                // Redirect to dashboard or main page
                return RedirectToAction("Dashboard", "Home", new { studentName = student.FullName });
            }

            ModelState.AddModelError("", "Invalid student number/email or password");
            return View(model);
        }

        public IActionResult TrackStatus(string studentNumber)
        {
            // FIXED: Get student number from multiple sources
            string finalStudentNumber = null;

            // Priority 1: URL parameter
            if (!string.IsNullOrEmpty(studentNumber))
            {
                finalStudentNumber = studentNumber;
            }
            // Priority 2: Session data (most common case)
            else if (HttpContext.Session.GetString("_UserIdentifier") != null)
            {
                string userIdentifier = HttpContext.Session.GetString("_UserIdentifier");
                // If it's not an email (doesn't contain @), it's a student number
                if (!userIdentifier.Contains("@"))
                {
                    finalStudentNumber = userIdentifier;
                }
            }
            // Priority 3: Query string
            else if (!string.IsNullOrEmpty(Request.Query["studentId"]))
            {
                finalStudentNumber = Request.Query["studentId"];
            }

            ViewBag.StudentId = finalStudentNumber;
            ViewBag.Debug_StudentNumber = finalStudentNumber; // For debugging
            ViewBag.Debug_SessionIdentifier = HttpContext.Session.GetString("_UserIdentifier");
            ViewBag.Debug_QueryString = Request.Query["studentId"].ToString();

            return View();
        }

    }
}
