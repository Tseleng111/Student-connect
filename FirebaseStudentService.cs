using FirebaseAuth.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Firebase.Database.Query;
using Firebase.Database;

namespace FirebaseAuth.Services
{
    public class FirebaseStudentService 
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://studentconnect-693f9-default-rtdb.firebaseio.com/";

        public FirebaseStudentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Student?> ValidateStudentAsync(string studentNumber, string password)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}Students.json");
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            if (json == "null") return null;

            var dict = JsonConvert.DeserializeObject<Dictionary<string, Student>>(json);
            return dict?.Values.FirstOrDefault(s =>
                s.StudentNumber == studentNumber && s.Password == password);
        }

        // NEW METHOD: Get all students
        public async Task<IEnumerable<Student>> GetAllStudentsAsync()
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}Students.json");
            if (!response.IsSuccessStatusCode) return Enumerable.Empty<Student>();

            var json = await response.Content.ReadAsStringAsync();
            if (json == "null") return Enumerable.Empty<Student>();

            var dict = JsonConvert.DeserializeObject<Dictionary<string, Student>>(json);
            return dict?.Values ?? Enumerable.Empty<Student>();
        }

        public async Task<string?> GetStudentEmailAsync(string studentNumber)
        {
            var firebase = new FirebaseClient("https://studentconnect-693f9-default-rtdb.firebaseio.com/");

            // Fetch all students under the "Users" node
            var students = await firebase
                .Child("Users")
                .OnceAsync<dynamic>();

            foreach (var s in students)
            {
                if (s.Object.StudentNumber != null &&
                    s.Object.StudentNumber.ToString() == studentNumber) // convert to string
                {
                    return s.Object.Email; // return email
                }
            }

            return null; // if not found
        }



    }
}

