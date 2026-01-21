namespace FirebaseAuth.Models
{

    public class StudentPhoto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique ID

        public string StudentNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        // Full Name
        public string? PhotoUrl { get; set; }                       // Optional

        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public string Course { get; set; } 
        public string Email { get; set; }

        public string Status { get; set; } = "PhotoSubmitted";

    }
}

