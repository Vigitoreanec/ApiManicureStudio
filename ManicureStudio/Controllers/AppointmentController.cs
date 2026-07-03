using ManicureStudio.API.APIResult;
using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Interfaces;
using ManicureStudio.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ManicureStudio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AppointmentController(IAppointmentRepository appoinmentRepository,
                                        ILogger<AppointmentController> logger) : ControllerBase
    {
        private readonly IAppointmentRepository _appoinmentRepository = appoinmentRepository;
        private readonly ILogger<AppointmentController> _logger = logger;

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<Appointment>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Appointment>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWithDetails(int id)
        {
            var appointments = await _appoinmentRepository.GetWithDetailsAsync(id);

            if (appointments == null)
                return NotFound(ApiResult<Appointment>.Failure($"Запись с ID {id} не найдена."));


            _logger.LogInformation("Запрошена Запись, под номером {id}", appointments.Id);
            return Ok(ApiResult<Appointment>.Success(appointments));
        }

        [HttpGet("master/{masterId}")]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<Appointment>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Appointment>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetMasterAppointments(int masterId,
                                                                                [FromQuery] DateOnly date)
        {
            var appointments = await _appoinmentRepository.GetMasterAppointmentsAsync(masterId, date);

            if (appointments == null || !appointments.Any())
            {
                return NotFound(ApiResult<IEnumerable<Appointment>>.Failure($"Записи для мастера с ID {masterId} на дату {date} не найдены."));
            }

            return Ok(ApiResult<IEnumerable<Appointment>>.Success(appointments));
        }

        [HttpGet("client/{clientId}")]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<Appointment>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Appointment>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClientAppointments(int clientId)
        {
            var appointments = await _appoinmentRepository.GetClientAppointmentsAsync(clientId);

            if (appointments == null || !appointments.Any())
            {
                return NotFound(ApiResult<IEnumerable<Appointment>>.Failure($"У клиента с ID {clientId} история записей пуста."));
            }

            return Ok(ApiResult<IEnumerable<Appointment>>.Success(appointments));
        }


        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<Appointment>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Appointment>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUpcomingAppointments([FromQuery] int hoursAhead = 24)
        {
            var appointments = await _appoinmentRepository.GetUpcomingAppointmentsAsync(hoursAhead);

            _logger.LogInformation("Time Now: {now}", DateTime.Now.ToString());
            
            if (appointments == null || !appointments.Any())
            {
                return NotFound(ApiResult<IEnumerable<Appointment>>.Failure($"Нет подтвержденных записей на ближайшие {hoursAhead} ч."));
            }

            return Ok(ApiResult<IEnumerable<Appointment>>.Success(appointments));
        }
    }
}


