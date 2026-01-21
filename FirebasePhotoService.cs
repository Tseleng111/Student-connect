using Firebase.Database;
using Firebase.Database.Query;
using FirebaseAuth.Models;
using FirebaseAuth.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class FirebaseStudentPhotoService : IStudentPhotoService
{
    private readonly FirebaseClient _firebase;

    public FirebaseStudentPhotoService(FirebaseClient firebase)
    {
        _firebase = firebase;
    }

    public async Task<IEnumerable<StudentPhoto>> GetAllApprovedPhotosAsync()
    {
        var studentPhotos = await _firebase.Child("StudentPhotos").OnceAsync<StudentPhoto>();

        return studentPhotos
            .Select(item => new StudentPhoto
            {
                Id = item.Key,
                StudentNumber = item.Object.StudentNumber,
                FullName = string.IsNullOrEmpty(item.Object.FullName) ? "—" : item.Object.FullName,
                PhotoUrl = item.Object.PhotoUrl,
                SubmissionDate = item.Object.SubmissionDate,
                Status = item.Object.Status // ✅ include Status here
            })
            .Where(s => !string.IsNullOrEmpty(s.PhotoUrl));
    }

    public async Task<IEnumerable<StudentPhoto>> GetApprovedPhotoByStudentNumberAsync(string studentNumber)
    {
        // Use GetAllApprovedPhotosAsync() and filter by StudentNumber
        var allPhotos = await GetAllApprovedPhotosAsync();

        return allPhotos
            .Where(p => !string.IsNullOrEmpty(p.StudentNumber) && p.StudentNumber == studentNumber);
    }

    public async Task UpdateStatusAsync(string studentId, string newStatus)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Updating status for student ID: {studentId} to: {newStatus}");

            await _firebase
                .Child("StudentPhotos")
                .Child(studentId)  // Use studentId directly as the key
                .Child("Status")
                .PutAsync(newStatus);

            System.Diagnostics.Debug.WriteLine($"Status updated successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating status: {ex.Message}");
            throw;
        }
    }
}
