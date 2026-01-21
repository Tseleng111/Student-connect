using FirebaseAuth.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebaseAuth.Services
{
    public interface IStudentPhotoService
    {
        Task<IEnumerable<StudentPhoto>> GetAllApprovedPhotosAsync();
        Task<IEnumerable<StudentPhoto>> GetApprovedPhotoByStudentNumberAsync(string studentNumber);

        Task UpdateStatusAsync(string studentId, string newStatus);

    }
}
