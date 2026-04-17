using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private LMSContext db;
        public StudentController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Catalog()
        {
            return View();
        }

        public IActionResult Class(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult Assignment(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }


        public IActionResult ClassListings(string subject, string num)
        {
            System.Diagnostics.Debug.WriteLine(subject + num);
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }


        /*******Begin code to modify********/

        /// <summary>
        /// Returns a JSON array of the classes the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester
        /// "year" - The year part of the semester
        /// "grade" - The grade earned in the class, or "--" if one hasn't been assigned
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {
            // join course with class with enrolled with student
            var query = from e in db.Enrolleds 
                        join c in db.Classes on e.Class equals c.ClassId
                        join course in db.Courses on c.Listing equals course.CatalogId
                        where e.Student == uid
                        select new
                        {
                            subject = course.Department,
                            number = course.Number,
                            name = course.Name,
                            season = c.Season,
                            year = c.Year,
                            grade = e.Grade
                        };
            return Json(query.ToArray());
        }

        /// <summary>
        /// Returns a JSON array of all the assignments in the given class that the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The category name that the assignment belongs to
        /// "due" - The due Date/Time
        /// "score" - The score earned by the student, or null if the student has not submitted to this assignment.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="uid"></param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInClass(string subject, int num, string season, int year, string uid)
        {
            // bunch of manual joins
            var query = from c in db.Classes
                        where c.Season == season
                              && c.Year == year
                              && c.Course.Department == subject
                              && c.Course.Number == num
                              && c.Enrolled.Any(e => e.Student == uid)
                        from category in c.AssignmentCategories
                        from assignment in category.Assignments
                        join submission in db.Submissions.Where(s => s.Student == uid)
                            on assignment.AssignmentId equals submission.Assignment into temp
                        from sub in temp.DefaultIfEmpty()
                        select new
                        {
                            aname = assignment.Name,
                            cname = category.Name,
                            due = assignment.Due,
                            score = (uint?)sub.Score
                        };
                        
            return Json(query.ToArray());
        }



        /// <summary>
        /// Adds a submission to the given assignment for the given student
        /// The submission should use the current time as its DateTime
        /// You can get the current time with DateTime.Now
        /// The score of the submission should start as 0 until a Professor grades it
        /// If a Student submits to an assignment again, it should replace the submission contents
        /// and the submission time (the score should remain the same).
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="uid">The student submitting the assignment</param>
        /// <param name="contents">The text contents of the student's submission</param>
        /// <returns>A JSON object containing {success = true/false}</returns>
        public IActionResult SubmitAssignmentText(string subject, int num, string season, int year,
          string category, string asgname, string uid, string contents)
        {
            // get the assignment ID
            var assignmentIDQuery = from course in db.Courses
                        join c in db.Classes on course.CatalogId equals c.Listing
                        join ac in db.AssignmentCategories on c.ClassId equals ac.InClass
                        join a in db.Assignments on ac.CategoryId equals a.Category
                        where a.Name == asgname 
                        && course.Department == subject 
                        && course.Number == num 
                        && c.Season == season 
                        && c.Year == year 
                        && ac.Name == category
                        select a.AssignmentId;

            uint assignmentID = assignmentIDQuery.FirstOrDefault();

            var submissionQuery = from s in db.Submissions
                                  where s.Student == uid && s.Assignment == assignmentID
                                  select s.SubmissionContents;

            

            // if there is no submission, create a new one
            if(submissionQuery.FirstOrDefault() == null)
            {
                //System.Diagnostics.Debug.WriteLine("Create new submission reached");
                Submission submission = new Submission();
                submission.Student = uid;
                submission.SubmissionContents = contents;
                submission.Assignment = assignmentID;
                submission.Time = DateTime.Now;
                submission.Score = 0;
                try
                {
                    db.Submissions.Add(submission);
                    db.SaveChanges();
                    
                }
                catch
                {
                    return Json(new { success = false });
                }
                

            }
            // if not, update the existing submission
            else
            {
                //System.Diagnostics.Debug.WriteLine("Update submission reached");
                var query = from s in db.Submissions
                            where s.Student == uid && s.Assignment == assignmentID
                            select s;
                query.ToArray()[0].SubmissionContents = contents;
                query.ToArray()[0].Time = DateTime.Now;
                try
                {
                    db.SaveChanges();
                }
                catch
                {
                    return Json(new { success = false });
                }
            }
            
            return Json(new { success = true });
        }


        /// <summary>
        /// Enrolls a student in a class.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing {success = {true/false}. 
        /// false if the student is already enrolled in the class, true otherwise.</returns>
        public IActionResult Enroll(string subject, int num, string season, int year, string uid)
        {          
            return Json(new { success = false});
        }



        /// <summary>
        /// Calculates a student's GPA
        /// A student's GPA is determined by the grade-point representation of the average grade in all their classes.
        /// Assume all classes are 4 credit hours.
        /// If a student does not have a grade in a class ("--"), that class is not counted in the average.
        /// If a student is not enrolled in any classes, they have a GPA of 0.0.
        /// Otherwise, the point-value of a letter grade is determined by the table on this page:
        /// https://advising.utah.edu/academic-standards/gpa-calculator-new.php
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing a single field called "gpa" with the number value</returns>
        public IActionResult GetGPA(string uid)
        {            
            return Json(null);
        }
                
        /*******End code to modify********/

    }
}

