using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS.Controllers
{
    public class AdministratorController : Controller
    {
        private readonly LMSContext db;

        public AdministratorController(LMSContext _db)
        {
            db = _db;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Department(string subject)
        {
            ViewData["subject"] = subject;
            return View();
        }

        public IActionResult Course(string subject, string num)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }

        /*******Begin code to modify********/

        /// <summary>
        /// Create a department which is uniquely identified by it's subject code
        /// </summary>
        /// <param name="subject">the subject code</param>
        /// <param name="name">the full name of the department</param>
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the department already exists, true otherwise.</returns>
        public IActionResult CreateDepartment(string subject, string name)
        {
            Department d = new Department();
            d.Name = name;
            d.Subject = subject; 
            // try to add a department
            try
            {
                db.Departments.Add(d);
                db.SaveChanges();  
            } 
            catch
            {
                // if it gets here return false in the json object
                return Json(new { success = false });
            }
            // if it worked return true in the json object
            return Json(new { success = true });
        }


        /// <summary>
        /// Returns a JSON array of all the courses in the given department.
        /// Each object in the array should have the following fields:
        /// "number" - The course number (as in 5530)
        /// "name" - The course name (as in "Database Systems")
        /// </summary>
        /// <param name="subjCode">The department subject abbreviation (as in "CS")</param>
        /// <returns>The JSON result</returns>
        public IActionResult GetCourses(string subject)
        {
            var query = from c in db.Courses
                        //should we check for capitalization?
                        where c.Department == subject
                        select new
                        {
                            number = c.Number,
                            name = c.Name
                        };
            return Json(query.ToArray());
        }

        /// <summary>
        /// Returns a JSON array of all the professors working in a given department.
        /// Each object in the array should have the following fields:
        /// "lname" - The professor's last name
        /// "fname" - The professor's first name
        /// "uid" - The professor's uid
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <returns>The JSON result</returns>
        public IActionResult GetProfessors(string subject)
        {
            
            var query = from p in db.Professors
                        where p.WorksIn == subject
                        select new
                        {
                            lname = p.LName,
                            fname = p.FName,
                            uid = p.UId
                        };

            //over cooked with this
            // this might return duplicate professors if a professor teaches more than 1 class
            //var query = from course in db.Courses
            //            join c in db.Classes on course.CatalogId equals c.Listing
            //            join p in db.Professors on c.TaughtBy equals p.UId
            //            where course.Department == subject
            //            select new
            //            {
            //                lname = p.LName,
            //                fname = p.FName,
            //                uid = p.UId
            //            };

            return Json(query.ToArray());
            
        }



        /// <summary>
        /// Creates a course.
        /// A course is uniquely identified by its number + the subject to which it belongs
        /// </summary>
        /// <param name="subject">The subject abbreviation for the department in which the course will be added</param>
        /// <param name="number">The course number</param>
        /// <param name="name">The course name</param>
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the course already exists, true otherwise.</returns>
        public IActionResult CreateCourse(string subject, int number, string name)
        {
            Course course = new Course();
            course.Number = (uint)number;
            course.Department = subject;
            course.Name = name;
            // not sure if we should query to see if a course exists or just do this, I think they have the same result
            try
            {
                db.Courses.Add(course);
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false });
            }
            return Json(new { success = true });
        }



        /// <summary>
        /// Creates a class offering of a given course.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="number">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <param name="location">The location</param>
        /// <param name="instructor">The uid of the professor</param>
        /// <returns>A JSON object containing {success = true/false}. 
        /// false if another class occupies the same location during any time 
        /// within the start-end range in the same semester, or if there is already
        /// a Class offering of the same Course in the same Semester,
        /// true otherwise.</returns>
        public IActionResult CreateClass(string subject, int number, string season, int year, DateTime start, DateTime end, string location, string instructor)
        {
            // get the courseID
            var query = from course in db.Courses
                        where course.Number == number && course.Department == subject
                        select course.CatalogId;
            uint listing = query.FirstOrDefault();

            Class c = new Class();
            c.Season = season;
            c.Listing = listing;
            c.Year = (uint)year;
            // found out how to convert from DateTime to TimeOnly Here: https://stackoverflow.com/questions/4097127/getting-date-or-time-only-from-a-datetime-object cant remember if it was mentioned in class
            c.StartTime = TimeOnly.FromDateTime(start);
            c.EndTime = TimeOnly.FromDateTime(end);
            c.Location = location;
            c.TaughtBy = instructor;
            try
            {
                db.Classes.Add(c);
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false });
            }
            return Json(new { success = true });
        }


        /*******End code to modify********/

    }
}

