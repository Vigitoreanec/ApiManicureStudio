using ManicureStudio.API.APIResult;
using ManicureStudio.Core.Entities;
using ManicureStudio.Core.Interfaces;
using ManicureStudio.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ManicureStudio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    /*[Authorize]*/
    public class MastersController(IMasterRepository masterRepository, 
                                    ILogger<MastersController> logger) : ControllerBase
    {
        private readonly IMasterRepository _masterRepository = masterRepository;
        private readonly ILogger<MastersController> _logger = logger;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResult<IEnumerable<Master>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var masters = await _masterRepository.GetActiveMastersWithServicesAsync();
            _logger.LogInformation("Запрошен список Мастеров. Найдено: {Count}", masters.Count());
            return Ok(ApiResult<IEnumerable<Master>>.Success(masters));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var master = await _masterRepository.GetByIdAsync(id);
            if (master == null)
                return NotFound(ApiResult<Master>.Failure($"Мастер с ID={id} не найден"));

            return Ok(ApiResult<Master>.Success(master));
        }

        [HttpGet("by-service/{serviceId:int}")]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByService(int serviceId)
        {
            var service = await _masterRepository.GetServiceNameByIdAsync(serviceId);
            if (service == null)
            {
                return NotFound(ApiResult<List<Master>>.Failure($"Услуга не существует"));
            }

            var masters = await _masterRepository.GetMastersByServiceAsync(serviceId);
            if (masters == null)
            {
                return NotFound(ApiResult<List<Master>>.Failure($"Мастера для услуги {service} не найдены"));
            }
            _logger.LogInformation("Запрошен список Мастеров на услугу:\"{service}\". Найдено: {Count}", service, masters.Count());
            return Ok(ApiResult<IEnumerable<Master>>.Success(masters));
        }

        [HttpGet("{id:int}/availability")]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CheckAvailability(
        int id,
        [FromQuery] DateTime start,
        [FromQuery] DateTime end)
        {
            var isAvailable = await _masterRepository.IsMasterAvailableAsync(id, start, end);
            return Ok(ApiResult<bool>.Success(isAvailable,
                isAvailable ? "Мастер свободен" : "Мастер занят в это время"));
        }

        [HttpPost]
        /*[Authorize(Roles = "Admin")]*/
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] Master master)
        {
            var created = await _masterRepository.AddAsync(master);
            await _masterRepository.SaveChangesAsync();

            _logger.LogInformation("Добавлен мастер: {FirstName} {LastName}", created.FirstName, created.LastName);

            return CreatedAtAction(nameof(GetById), new { id = created.Id },
                ApiResult<Master>.Success(created, "Мастер добавлен"));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Master")]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<Master>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] Master master)
        {
            var existing = await _masterRepository.GetByIdAsync(id);
            if ( existing == null)
                return NotFound(ApiResult<Master>.Failure("Ошибка обновления", $"Мастер с ID={id} не найден"));

            existing.FirstName = master.FirstName;
            existing.LastName = master.LastName;
            existing.PhoneNumber = master.PhoneNumber;
            existing.Email = master.Email;
            existing.Specialization = master.Specialization;
            existing.Description = master.Description;
            existing.PhotoUrl = master.PhotoUrl;
            existing.IsActive = master.IsActive;

            existing.UpdatedAt = DateTime.Now;
            existing.Appointments = master.Appointments;

            await _masterRepository.UpdateAsync(existing);
            await _masterRepository.SaveChangesAsync();

            _logger.LogInformation("Обновлен клиент: {FirstName} {LastName}, ID={Id}",
                                   existing.FirstName,
                                   existing.LastName,
                                   existing.Id);

            return Ok(ApiResult<Master>.Success(existing, "Данные мастера обновлены"));
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _masterRepository.GetByIdAsync(id) == null)
                return NotFound(ApiResult.Failure($"Мастер с ID={id} не найден"));

            await _masterRepository.SoftDeleteAsync(id);
            await _masterRepository.SaveChangesAsync();

            return Ok(ApiResult.Ok($"Мастер ID={id} деактивирован"));
        }

    }
}
