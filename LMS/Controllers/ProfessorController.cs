using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS_CustomIdentity.Controllers
{
    [Authorize(Roles = "Professor")]
    public class ProfessorController : Controller
    {

        private readonly LMSContext db;

        public ProfessorController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Students(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
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

        public IActionResult Categories(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult CatAssignments(string subject, string num, string season, string year, string cat)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
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

        public IActionResult Submissions(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }

        public IActionResult Grade(string subject, string num, string season, string year, string cat, string aname, string uid)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            ViewData["uid"] = uid;
            return View();
        }

        /*******Begin code to modify********/


        /// <summary>
        /// Returns a JSON array of all the students in a class.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "dob" - date of birth
        /// "grade" - the student's grade in this class
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetStudentsInClass(string subject, int num, string season, int year)
        {
            var query = from c in db.Classes
                        join co in db.Courses on c.Listing equals co.CatalogId
                        join e in db.Enrolleds on c.ClassId equals e.Class
                        join s in db.Students on e.Student equals s.UId
                        where co.Department == subject && co.Number == num && c.Season == season && c.Year == year
                        select new
                        {
                            fname = s.FName,
                            lname = s.LName,
                            uid = s.UId,
                            dob = s.Dob,
                            grade = e.Grade
                        };
            return Json(query.ToArray());
        }

        /// <summary>
        /// Helper method to update the grades of students
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="uid">The uid of the student who's submission is being </param>
        /// <returns>true if successful, false otherwise</returns>
        public bool UpdateGrade(string subject, int num, string season, int year, string uid)
        {
            float scorecount;
            float maxpointcount;
            float finalscore = 0;
            float categoryWeightSum = 0;
            float scalingFactor = 0;

            // get classid
            var classIDQuery = from c in db.Classes
                               join co in db.Courses on c.Listing equals co.CatalogId
                               where co.Department == subject && co.Number == num && c.Season == season && c.Year == year
                               select c.ClassId;

            uint classID = classIDQuery.FirstOrDefault();

            // get the assignment categories using the class id
            var assignmentCategoriesQuery = from ac in db.AssignmentCategories
                                       where ac.InClass == classID
                                       select ac;
            // Got an exception because the MySqlConnection was already in use, saw here they use ToList() to solve the problem,
            // we used ToArray() instead to keep it consistent with the rest of our code
            // https://stackoverflow.com/questions/65771757/exception-this-mysqlconnection-is-already-in-use-when-using-mysql-server-ho
            var assignmentCategories = assignmentCategoriesQuery.ToArray();

            // loop through each category
            foreach(var category in assignmentCategories)
            {
                // get the assignments for each category
                var assignmentsQuery = from a in db.Assignments
                                  where category.CategoryId == a.Category
                                  select a;

                var assignments = assignmentsQuery.ToArray();

                //initialize the scorecount and maxpointcount to 0
                scorecount = 0;
                maxpointcount = 0;
               
                //loop through each assignment
                foreach (var assignment in assignments)
                {
                    //update the max point count
                    maxpointcount += assignment.MaxPoints;
                    //get the submissions for the assignment
                    var submissions = from s in db.Submissions
                                      where s.Student == uid && s.Assignment == assignment.AssignmentId
                                      select s;

                    // if the submission exists, add its score to scorecount
                    var submission = submissions.FirstOrDefault();
                    if (submission != null)
                    {
                        scorecount += submission.Score;
                    }
                }
                // make sure not to divide by 0
                if(maxpointcount != 0)
                {
                    //math from handout
                    finalscore += scorecount / maxpointcount * category.Weight;
                    // make sure the weight gets updated if the finalscore gets updated
                    categoryWeightSum += category.Weight;
                }
                
            }

            // make sure not to divide by 0
            if (categoryWeightSum != 0)
            {
                //calculate scaling factor
                scalingFactor = 100 / categoryWeightSum;
            }

            //apply scaling factor
            finalscore = finalscore * scalingFactor;
            string finalGrade;

            // get a letter grade depending on the finalscore
            if(finalscore >= 93)
            {
                finalGrade = "A";
            } else if (finalscore >= 90)
            {
                finalGrade = "A-";
            } else if (finalscore >= 87)
            {
                finalGrade = "B+";
            } else if (finalscore >= 83)
            {
                finalGrade = "B";
            } else if (finalscore >= 80)
            {
                finalGrade = "B-";
            } else if (finalscore >= 77)
            {
                finalGrade = "C+";
            } else if (finalscore >= 73)
            {
                finalGrade = "C";
            } else if (finalscore >= 70)
            {
                finalGrade = "C-";
            } else if (finalscore >= 67)
            {
                finalGrade = "D+";
            } else if (finalscore >= 63)
            {
                finalGrade = "D";
            } else if (finalscore >= 60)
            {
                finalGrade = "D-";
            } else
            {
                finalGrade = "E";
            }

            //update the grade in renrolled
            var enrolledQuery = from e in db.Enrolleds
                    where e.Student == uid && e.Class == classID
                    select e;
            var enrolled = enrolledQuery.FirstOrDefault();

            //enrolled should never be null but just in case
            if(enrolled == null)
            {
                return false;
            }

            enrolled.Grade = finalGrade;
   
            try
            { 
                db.SaveChanges();
            }
            catch
            {
                return false;
            }

            return true;                      
        }

        /// <summary>
        /// Returns a JSON array with all the assignments in an assignment category for a class.
        /// If the "category" parameter is null, return all assignments in the class.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The assignment category name.
        /// "due" - The due DateTime
        /// "submissions" - The number of submissions to the assignment
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class, 
        /// or null to return assignments from all categories</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInCategory(string subject, int num, string season, int year, string category)
        {                
            if(category == null)
            {
                var query = from c in db.Classes
                            where c.Season == season && c.Year == year
                            join co in db.Courses on c.Listing equals co.CatalogId
                            where co.Department == subject && co.Number == num
                            join ac in db.AssignmentCategories on c.ClassId equals ac.InClass
                            join a in db.Assignments on ac.CategoryId equals a.Category
                            select new
                            {
                                aname = a.Name,
                                cname = ac.Name,
                                due = a.Due,
                                // assignments have a collection of submissions
                                submissions = a.Submissions.Count()
                            };
                return Json(query.ToArray());
            }
            else
            {
                var query = from c in db.Classes
                            where c.Season == season && c.Year == year
                            join co in db.Courses on c.Listing equals co.CatalogId
                            where co.Department == subject && co.Number == num
                            join ac in db.AssignmentCategories on c.ClassId equals ac.InClass
                            join a in db.Assignments on ac.CategoryId equals a.Category
                            where ac.Name == category
                            select new
                            {
                                aname = a.Name,
                                cname = ac.Name,
                                due = a.Due,
                                submissions = a.Submissions.Count()
                            };
                return Json(query.ToArray());
            }
        }


        /// <summary>
        /// Returns a JSON array of the assignment categories for a certain class.
        /// Each object in the array should have the folling fields:
        /// "name" - The category name
        /// "weight" - The category weight
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentCategories(string subject, int num, string season, int year)
        {
            var query = from c in db.Classes
                        join co in db.Courses on c.Listing equals co.CatalogId
                        join ac in db.AssignmentCategories on c.ClassId equals ac.InClass
                        where co.Department == subject && co.Number == num && c.Season == season && c.Year == year
                        select new
                        {
                            name = ac.Name,
                            weight = ac.Weight
                        };
            return Json(query.ToArray());
        }

        /// <summary>
        /// Creates a new assignment category for the specified class.
        /// If a category of the given class with the given name already exists, return success = false.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The new category name</param>
        /// <param name="catweight">The new category weight</param>
        /// <returns>A JSON object containing {success = true/false} </returns>
        public IActionResult CreateAssignmentCategory(string subject, int num, string season, int year, string category, int catweight)
        {
            // get classid
            var classIDQuery = from c in db.Classes
                               join co in db.Courses on c.Listing equals co.CatalogId
                               where co.Department == subject && co.Number == num && c.Season == season && c.Year == year
                               select c.ClassId;

            uint classID = classIDQuery.FirstOrDefault();

            AssignmentCategory ac = new AssignmentCategory();
            ac.Weight = (uint)catweight;
            ac.InClass = classID;
            ac.Name = category;
            
            try
            {
                db.AssignmentCategories.Add(ac);
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false });
            }
            return Json(new { success = true });
        }

        /// <summary>
        /// Creates a new assignment for the given class and category.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="asgpoints">The max point value for the new assignment</param>
        /// <param name="asgdue">The due DateTime for the new assignment</param>
        /// <param name="asgcontents">The contents of the new assignment</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult CreateAssignment(string subject, int num, string season, int year, string category, string asgname, int asgpoints, DateTime asgdue, string asgcontents)
        {
            // get categoryID
            var acIDQuery = from c in db.Classes
                               join co in db.Courses on c.Listing equals co.CatalogId
                               where co.Department == subject && co.Number == num && c.Season == season && c.Year == year
                               join ac in db.AssignmentCategories on c.ClassId equals ac.InClass
                               where ac.Name == category
                               select ac.CategoryId;

            uint acID = acIDQuery.FirstOrDefault();

            Assignment a = new Assignment();
            a.Name = asgname;
            a.Contents = asgcontents;
            a.Due = asgdue;
            a.MaxPoints = (uint)asgpoints;
            a.Category = acID;
            try
            {
                db.Assignments.Add(a);
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false });
            }

            // get all the uids of students in the class
            var uidQuery = from c in db.Classes
                           join co in db.Courses on c.Listing equals co.CatalogId
                           where co.Department == subject && co.Number == num && c.Season == season && c.Year == year
                           join e in db.Enrolleds on c.ClassId equals e.Class
                           select e.Student;

            foreach(var uid in uidQuery)
            {
                if(!UpdateGrade(subject, num, season, year, uid))
                {
                    return Json(new { success = false });
                }
            }
            return Json(new { success = true });
        }


        /// <summary>
        /// Gets a JSON array of all the submissions to a certain assignment.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "time" - DateTime of the submission
        /// "score" - The score given to the submission
        /// 
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetSubmissionsToAssignment(string subject, int num, string season, int year, string category, string asgname)
        {
            var query = from c in db.Classes
                        where c.Season == season && c.Year == year
                        join co in db.Courses on c.Listing equals co.CatalogId
                        where co.Department == subject && co.Number == num
                        join ac in db.AssignmentCategories on c.ClassId equals ac.InClass
                        join a in db.Assignments on ac.CategoryId equals a.Category
                        where ac.Name == category && a.Name == asgname
                        join s in db.Submissions on a.AssignmentId equals s.Assignment
                        join stu in db.Students on s.Student equals stu.UId
                        select new
                        {
                            fname = stu.FName,
                            lname = stu.LName,
                            uid = stu.UId,
                            time = s.Time,
                            score = s.Score
                        };

                        
            return Json(query.ToArray());
        }


        /// <summary>
        /// Set the score of an assignment submission
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <param name="uid">The uid of the student who's submission is being graded</param>
        /// <param name="score">The new score for the submission</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult GradeSubmission(string subject, int num, string season, int year, string category, string asgname, string uid, int score)
        {
            var query = from c in db.Classes
                        where c.Season == season && c.Year == year
                        join co in db.Courses on c.Listing equals co.CatalogId
                        where co.Department == subject && co.Number == num
                        join ac in db.AssignmentCategories on c.ClassId equals ac.InClass
                        join a in db.Assignments on ac.CategoryId equals a.Category
                        where ac.Name == category && a.Name == asgname
                        join s in db.Submissions on a.AssignmentId equals s.Assignment
                        join stu in db.Students on s.Student equals stu.UId
                        where stu.UId == uid
                        select s;

            try
            {
                query.ToArray()[0].Score = (uint)score;
                db.SaveChanges();
            }
            catch
            {
                return Json(new { success = false });
            }

            if(UpdateGrade(subject, num, season, year, uid))
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false });
            }
        }


        /// <summary>
        /// Returns a JSON array of the classes taught by the specified professor
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester in which the class is taught
        /// "year" - The year part of the semester in which the class is taught
        /// </summary>
        /// <param name="uid">The professor's uid</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {
            var query = from c in db.Classes
                        where c.TaughtBy == uid
                        join co in db.Courses on c.Listing equals co.CatalogId
                        select new
                        {
                            subject = co.Department,
                            number = co.Number,
                            name = co.Name,
                            season = c.Season,
                            year = c.Year
                        };

     
            return Json(query.ToArray());
        }


        
        /*******End code to modify********/
    }
}

