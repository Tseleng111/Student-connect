using System.ComponentModel.DataAnnotations;

namespace FirebaseAuth.Models
{
	public class FirebaseError
	{
		public ErrorDetails? error { get; set; }
	}

	public class ErrorDetails
	{
		public int code { get; set; }
		public string? message { get; set; }
		public ErrorInfo[]? errors { get; set; }
	}

	public class ErrorInfo
	{
		public string? message { get; set; }
		public string? domain { get; set; }
		public string? reason { get; set; }
	}

}