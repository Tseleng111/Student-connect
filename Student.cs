namespace FirebaseAuth.Models
{
	public class Student
	{
		public string? Email { get; set; }
		public string? Password { get; set; }
		public string? FullName { get; set; }
		public string? StudentNumber { get; set; }
        public string? PhotoUrl { get; set; }
		
		public string? Course { get; set; }

        public string Status { get; set; } = StatusEnum.photosubmitted.ToString();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum StatusEnum
    {
      photosubmitted,
     underreview,
    cardproduction,
     readyforcollection
    }
}

