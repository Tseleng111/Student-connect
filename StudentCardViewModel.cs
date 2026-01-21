using System.Collections.Generic;

namespace FirebaseAuth.Models
{
    public class StudentCardModel
    {
        // Photo of the selected student
        public StudentPhoto Photo { get; set; } = new StudentPhoto();

        // List of students (for email buttons)
        public List<StudentPhoto> Students { get; set; } = new List<StudentPhoto>();
    }
}
