using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SWARM.EF.Data;
using SWARM.EF.Models;
using SWARM.Server.Models;
using SWARM.Shared;
using SWARM.Shared.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Telerik.DataSource;
using Telerik.DataSource.Extensions;

namespace SWARM.Server.Controllers.Crse
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : Controller
    {
        protected readonly SWARMOracleContext _context;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public StudentController(SWARMOracleContext context, IHttpContextAccessor httpContextAccessor)
        {
            this._context = context;
            this._httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        [Route("GetStudents")]
        public async Task<IActionResult> GetStudents()
        {
            List<Student> lstStudents = await _context.Students.OrderBy(x => x.StudentNo).ToListAsync();
            return Ok(lstStudents);
        }

        [HttpGet]
        [Route("GetStudents/{pStudentNo}")]
        public async Task<IActionResult> GetStudent(int pStudentNo)
        {
            Student itmStudent = await _context.Students.Where(x => x.StudentNo == pStudentNo).FirstOrDefaultAsync();
            return Ok(itmStudent);
        }

        [HttpDelete]
        [Route("DeleteStudent/{pStudentNo}")]
        public async Task<IActionResult> DeleteStudent(int pStudentNo)
        {
            Student itmStudent = await _context.Students.Where(x => x.StudentNo == pStudentNo).FirstOrDefaultAsync();
            _context.Remove(itmStudent);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] StudentDTO _StudentDTO)
        {
            var trans = _context.Database.BeginTransaction();
            try
            {
                var existStudent = await _context.Students.Where(x => x.StudentNo == _StudentDTO.StudentNo).FirstOrDefaultAsync();

                existStudent.Cost = _StudentDTO.Cost;
                existStudent.Description = _StudentDTO.Description;
                existStudent.Prerequisite = _StudentDTO.Prerequisite;
                existStudent.PrerequisiteSchoolId = _StudentDTO.PrerequisiteSchoolId;
                existStudent.SchoolId = _StudentDTO.SchoolId;
                _context.Update(existStudent);
                await _context.SaveChangesAsync();
                trans.Commit();

                return Ok(_StudentDTO.StudentNo);
            }
            catch (Exception ex)
            {
                trans.Rollback();
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }



        [HttpPost]
        [Route("GetStudents")]
        public async Task<DataEnvelope<StudentDTO>> GetStudentsPost([FromBody] DataSourceRequest gridRequest)
        {
            DataEnvelope<StudentDTO> dataToReturn = null;
            IQueryable<StudentDTO> queriableStates = _context.Students
                    .Select(sp => new StudentDTO
                    {
                        Cost = sp.Cost,
                        StudentNo = sp.StudentNo,
                        CreatedBy = sp.CreatedBy,
                        CreatedDate = sp.CreatedDate,
                        Description = sp.Description,
                        ModifiedBy = sp.ModifiedBy,
                        ModifiedDate = sp.ModifiedDate,
                        Prerequisite = sp.Prerequisite,
                        PrerequisiteSchoolId = sp.PrerequisiteSchoolId,
                        SchoolId = sp.SchoolId
                    });

            // use the Telerik DataSource Extensions to perform the query on the data
            // the Telerik extension methods can also work on "regular" collections like List<T> and IQueriable<T>
            try
            {

                DataSourceResult processedData = await queriableStates.ToDataSourceResultAsync(gridRequest);

                if (gridRequest.Groups.Count > 0)
                {
                    // If there is grouping, use the field for grouped data
                    // The app must be able to serialize and deserialize it
                    // Example helper methods for this are available in this project
                    // See the GroupDataHelper.DeserializeGroups and JsonExtensions.Deserialize methods
                    dataToReturn = new DataEnvelope<StudentDTO>
                    {
                        GroupedData = processedData.Data.Cast<AggregateFunctionsGroup>().ToList(),
                        TotalItemCount = processedData.Total
                    };
                }
                else
                {
                    // When there is no grouping, the simplistic approach of 
                    // just serializing and deserializing the flat data is enough
                    dataToReturn = new DataEnvelope<StudentDTO>
                    {
                        CurrentPageData = processedData.Data.Cast<StudentDTO>().ToList(),
                        TotalItemCount = processedData.Total
                    };
                }
            }
            catch (Exception e)
            {
                //fixme add decent exception handling
            }
            return dataToReturn;
        }

    }
}
